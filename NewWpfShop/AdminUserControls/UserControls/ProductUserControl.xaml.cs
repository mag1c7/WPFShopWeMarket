using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.UserWindows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Collections.Specialized.BitVector32;
namespace NewWpfShop.AdminUserControls.UserControls
{
    /// <summary>
    /// Логика взаимодействия для ProductUserControl.xaml
    /// </summary>
    public partial class ProductUserControl : UserControl
    {
		private static readonly HttpClient httpClient = new HttpClient();
		private Product product;
		private int myproductId;
		private bool isUserInput = true;

		public Product _product
		{
			get => product;
			set
			{
				product = value;
				if (product != null)
				{
					labelname.Content = product.Name;
					labelprice.Content = product.Price + " Р";
					labelcount.Content = product.Stock + " Шт.";
					myproductId = product.ProductId;
					LoadImageFromApi(product.ProductId);
				}
			}
		}
		public ProductUserControl(Product product)
		{
			InitializeComponent();
			_product = product;
			this.Loaded += async (s, e) => await LoadCartItems();
		}
		private async Task LoadCartItems()
		{
			int userId = Session.IdUser;
			if (userId == 0) return;

			try
			{
				// Получаем информацию о количестве товара в корзине
				var response = await httpClient.GetAsync($"http://localhost:5099/TakeInfoCartItem?userId={userId}&productId={product.ProductId}");

				if (response.IsSuccessStatusCode)
				{
					var cartJson = await response.Content.ReadAsStringAsync();
					var cartItem = JsonConvert.DeserializeObject<Cart>(cartJson);
					UpdateUIForCartItems(cartItem?.Quantity ?? 0);
				}
				else
				{
					UpdateUIForCartItems(0);
				}

				// Теперь проверяем, добавлен ли товар в избранное
				var favoriteResponse = await httpClient.GetAsync($"http://localhost:5099/CheckIfFavorited?userId={userId}&productId={product.ProductId}");

				if (favoriteResponse.IsSuccessStatusCode)
				{
					var json = await favoriteResponse.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<dynamic>(json);

					bool isFavorited = result.isFavorited;

					// Обновляем UI для избранного
					UpdateUIForFavorite(isFavorited);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
			}
		}
		private void UpdateUIForFavorite(bool isFavorited)
		{
			// Находим Ellipse внутри шаблона кнопки
			var ellipse = FavoriteButton.Template.FindName("FavoriteEllipse", FavoriteButton) as Ellipse;
			if (ellipse == null) return;

			var imageBrush = ellipse.Fill as ImageBrush;
			if (imageBrush == null) return;

			string newImagePath = isFavorited
				? "pack://application:,,,/Assets/SelectedProduct.png"
				: "pack://application:,,,/Assets/FavoriteProduct.png";

			imageBrush.ImageSource = new BitmapImage(new Uri(newImagePath));
		}
		private void UpdateUIForCartItems(int totalQuantity)
		{
			if (totalQuantity > 0)
			{
				BAddToCart.Visibility = Visibility.Collapsed;
				BMinus.Visibility = Visibility.Visible;
				BPlus.Visibility = Visibility.Visible;
				TextboxPoint.Visibility = Visibility.Visible;

				BBuyNow.Visibility = Visibility.Collapsed;
				BGoToCart.Visibility = Visibility.Visible;

				TextboxPoint.Text = totalQuantity.ToString();
			}

		}
		private async void LoadImageFromApi(int productId)
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductImage/{productId}");

				if (response.IsSuccessStatusCode)
				{
					var imageBytes = await response.Content.ReadAsByteArrayAsync();
					LoadProductImage(imageBytes);
				}
				else
				{
					image.Source = null;
				}
			}
			catch (Exception ex)
			{
				image.Source = null;
				Console.WriteLine($"Ошибка при загрузке изображения: {ex.Message}");
			}
		}
		private void LoadProductImage(object imageData)
		{
			if (imageData == null)
			{
				image.Source = null;
				return;
			}

			byte[] byteArray;

			// Если данные приходят в формате Base64
			if (imageData is string base64String)
			{
				try
				{
					byteArray = Convert.FromBase64String(base64String);
				}
				catch
				{
					MessageBox.Show("Ошибка при преобразовании изображения.");
					return;
				}
			}
			else if (imageData is byte[] bytes)
			{
				byteArray = bytes;
			}
			else
			{
				image.Source = null;
				return;
			}

			if (byteArray == null || byteArray.Length == 0)
			{
				image.Source = null;
				return;
			}

			var bitmapImage = new BitmapImage();
			using (var mem = new MemoryStream(byteArray))
			{
				mem.Position = 0;
				bitmapImage.BeginInit();
				bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.UriSource = null;
				bitmapImage.StreamSource = mem;
				bitmapImage.EndInit();
			}
			bitmapImage.Freeze();
			image.Source = bitmapImage;
		}
		private async void ButtonAddToCart(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser == 0)
			{
				MessageBox.Show("Для добавления в корзину необходимо авторизоваться");
			}
			else
			{
				int userId = Session.IdUser;
				int productId = _product.ProductId;
				int quantity = 1;

				try
				{
					// Проверяем, есть ли товар уже в корзине
					var cartItemResponse = await httpClient.GetAsync($"http://localhost:5099/CheckCartItem?userId={userId}&productId={productId}");

					if (cartItemResponse.IsSuccessStatusCode)
					{
						var cartItemExists = await cartItemResponse.Content.ReadFromJsonAsync<bool>();

						if (cartItemExists)
						{
							MessageBox.Show("Товар добавлен в корзину.");
							UpdateUIForInCartState();
							return;
						}
					}
					else if (cartItemResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						MessageBox.Show("Эндпоинт проверки товара в корзине не найден. Проверьте API.");
						return;
					}

					var response = await httpClient.PostAsync($"http://localhost:5099/AddToCart?userId={userId}&productId={productId}&quantity={quantity}", null);

					if (response.IsSuccessStatusCode)
					{
						MessageBox.Show("Товар добавлен в корзину.");
						UpdateUIForInCartState();
					}
					else
					{
						var errorResponse = await response.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка при добавлении товара в корзину: {errorResponse}");
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при добавлении товара в корзину: {ex.Message}");
				}
			}
			
		}
		private async void ButtonMinus(object sender, RoutedEventArgs e)
		{
			try
			{
				int currentQuantity = ExtractNumber(TextboxPoint.Text);
				int maxStock = ExtractNumber(labelcount.Content.ToString());

				if (currentQuantity > 1)
				{
					int newQuantity = currentQuantity - 1;

					int userId = Session.IdUser;
					int productId = _product.ProductId;

					var url = $"http://localhost:5099/UpdateCartItem?userId={userId}&productId={productId}&quantity={newQuantity}";

					var response = await httpClient.PutAsync(url, null);

					if (response.IsSuccessStatusCode)
					{
						TextboxPoint.Text = newQuantity.ToString();
					}
					else
					{
						var errorResponse = await response.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка: {errorResponse}");
					}
				}
				else
				{
					MessageBoxResult result = MessageBox.Show("Хотите удалить товар из корзины?", "Подтверждение", MessageBoxButton.YesNo);

					if (result == MessageBoxResult.Yes)
					{
						int userId = Session.IdUser;
						int productId = _product.ProductId;

						var url = $"http://localhost:5099/DeleteFromCart?userId={userId}&productId={productId}";

						var response = await httpClient.DeleteAsync(url);

						if (response.IsSuccessStatusCode)
						{
							MessageBox.Show("Товар удален из корзины.");
							UpdateUIForNotInCartState();
						}
						else
						{
							var errorResponse = await response.Content.ReadAsStringAsync();
							MessageBox.Show($"Ошибка: {errorResponse}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при обновлении или удалении товара: {ex.Message}");
			}
		}
		private async void ButtonPlus(object sender, RoutedEventArgs e)
		{
			try
			{
				int currentQuantity = ExtractNumber(TextboxPoint.Text);
				int maxStock = ExtractNumber(labelcount.Content.ToString());

				if (currentQuantity < maxStock)
				{
					int newQuantity = currentQuantity + 1;

					int userId = Session.IdUser;
					int productId = _product.ProductId;
					var url = $"http://localhost:5099/UpdateCartItem?userId={userId}&productId={productId}&quantity={newQuantity}";

					var response = await httpClient.PutAsync(url, null);

					if (response.IsSuccessStatusCode)
					{
						TextboxPoint.Text = newQuantity.ToString();
					}
					else
					{
						var errorResponse = await response.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка: {errorResponse}");
					}
				}
				else
				{
					MessageBox.Show("Вы выбрали максимальное количество продукта");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при обновлении количества товара: {ex.Message}");
			}
		}
		private void ButtonGoToCart(object sender, RoutedEventArgs e)
		{
			//это окно закроется
			new CartWindow().Show();
		}
		private void UpdateUIForInCartState()
		{
			// Состояние: товар уже в корзине
			BBuyNow.Visibility = Visibility.Collapsed;
			BAddToCart.Visibility = Visibility.Collapsed;
			BGoToCart.Visibility = Visibility.Visible;
			BMinus.Visibility = Visibility.Visible;
			BPlus.Visibility = Visibility.Visible;
			TextboxPoint.Visibility = Visibility.Visible;
			TextboxPoint.Text = "1"; // Устанавливаем начальное значение
		}
		private void UpdateUIForNotInCartState()
		{
			// Состояние: товар не в корзине
			BBuyNow.Visibility = Visibility.Visible;
			BAddToCart.Visibility = Visibility.Visible;
			BGoToCart.Visibility = Visibility.Collapsed;
			BMinus.Visibility = Visibility.Collapsed;
			BPlus.Visibility = Visibility.Collapsed;
			TextboxPoint.Visibility = Visibility.Collapsed;
			TextboxPoint.Text = string.Empty; // Очищаем текстовое поле
		}
		private void ProductUserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			new ProductInfoWindow(_product).Show();
			//это окно закроется

		}
		private async void ButtonBuyNow(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser == 0)
			{
				MessageBox.Show("Для покупки необходимо авторизоваться");
				return;
			}

			int userId = Session.IdUser;
			int productId = _product.ProductId;
			int quantity = 1;

			try
			{
				// Проверяем, есть ли товар уже в корзине
				var cartItemResponse = await httpClient.GetAsync($"http://localhost:5099/CheckCartItem?userId={userId}&productId={productId}");

				if (cartItemResponse.IsSuccessStatusCode)
				{
					var cartItemExists = await cartItemResponse.Content.ReadFromJsonAsync<bool>();

					if (!cartItemExists)
					{
						var response = await httpClient.PostAsync(
							$"http://localhost:5099/AddToCart?userId={userId}&productId={productId}&quantity={quantity}",
							null
						);

						if (response.IsSuccessStatusCode)
						{
							MessageBox.Show("Товар добавлен в корзину.");
						}
						else
						{
							var errorResponse = await response.Content.ReadAsStringAsync();
							MessageBox.Show($"Ошибка при добавлении товара в корзину: {errorResponse}");
							return;
						}
					}
					UpdateUIForInCartState();
				}
				else if (cartItemResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					MessageBox.Show("Эндпоинт проверки товара в корзине не найден. Проверьте API.");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при добавлении товара в корзину: {ex.Message}");
				return;
			}

			// Покупка — всегда открываем окно после успешной обработки
			new PurchaseWindow(productId).Show();
		}
		private async void TextboxPoint_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!isUserInput) return;

			if (Session.IdUser == 0)
			{
				return;
			}

			int userId = Session.IdUser;
			int productId = myproductId;

			if (productId <= 0)
			{
				return;
			}

			// Проверяем ввод
			if (string.IsNullOrWhiteSpace(TextboxPoint.Text))
			{
				// Если текст пустой, ничего не делаем
				return;
			}

			if (!int.TryParse(TextboxPoint.Text, out int quantity) || quantity <= 0)
			{
				MessageBox.Show("Введите корректное количество.");
				return;
			}

			// Проверяем, не превышает ли количество остаток на складе
			if (quantity > product.Stock)
			{
				MessageBox.Show($"Нельзя заказать больше {product.Stock} шт.");
				isUserInput = false;
				TextboxPoint.Text = product.Stock.ToString(); // Возвращаем максимальное допустимое значение
				isUserInput = true;
				return;
			}

			try
			{
				var url = $"http://localhost:5099/UpdateCartItem?userId={userId}&productId={productId}&quantity={quantity}";
				var response = await httpClient.PutAsync(url, null);

				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка обновления: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Сетевая ошибка: {ex.Message}");
			}
		}
		private void TextboxPoint_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!char.IsDigit(e.Text, 0))
			{
				e.Handled = true;
			}
		}
		private void TextboxPoint_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				var text = (string)e.DataObject.GetData(typeof(string));
				if (!int.TryParse(text, out int result) || result <= 0)
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}
		private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			// Получаем продукт из DataContext
			
			// Находим Ellipse внутри шаблона кнопки
			var ellipse = button.Template.FindName("FavoriteEllipse", button) as Ellipse;
			if (ellipse == null) return;

			// Получаем ImageBrush
			var imageBrush = ellipse.Fill as ImageBrush;
			if (imageBrush == null) return;

			int userId = Session.IdUser;
			if (userId == 0)
			{
				MessageBox.Show("Вы не авторизованы.");
				return;
			}

			try
			{
				int productId = product.ProductId;

				// Проверяем текущее состояние — добавлено или нет
				bool isAlreadyFavorited = imageBrush.ImageSource.ToString().Contains("SelectedProduct.png");

				string url;

				if (isAlreadyFavorited)
				{
					// Удалить из избранного
					url = $"http://localhost:5099/RemoveFromFavorite?userId={userId}&productId={productId}";
				}
				else
				{
					// Добавить в избранное
					url = $"http://localhost:5099/AddToFavorite?userId={userId}&productId={productId}";
				}

				// Отправляем GET-запрос
				var response = await httpClient.GetAsync(url);

				if (response.IsSuccessStatusCode)
				{
					// Меняем изображение
					string newImagePath = isAlreadyFavorited
						? "pack://application:,,,/Assets/FavoriteProduct.png"
						: "pack://application:,,,/Assets/SelectedProduct.png";

					imageBrush.ImageSource = new BitmapImage(new Uri(newImagePath));
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка сети: {ex.Message}");
			}
		}
		private int ExtractNumber(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return 0;

			var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
			if (match.Success && int.TryParse(match.Value, out int result))
			{
				return result;
			}

			return 0; // Возвращаем 0, если число не найдено
		}
	}
}
