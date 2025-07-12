using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
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
using Xceed.Words.NET;
using Path = System.IO.Path;
namespace NewWpfShop.AdminUserControls.UserControls
{
	/// <summary>
	/// Логика взаимодействия для OrderUserControl.xaml
	/// </summary>
	public partial class OrderUserControl : UserControl
	{
		private Order _order;
		private static readonly HttpClient httpClient = new HttpClient();
		public OrderUserControl(Order order)
		{
			InitializeComponent();
			Order = order;
			if (Session.IsAdmin != true)
			{
				btnMake.Visibility = Visibility.Collapsed;
				btnCancel.Visibility = Visibility.Collapsed;
			}
		}
		public Order Order
		{
			get => _order;
			set
			{
				_order = value;
				if (_order == null) return;

				_order = LoadOrderWithDetails(_order.OrderId);

				// Номер заказа
				labelID.Content = $"#{_order.OrderId}";

				// Статус заказа и цвет
				labelStatus.Content = _order.PaymentStatus;
				switch (_order.PaymentStatus)
				{
					case "В процессе":
						labelStatus.Foreground = Brushes.Red;
						break;
					case "Выдан":
						labelStatus.Foreground = Brushes.Green;
						break;
					case "Отменён":
						labelStatus.Foreground = Brushes.Black;
						break;
					default:
						labelStatus.Foreground = Brushes.Gray;
						break;
				}

				// Скрываем кнопки, если заказ выдан или отменён
				if (_order.PaymentStatus == "Выдан" || _order.PaymentStatus == "Отменён")
				{
					btnMake.Visibility = Visibility.Collapsed;
					btnCancel.Visibility = Visibility.Collapsed;
				}
				else if (Session.IsAdmin)
				{
					btnMake.Visibility = Visibility.Visible;
					btnCancel.Visibility = Visibility.Visible;
				}

				// Дата и время заказа
				labelDate.Content = _order.OrderDate.ToString("dd.MM.yyyy");
				labelTime.Content = _order.OrderDate.ToString("HH:mm");

				// Пункт самовывоза или доставка
				if (!_order.IsPickup)
					labelPickup.Content = "Доставка";
				else if (_order.PickupPoint != null)
					labelPickup.Content = $"{_order.PickupPoint.Address}";
				else
					labelPickup.Content = "Пункт самовывоза: не указан";

				// Общая сумма
				labelTotal.Content = $"{_order.Total:F2} ₽";

				// Список товаров
				listview.Items.Clear();
				if (_order.OrderItems != null && _order.OrderItems.Any())
				{
					foreach (var item in _order.OrderItems)
					{
						var infoControl = new OrderInfoUserControl(item);
						listview.Items.Add(infoControl);
					}
				}

				// Количество товаров
				int itemCount = _order.OrderItems?.Sum(i => i.Quantity) ?? 0;
				ShowMenuButton.Content = GetItemCountText(itemCount);

				// Дата выдачи
				if (_order.PaymentStatus == "Выдан" && _order.PickupDate.HasValue)
				{
					labelTime.Visibility = Visibility.Visible;
					labelTime.Content = $"{_order.PickupDate.Value:dd.MM.yyyy}";
				}
				else
				{
					labelTime.Visibility = Visibility.Visible;
					labelTime.Content = "-";
				}
			}
		}
		public Order LoadOrderWithDetails(int orderId)
		{
			using (var context = new ProductshopwmContext())
			{
				return context.Orders
					.Include(o => o.PickupPoint)
					.Include(o => o.OrderItems)
						.ThenInclude(i => i.Product)
					.FirstOrDefault(o => o.OrderId == orderId);
			}
		}

		private string GetItemCountText(int count)
		{
			if (count == 0)
				return "Нет товаров";

			int n = count % 100;
			if (n >= 11 && n <= 14)
				return $"{count} товаров";

			switch (count % 10)
			{
				case 1: return $"{count} товар";
				case 2:
				case 3:
				case 4: return $"{count} товара";
				default: return $"{count} товаров";
			}
		}

		private void ShowOrHideMenu_Click(object sender, RoutedEventArgs e)
		{
			FormPopup.IsOpen = !FormPopup.IsOpen;
		}
		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button)
			{
				// Получаем текст нажатой кнопки
				string menuItemText = button.Content.ToString();

				// Выводим сообщение о выбранном пункте
				MessageBox.Show($"Вы выбрали: {menuItemText}");
			}
		}
		private void Checkchek_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser == 0)
			{
				MessageBox.Show("Вы не авторизованы.");
				return;
			}

			if (_order == null || _order.OrderId <= 0)
			{
				MessageBox.Show("Не удалось определить заказ.");
				return;
			}
			int orderId = _order.OrderId;
			try
			{
				string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
				string checksFolder = Path.Combine(projectRoot, "Assets", "Checks");
				string formattedOrderId = orderId.ToString("D6");
				string checkFileName = $"check_{formattedOrderId}.pdf";
				string checkFilePath = Path.Combine(checksFolder, checkFileName);
				if (!File.Exists(checkFilePath))
				{
					MessageBox.Show($"Чек не найден:\n{checkFilePath}");
					return;
				}
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = checkFilePath,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при открытии чека: {ex.Message}");
			}
		}
		private async void MakeOrder_Click(object sender, RoutedEventArgs e)
		{
			if (_order == null)
			{
				MessageBox.Show("Заказ не выбран.");
				return;
			}

			try
			{
				var response = await httpClient.PostAsJsonAsync(
					$"http://localhost:5099/ConfirmDeliveryy?orderId={_order.OrderId}", new { });

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("✅ Заказ успешно выдан.");

					labelStatus.Content = "Выдан";
					labelStatus.Foreground = Brushes.Green;

					// 🔽 Скрываем кнопки
					btnMake.Visibility = Visibility.Collapsed;
					btnCancel.Visibility = Visibility.Collapsed;
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"❌ Ошибка при выдаче: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		private async void CancelOrder_Click(object sender, RoutedEventArgs e)
		{
			if (_order == null)
			{
				MessageBox.Show("Заказ не выбран.");
				return;
			}

			try
			{
				var response = await httpClient.PostAsJsonAsync(
					$"http://localhost:5099/CancelOrder?orderId={_order.OrderId}", new { });

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("❌ Заказ успешно отменён.");

					labelStatus.Content = "Отменён";
					labelStatus.Foreground = Brushes.Black;

					// 🔽 Скрываем кнопки
					btnMake.Visibility = Visibility.Collapsed;
					btnCancel.Visibility = Visibility.Collapsed;
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка при отмене заказа: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
	}
}
