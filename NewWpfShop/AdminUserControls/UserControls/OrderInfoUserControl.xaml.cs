using Newtonsoft.Json;
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

namespace NewWpfShop.AdminUserControls.UserControls
{
	/// <summary>
	/// Логика взаимодействия для OrderInfoUserControl.xaml
	/// </summary>
	public partial class OrderInfoUserControl : UserControl
	{
		private OrderItem _orderItem;
		private static readonly HttpClient httpClient = new HttpClient();
		public OrderInfoUserControl(OrderItem orderItem)
		{
			InitializeComponent();
			OrderItem = orderItem;
		}

		public OrderItem OrderItem
		{
			get => _orderItem;
			set
			{
				_orderItem = value;

				if (_orderItem != null)
				{
					var product = _orderItem.Product;

					if (product != null)
					{
						labelName.Content = product.Name;
						labelPrice.Content = $"{_orderItem.Price:F2} ₽";
						labelCoast.Content = $"{_orderItem.Quantity} шт.";
						LoadImageFromApi(_orderItem.ProductId);
					}
					else
					{
						int productId = _orderItem.ProductId;
						LoadProductAsync(productId);
					}

					// Если OrderId > 0 → проверяем выдачу
					//if (_orderItem.OrderId > 0)
					//{
					//	CheckIfIssuedAsync(_orderItem.OrderId);
					//}
					//else
					//{
					//	labelIssued.Content = "⚠ Заказ не найден";
					//}
				}
			}
		}
		private async void CheckIfIssuedAsync(int orderId)
		{
			if (orderId <= 0)
			{
				labelIssued.Content = "⚠ Нет данных";
				return;
			}

			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetOrderDetailss?orderId={orderId}");
				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					var order = JsonConvert.DeserializeObject<Order>(json);

					if (order?.PickupDate.HasValue == true)
					{
						labelIssued.Content = "✅ Выдан";
						labelIssued.Foreground = Brushes.Green;
					}
					else
					{
						labelIssued.Content = "❌ Не выдан";
						labelIssued.Foreground = Brushes.Red;
					}
				}
				else
				{
					labelIssued.Content = "⚠ Неизвестно";
				}
			}
			catch (Exception ex)
			{
				labelIssued.Content = "Ошибка загрузки";
				MessageBox.Show("Ошибка при определении статуса выдачи: " + ex.Message);
			}
		}
		private async void LoadProductAsync(int productId)
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductById?id={productId}");
				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					var product = JsonConvert.DeserializeObject<Product>(json);

					Application.Current.Dispatcher.Invoke(() =>
					{
						labelName.Content = product?.Name ?? "Неизвестный товар";
						labelPrice.Content = $"{_orderItem.Price:F2} ₽";
						labelCoast.Content = $"{_orderItem.Quantity} шт.";
						LoadImageFromApi(productId);
					});
				}
				else
				{
					labelName.Content = "Ошибка загрузки";
				}
			}
			catch (Exception ex)
			{
				labelName.Content = $"Ошибка: {ex.Message}";
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
	}
}
