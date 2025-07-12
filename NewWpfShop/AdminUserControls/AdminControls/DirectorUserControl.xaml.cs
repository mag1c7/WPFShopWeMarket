using Newtonsoft.Json;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.DirectorWindows;
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
namespace NewWpfShop.AdminUserControls.AdminControls
{
	/// <summary>
	/// Логика взаимодействия для DirectorUserControl.xaml
	/// </summary>
	public partial class DirectorUserControl : UserControl
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
					labelid.Content = product.ProductId;
					labelname.Content = product.Name;
					labelprice.Content = product.Price + " Р";
					labelcount.Content = product.Stock + " Шт.";
					myproductId = product.ProductId;
					LoadImageFromApi(product.ProductId);
					if (product.IsDeleted)
					{
						BChangeProduct.Content = "Восстановить";
						BChangeProduct.Background = Brushes.Gray;
						BChangeProduct.IsEnabled = true;
					}
					else
					{
						BChangeProduct.Content = "Изменить товар";
						BChangeProduct.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0356FF"));
						BChangeProduct.IsEnabled = true;
					}
				}
			}
		}
		public DirectorUserControl(Product product)
		{
			InitializeComponent();
			_product = product;
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
		private async void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (_product == null)
			{
				MessageBox.Show("Не удалось определить удаляемый товар.");
				return;
			}
			var result = MessageBox.Show(
				$"Вы уверены, что хотите удалить товар \"{_product.Name}\"?",
				"Подтверждение удаления",
				MessageBoxButton.YesNo,MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					var response = await httpClient.PutAsync($"http://localhost:5099/DeleteProduct/{_product.ProductId}", null);

					if (response.IsSuccessStatusCode)
					{
						MessageBox.Show("Товар успешно удалён.");

						_product.IsDeleted = true;

						BChangeProduct.Content = "Восстановить";
						BChangeProduct.Background = Brushes.Gray;
						BChangeProduct.IsEnabled = true;
					}
					else
					{
						var errorText = await response.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка при удалении товара:\n{errorText}");
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Не удалось подключиться к серверу:\n{ex.Message}");
				}
			}
		}
		private async void ButtonChangeProduct_Click(object sender, RoutedEventArgs e)
		{
			if (_product == null)
			{
				MessageBox.Show("Товар не найден.");
				return;
			}
			if (_product.IsDeleted)
			{
				var result = MessageBox.Show(
					$"Вы уверены, что хотите восстановить товар \"{_product.Name}\"?",
					"Восстановление товара",
					MessageBoxButton.YesNo,MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					try
					{
						var response = await httpClient.PutAsync($"http://localhost:5099/RestoreProduct/{_product.ProductId}", null);
						if (response.IsSuccessStatusCode)
						{
							MessageBox.Show("Товар успешно восстановлен.");
							_product.IsDeleted = false;
							BChangeProduct.Content = "Изменить товар";
							BChangeProduct.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0356FF"));
							BChangeProduct.IsEnabled = true;
						}
						else
						{
							var error = await response.Content.ReadAsStringAsync();
							MessageBox.Show($"Ошибка восстановления товара:\n{error}");
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Ошибка подключения:\n{ex.Message}");
					}
				}
			}
			else
			{
				new EditProductWindow(_product).ShowDialog();
			}
		}
	}
}
