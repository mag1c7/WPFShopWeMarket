using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
using System.Windows.Threading;
namespace NewWpfShop.AdminUserControls.UserControls
{
    /// <summary>
    /// Логика взаимодействия для PurchaseUserControl.xaml
    /// </summary>
    public partial class PurchaseUserControl : UserControl
    {
		private DispatcherTimer textUpdateTimer = new DispatcherTimer();
		private int pendingQuantity;
		public event EventHandler<int> ProductRemoved;
		public delegate void DataPassedEventHandler(object sender, DataPassedEventArgsPurchase e);
		public event DataPassedEventHandler DataPassed;
		private int myproductId;
		private int originalStock;
		private static readonly HttpClient httpClient = new HttpClient();
		private Product product;
		private bool isUserInput = true;
		private bool isInitialized = false;
		public Product _product
		{
			get => product;
			set
			{
				product = value;
				if (product != null)
				{
					labelName.Content = product.Name;
					labelPrice.Content = product.Price + " Р";
					originalStock = product.Stock;
					LoadProductStockFromApi(product.ProductId);

					LoadImageFromApi(product.ProductId);
					LoadCartItemQuantity(product.ProductId); ;
					isInitialized = true;
				}
			}
		}
		public PurchaseUserControl(Product product)
		{
			InitializeComponent();
			_product = product;
			myproductId = product.ProductId;

			// Настройка таймера
			textUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
			textUpdateTimer.Tick += async (s, e) =>
			{
				textUpdateTimer.Stop();
				await UpdateCartItemOnServer(pendingQuantity);
			};

			this.Loaded += async (s, e) => await LoadCartItems();
		}
		private async void LoadCartItemQuantity(int productId)
		{
			int userId = Session.IdUser;
			if (userId == 0) return;

			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/TakeInfoCartItem?userId={userId}&productId={productId}");
				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					var cartItem = JsonConvert.DeserializeObject<CartItem>(content);

					UpdateUIForCartItems(cartItem?.Quantity ?? 0);
				}
				else
				{
					UpdateUIForCartItems(0);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке данных о корзине: " + ex.Message);
			}
		}

		private void UpdateUIForCartItems(int totalQuantity)
		{
			if (totalQuantity > 0)
			{
				BMinus.Visibility = Visibility.Visible;
				BPlus.Visibility = Visibility.Visible;
				TextboxPoint.Visibility = Visibility.Visible;
				TextboxPoint.Text = totalQuantity.ToString();
			}
			else
			{
				BMinus.Visibility = Visibility.Collapsed;
				BPlus.Visibility = Visibility.Collapsed;
				TextboxPoint.Visibility = Visibility.Collapsed;
				TextboxPoint.Text = string.Empty;
			}
		}
		private async Task LoadCartItems()
		{
			int userId = Session.IdUser;
			if (userId == 0) return;

			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/TakeInfoCartItem?userId={userId}&productId={myproductId}");

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

				var favoriteResponse = await httpClient.GetAsync($"http://localhost:5099/CheckIfFavorited?userId={userId}&productId={myproductId}");

				if (favoriteResponse.IsSuccessStatusCode)
				{
					var json = await favoriteResponse.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<dynamic>(json);

					bool isFavorited = result.isFavorited;

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
		private async void ButtonMinus(object sender, RoutedEventArgs e)
		{
			try
			{
				int currentQuantity = ExtractNumber(TextboxPoint.Text);
				int maxStock = ExtractNumber(TextboxPoint.Text.ToString());

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
						OnDataPassed("QuantityUpdated");
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
							ProductRemoved?.Invoke(this, productId); // ✅ Добавлено: оповещаем о удалении
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
				// Получаем текущее количество товара из TextBox
				if (!int.TryParse(TextboxPoint.Text, out int currentQuantity))
				{
					MessageBox.Show("Некорректное значение количества.");
					return;
				}

				// Получаем максимальный остаток
				if (!int.TryParse(labelStock.Content?.ToString(), out int maxStock))
				{
					MessageBox.Show("Не удалось определить максимальный остаток.");
					return;
				}

				if (currentQuantity < maxStock)
				{
					int newQuantity = currentQuantity + 1;
					int userId = Session.IdUser;
					int productId = _product.ProductId;

					var url = $"http://localhost:5099/UpdateCartItem?userId={userId}&productId={productId}&quantity={newQuantity}";
					var response = await httpClient.PutAsync(url, null);

					if (response.IsSuccessStatusCode)
					{
						// Обновляем TextBox напрямую
						TextboxPoint.Text = newQuantity.ToString();

						// Если используется таймер для отложенного обновления — сбрасываем его
						if (textUpdateTimer.IsEnabled)
						{
							textUpdateTimer.Stop();
							textUpdateTimer.Start(); // Перезапускаем таймер
						}

						// Оповещаем подписчиков об изменении количества
						OnDataPassed("QuantityUpdated");
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
		private void TextboxPoint_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!isInitialized || !isUserInput || Session.IdUser == 0)
				return;

			if (!int.TryParse(TextboxPoint.Text, out int quantity) || quantity <= 0)
			{
				MessageBox.Show("Введите корректное количество.");
				isUserInput = false;
				TextboxPoint.Text = "1";
				isUserInput = true;
				return;
			}

			if (quantity > originalStock)
			{
				MessageBox.Show($"Нельзя заказать больше {originalStock} шт.");
				isUserInput = false;
				TextboxPoint.Text = originalStock.ToString();
				isUserInput = true;
				return;
			}

			pendingQuantity = quantity;
			textUpdateTimer.Stop(); // Останавливаем предыдущий таймер
			textUpdateTimer.Start(); // Запускаем новый с задержкой
		}
		private async Task UpdateCartItemOnServer(int quantity)
		{
			int userId = Session.IdUser;
			int productId = product.ProductId;

			try
			{
				var url = $"http://localhost:5099/UpdateCartItem?userId={userId}&productId={productId}&quantity={quantity}";
				var response = await httpClient.PutAsync(url, null);

				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка обновления: {error}");
				}
				else
				{
					OnDataPassed("QuantityUpdated");
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
		private async void LoadProductStockFromApi(int productId)
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductStock?productId={productId}");
				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					if (int.TryParse(content, out int stock))
					{
						originalStock = stock;
						labelStock.Content = stock.ToString();
					}
					else
					{
						MessageBox.Show("Ошибка: получено некорректное значение остатка.");
					}
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка получения остатка: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Сетевая ошибка при получении остатка: {ex.Message}");
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
			var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+");
			if (match.Success)
			{
				return int.Parse(match.Value);
			}
			throw new FormatException("Не удалось извлечь число из строки.");
		}
		private void OnDataPassed(string data)
		{
			DataPassed?.Invoke(this, new DataPassedEventArgsPurchase { Data = data });
		}
		public async Task DeleteFromCart(int userId, int productId)
		{
			try
			{
				var url = $"http://localhost:5099/DeleteFromCart?userId={userId}&productId={productId}";
				var response = await httpClient.DeleteAsync(url);
				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Продукт удален из корзины.");
					ProductRemoved?.Invoke(this, productId);
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка при удалении из корзины: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Сетевая ошибка: {ex.Message}");
			}
		}
		private void ButtonDeleteFromCart(object sender, RoutedEventArgs e)
		{
			DeleteFromCart(Session.IdUser, product.ProductId);
		}
	}
}
public class DataPassedEventArgsPurchase : EventArgs
{
	public string Data { get; set; }
}