using Microsoft.EntityFrameworkCore.Infrastructure;
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
namespace NewWpfShop.AdminUserControls.UserControls
{
    /// <summary>
    /// Логика взаимодействия для FavoriteUserControl.xaml
    /// </summary>
    public partial class FavoriteUserControl : UserControl
    {
		private int myproductId;
		private Product product;
		private static readonly HttpClient httpClient = new HttpClient();
		public Product _product
		{
			get => product;
			set
			{
				product = value;
				myproductId = product.ProductId;
				labelname.Content = product.Name;
				labelprice.Content = product.Price + " Р";
				labelcount.Content = product.Stock + " Шт.";
				LoadImageFromApi(product.ProductId);
			}
		}
		public FavoriteUserControl(Product product)
        {
            InitializeComponent();
			_product = product;
			this.Loaded += async (s, e) => await LoadFavorioteItems(myproductId);
		}
		private async Task LoadFavorioteItems(int productId)
		{
			try
			{
				var favoriteResponse = await httpClient.GetAsync($"http://localhost:5099/CheckIfFavorited?userId={Session.IdUser}&productId={productId}");
				if (favoriteResponse.IsSuccessStatusCode)
				{
					var json = await favoriteResponse.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<dynamic>(json);

					bool isFavorited = result?.isFavorited ?? false;

					UpdateUIForFavorite(isFavorited);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при проверке избранного: " + ex.Message);
			}
		}
		private void UpdateUIForFavorite(bool isFavorited)
		{
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
			int userId = Session.IdUser;
			int productId = _product.ProductId;
			int quantity = 1;
			try
			{
				var cartItemResponse = await httpClient.GetAsync($"http://localhost:5099/CheckCartItem?userId={userId}&productId={productId}");
				if (cartItemResponse.IsSuccessStatusCode)
				{
					var cartItemExists = await cartItemResponse.Content.ReadFromJsonAsync<bool>();
					if (cartItemExists)
					{
						MessageBox.Show("Товар добавлен в корзину.");
						BGoToCart.Visibility = Visibility.Visible;
						BAddToCart.Visibility = Visibility.Collapsed;
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
					BGoToCart.Visibility = Visibility.Visible;
					BAddToCart.Visibility = Visibility.Collapsed;
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
		private void ProductUserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			new ProductInfoWindow(_product).Show();
		}

		private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;
			var ellipse = button.Template.FindName("FavoriteEllipse", button) as Ellipse;
			if (ellipse == null) return;
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
				bool isAlreadyFavorited = imageBrush.ImageSource.ToString().Contains("SelectedProduct.png");
				string url;

				if (isAlreadyFavorited)
				{
					url = $"http://localhost:5099/RemoveFromFavorite?userId={userId}&productId={productId}";
				}
				else
				{
					url = $"http://localhost:5099/AddToFavorite?userId={userId}&productId={productId}";
				}
				var response = await httpClient.GetAsync(url);
				if (response.IsSuccessStatusCode)
				{
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
		private void ButtonAddGoToCart(object sender, RoutedEventArgs e)
		{
			new CartWindow().Show();
		}
	}
}
