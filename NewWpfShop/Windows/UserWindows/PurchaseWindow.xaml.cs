using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using NewWpfShop.AdminUserControls.UserControls;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.GeneralWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Words.NET;
using Path = System.IO.Path;
namespace NewWpfShop.Windows.UserWindows
{
	/// <summary>
	/// Логика взаимодействия для PurchaseWindow.xaml
	/// </summary>
	public partial class PurchaseWindow : Window
	{
		private bool _isCategoryInitialized = false;
		private List<ComboBoxItemModel> _pickupPointItems;
		private ObservableCollection<PurchaseUserControl> _cartControls = new ObservableCollection<PurchaseUserControl>();
		private List<ComboBoxItemModel> _categoryItems;
		private List<Product> _allProducts = new();
		private int? _selectedCategoryId = null;
		private IEnumerable<Product> FilteredProducts =>
			_selectedCategoryId switch
			{
				null => _allProducts.Where(p => p.Stock > 0 && !p.IsDeleted),
				int id when id > 0 => _allProducts.Where(p => p.CategoryId == id && p.Stock > 0 && !p.IsDeleted),
				_ => _allProducts.Where(p => p.Stock > 0 && !p.IsDeleted)
			};
		private string _sortOrder;
		private bool isMenuVisible = false;
		private readonly int _productId;
		private static readonly HttpClient httpClient = new HttpClient();
		private ProductshopwmContext _context = new ProductshopwmContext();

		// Конструктор получает productId
		public PurchaseWindow(int productId)
		{
			InitializeComponent();
			int userId = Session.IdUser;

			var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

			if (user != null)
			{
				UpdateUIForAuthenticationStatus(true, user.Name, user.Surname, user.Email, user.ImageUser);
			}
			else
			{
				UpdateUIForAuthenticationStatus(false);
			}

			ButtonAuth.Visibility = Session.IdUser == 0 ? Visibility.Visible : Visibility.Collapsed;
			ButtonAccountUser.Visibility = Session.IdUser == 0 ? Visibility.Collapsed : Visibility.Visible;
			
			_productId = productId;
			ListViewProducts.ItemsSource = _cartControls;

			Loaded += async (s, e) => await LoadPickupPointsAsync();
			Loaded += async (s, e) => await LoadProduct();
			LoadCategoriesAsync();
			cmbPointPickUp.ItemsSource = _pickupPointItems; // Привязываем данные
			cmbPointPickUp.SelectedValuePath = "Id";
			cmbPointPickUp.DisplayMemberPath = "Name";
			CloseAllWindowsExceptLast();
		}
		private void UpdateUIForAuthenticationStatus(bool isAuthenticated, string name = null, string surname = null, string email = null, byte[] imageBytes = null)
		{
			if (Session.IdUser != 0)
			{
				// Пользователь авторизован
				string displayName = string.IsNullOrWhiteSpace(Session.CurrentUser) ? "no name" : Session.CurrentUser;
				labelname.Text = $"{displayName}";
				labelemail.Text = email ?? "";
				ButtonExit.Visibility = Visibility.Visible;
				if (imageBytes != null)
				{
					Session.LoadProductImage(imageBytes, image1);
				}
				else
				{
					image1.Source = new BitmapImage(new Uri("/Assets/DefaultImage.png", UriKind.Relative));
				}

				ButtonExit.Content = "Выйти из аккаунта";
				LabelWelcome.Visibility = Visibility.Visible;
			}
			else
			{
				// Пользователь не авторизован
				labelname.Text = "Войдите в аккаунт";
				labelemail.Text = "";
				image1.Source = new BitmapImage(new Uri("/Assets/DefaultImage.png", UriKind.Relative));
				ButtonExit.Content = "Войти в аккаунт";
				LabelWelcome.Visibility = Visibility.Collapsed;
			}
		}
		public async Task LoadCategoriesAsync()
		{
			try
			{
				using var httpClient = new HttpClient();
				var categories = await httpClient.GetFromJsonAsync<List<Category>>("http://localhost:5099/GetAllCategories");

				if (categories != null)
				{
					_isCategoryInitialized = false; // 🔐 блокируем SelectionChanged временно

					_categoryItems = new List<ComboBoxItemModel>
			{
				new ComboBoxItemModel { Id = 0, DisplayText = "По умолчанию" }
			};

					foreach (var category in categories)
					{
						_categoryItems.Add(new ComboBoxItemModel
						{
							Id = category.CategoryId,
							DisplayText = category.CategoryName
						});
					}

					cmbCategory.Items.Clear();
					cmbCategory.ItemsSource = _categoryItems;
					cmbCategory.DisplayMemberPath = "DisplayText";
					cmbCategory.SelectedValuePath = "Id";
					cmbCategory.SelectedValue = 0;

					_isCategoryInitialized = true; // 🔓 теперь можно слушать изменения
				}
				else
				{
					MessageBox.Show("Категории не найдены.");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Ошибка при загрузке категорий.");
			}
		}

		private void CloseAllWindowsExceptLast()
		{
			var windows = Application.Current.Windows.Cast<Window>().ToList();
			if (windows.Count <= 1) return;

			Window lastWindow = this;

			foreach (var window in windows)
			{
				if (window != lastWindow)
				{
					window.Close();
				}
			}
		}
		private async Task LoadProduct()
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductById?id={_productId}");
				if (!response.IsSuccessStatusCode)
				{
					MessageBox.Show("Не удалось загрузить товар.");
					return;
				}

				var json = await response.Content.ReadAsStringAsync();
				var product = JsonConvert.DeserializeObject<Product>(json);

				var productControl = new PurchaseUserControl(product);
				productControl.ProductRemoved += (s, id) =>
				{
					var toRemove = _cartControls.FirstOrDefault(c => c._product.ProductId == id);
					if (toRemove != null)
						_cartControls.Remove(toRemove);
				};

				productControl.DataPassed += (s, e) =>
				{
					UpdateTotalStockAndPrice();
					UpdateBuyButtonState(); // ✅ Обновляем состояние кнопки
				};

				_cartControls.Add(productControl);
				UpdateTotalStockAndPrice();
				UpdateBuyButtonState(); // ✅ Устанавливаем начальное состояние кнопки
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке товара: " + ex.Message);
			}
		}
		public async Task LoadPickupPointsAsync()
		{
			try
			{
				using var httpClient = new HttpClient();
				var pickupPoints = await httpClient.GetFromJsonAsync<List<PickupPoint>>("http://localhost:5099/GetAllPickupPoints");

				if (pickupPoints != null)
				{
					_pickupPointItems = pickupPoints
						.Select(p => new ComboBoxItemModel { Id = p.PickupPointId, DisplayText = p.Address })
						.ToList();

					cmbPointPickUp.ItemsSource = _pickupPointItems;
					cmbPointPickUp.DisplayMemberPath = "DisplayText";
					cmbPointPickUp.SelectedValuePath = "Id";
					cmbPointPickUp.SelectedIndex = 0;
				}
				else
				{
					MessageBox.Show("Пункты выдачи не найдены.");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Ошибка при загрузке пунктов выдачи.");
			}
		}

		private void BackToHome_Click(object sender, RoutedEventArgs e)
		{
			new BuyerWindow().Show();
			this.Close();
		}
		private void ButtonOrders(object sender, RoutedEventArgs e)
		{
			new OrderWindow().Show();
			this.Close();
		}
		private void ButtonFavorit(object sender, RoutedEventArgs e)
		{
			new FavoriteWindow().Show();
			this.Close();
		}
		private void ButtonCart(object sender, RoutedEventArgs e)
		{
			new CartWindow().Show();
			this.Close();
		}
		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			double targetRight = isMenuVisible ? -210 : 0;
			DoubleAnimation animation = new DoubleAnimation
			{
				From = Canvas.GetRight(SlidingMenu),
				To = targetRight,
				Duration = new Duration(TimeSpan.FromSeconds(0.3))
			};
			SlidingMenu.BeginAnimation(Canvas.RightProperty, animation);
			isMenuVisible = !isMenuVisible;
		}
		private void ButtonInfoUser(object sender, RoutedEventArgs e)
		{
			new AccountInformationWindow().ShowDialog();
		}
		private void ButtonInfo_Click(object sender, RoutedEventArgs e)
		{
			new InfoProgrammWindow().Show();
			this.Close();
		}
		private void ButtonExit_Click(object sender, RoutedEventArgs e)
		{
			Session.IsAuthenticated = false;
			Session.IsAdmin = false;
			Session.IdUser = 0;
			Session.EmailUser = "";
			Session.CurrentUser = "";
			Session.IsAuthenticated = false;
			new AuthorizationWindow().Show();
			this.Close();
		}
		private void ButtonClose(object sender, RoutedEventArgs e)
		{
			isMenuVisible = false;
		}
		private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			new BuyerWindow().Show();
			this.Close();
		}
		private void ButtonAccountUser_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser != 0)
			{
				new AccountInformationWindow().ShowDialog();
			}
		}
		private async void ButtonBuy(object sender, RoutedEventArgs e)
		{
			int userId = Session.IdUser;
			if (userId == 0)
			{
				MessageBox.Show("Вы не авторизованы.");
				return;
			}

			if (!(checkBoxRule.IsChecked ?? false) || !(checkBoxProcessing.IsChecked ?? false))
			{
				MessageBox.Show("Необходимо принять правила и согласие на обработку данных.");
				return;
			}

			// Получаем выбранный пункт выдачи
			var selectedItem = cmbPointPickUp.SelectedItem as ComboBoxItemModel;
			if (selectedItem == null || selectedItem.Id <= 0)
			{
				MessageBox.Show("Выберите пункт выдачи.");
				return;
			}
			int pickupPointId = selectedItem.Id;

			// Проверяем наличие товара в ObservableCollection
			if (_cartControls == null || _cartControls.Count == 0)
			{
				MessageBox.Show("Товар не найден.");
				return;
			}

			var control = _cartControls.FirstOrDefault();
			if (control == null)
			{
				MessageBox.Show("Продукт не найден.");
				return;
			}

			if (!int.TryParse(control.TextboxPoint.Text, out int quantity) || quantity <= 0)
			{
				MessageBox.Show("Некорректное количество.");
				return;
			}

			var product = control._product;
			if (product == null)
			{
				MessageBox.Show("Продукт не найден.");
				return;
			}

			decimal totalSum = product.Price * quantity;

			var checkDetails = new List<OrderItemDetail>
	{
		new OrderItemDetail
		{
			ProductId = product.ProductId,
			ProductName = product.Name,
			Price = product.Price,
			Quantity = quantity
		}
	};

			try
			{
				// Отправляем запрос на создание заказа
				var createOrderResponse = await httpClient.PostAsJsonAsync(
					"http://localhost:5099/CreateOrderFromCartt",
					new { UserId = userId, PickupPointId = pickupPointId });

				if (!createOrderResponse.IsSuccessStatusCode)
				{
					var errorText = await createOrderResponse.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка создания заказа: {errorText}");
					return;
				}

				var resultJson = await createOrderResponse.Content.ReadAsStringAsync();
				dynamic result = JsonConvert.DeserializeObject(resultJson);
				int orderId = result.orderId;

				// Очищаем корзину (если нужна очистка)
				var clearCartResponse = await httpClient.DeleteAsync($"http://localhost:5099/ClearCart?userId={userId}");
				if (!clearCartResponse.IsSuccessStatusCode)
				{
					MessageBox.Show("Не удалось очистить корзину.");
				}

				string pickupPointName = selectedItem.DisplayText;
				CheckSave(orderId, userId, DateTime.Now, totalSum, checkDetails, pickupPointName);


				// ✅ Чистим ObservableCollection, а не ListView.Items
				_cartControls.Clear();

				MessageBox.Show("Заказ успешно оформлен. Чек отправлен на почту.");
				UpdateBuyButtonState();
				stockProduct.Content = "0 шт";
				labelPrice.Content = "0 Р";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}");
			}
		}
		public static void CheckSave(int orderId, int userId, DateTime saleDate, decimal totalSum, List<OrderItemDetail> checkDetails, string pickupPointName)
		{
			try
			{
				using var httpClient = new HttpClient();

				// Получаем данные пользователя
				var userResponse = httpClient.GetAsync($"http://localhost:5099/GetUserById?userId={userId}").Result;
				if (!userResponse.IsSuccessStatusCode) throw new Exception("Пользователь не найден");

				var userJson = userResponse.Content.ReadAsStringAsync().Result;
				var user = JsonConvert.DeserializeObject<User>(userJson);
				string userEmail = user.Email;

				// Путь к шаблону
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string templatePath = Path.Combine(baseDir, "..", "..", "..", "Assets", "PatternChecks", "CheckSH.docx");
				templatePath = Path.GetFullPath(templatePath);

				// Временный файл Word
				string tempDocPath = Path.Combine(Path.GetTempPath(), $"temp_check_{Guid.NewGuid()}.docx");

				// Путь для сохранения PDF (временный)
				string tempPdfPath = Path.Combine(Path.GetTempPath(), $"check_order_{orderId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

				string checksFolder = Path.Combine(baseDir, "..", "..", "..", "Assets", "Checks");
				checksFolder = Path.GetFullPath(checksFolder); // Нормализуем путь

				Directory.CreateDirectory(checksFolder); // Создаём папку, если её нет

				// Генерируем имя файла в нужном формате
				string finalPdfFileName = $"check_{orderId:D6}.pdf";
				string finalPdfPath = Path.Combine(checksFolder, finalPdfFileName);

				// Копируем шаблон во временную директорию
				File.Copy(templatePath, tempDocPath, overwrite: true);

				// Работаем с Word-документом
				using (var doc = DocX.Load(tempDocPath))
				{
					doc.ReplaceText("<Date>", saleDate.ToString("dd.MM.yyyy HH:mm:ss"));
					doc.ReplaceText("<NumberCheck>", orderId.ToString("D6").ToUpper());
					doc.ReplaceText("<SumProducts>", totalSum.ToString("F2"));
					doc.ReplaceText("<PickUpPoint>", pickupPointName);
					// Находим таблицу
					var table = doc.Tables.FirstOrDefault(t => t.ColumnCount == 5 && t.RowCount >= 1);
					if (table == null)
					{
						MessageBox.Show("Таблица с товарами не найдена в шаблоне.");
						return;
					}

					// Добавляем товары в таблицу
					foreach (var item in checkDetails)
					{
						var row = table.InsertRow();

						row.Cells[0].Paragraphs[0].Append(item.ProductId.ToString()); // ID товара
						row.Cells[1].Paragraphs[0].Append(item.ProductName);         // Название
						row.Cells[2].Paragraphs[0].Append(item.Price.ToString("F2")); // Цена
						row.Cells[3].Paragraphs[0].Append(item.Quantity.ToString());  // Кол.
						row.Cells[4].Paragraphs[0].Append((item.Price * item.Quantity).ToString("F2")); // Сумма
					}

					// Удаляем первую строку (плейсхолдер), если она есть
					if (table.RowCount > 1)
					{
						table.RemoveRow(0);
					}
					// Генерируем QR-код
					string qrImagePath = QrCodeHelper.GenerateQrCode(orderId);
					if (string.IsNullOrEmpty(qrImagePath)) throw new Exception("Не удалось создать QR-код");

					// Ищем "qr-код" и вставляем изображение
					bool qrFound = false;
					foreach (var paragraph in doc.Paragraphs)
					{
						if (paragraph.Text.Contains("qr-код", StringComparison.OrdinalIgnoreCase))
						{
							//paragraph.Clear(); // очищаем старый текст

							var image = doc.AddImage(qrImagePath);
							var picture = image.CreatePicture(80, 80);

							var qrParagraph = doc.InsertParagraph();
							qrParagraph.AppendPicture(picture);
							qrParagraph.Alignment = Xceed.Document.NET.Alignment.center;

							qrFound = true;
							break;
						}
					}

					if (!qrFound)
					{
						MessageBox.Show("⚠️ Место для QR-кода в шаблоне не найдено ('qr-код')");
					}

					doc.Save();
				}



				// Сохраняем временный PDF
				using (var document = new Spire.Doc.Document())
				{
					document.LoadFromFile(tempDocPath, Spire.Doc.FileFormat.Auto);
					document.SaveToFile(tempPdfPath, Spire.Doc.FileFormat.PDF);
				}

				// Очищаем временный Word-файл
				File.Delete(tempDocPath);

				// 🚀 Вызываем метод удаления оценочной надписи
				RemoveEvaluationWarningFromPdf(tempPdfPath, finalPdfPath);
				SendPdfEmail(Session.EmailUser, finalPdfPath);
				// Удаляем временный PDF после очистки
				if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"❌ Ошибка при формировании чека: {ex.Message}");
			}
		}
		private static void RemoveEvaluationWarningFromPdf(string inputPdfPath, string outputPdfPath)
		{
			try
			{
				// Открываем исходный PDF напрямую
				using (PdfReader reader = new PdfReader(inputPdfPath))
				{
					using (FileStream fs = new FileStream(outputPdfPath, FileMode.Create, FileAccess.Write))
					{
						Document pdfDoc = new Document();
						PdfWriter writer = PdfWriter.GetInstance(pdfDoc, fs);
						pdfDoc.Open();

						PdfContentByte cb = writer.DirectContent;

						float cropY = 23f; // Сдвиг содержимого вверх (~20 пикселей)

						for (int i = 1; i <= reader.NumberOfPages; i++)
						{
							// Размеры страницы: 14см x 21.5см → ~397x610 пунктов
							iTextSharp.text.Rectangle newPageSize = new iTextSharp.text.Rectangle(0, cropY, 397, 610);
							pdfDoc.SetPageSize(newPageSize);
							pdfDoc.NewPage();

							PdfImportedPage page = writer.GetImportedPage(reader, i);
							cb.AddTemplate(page, 0, cropY); // Сдвигаем вверх
						}

						pdfDoc.Close();
					}
				}

				Console.WriteLine("Оценочный водяной знак успешно удален.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при удалении водяного знака: {ex.Message}");
			}
		}
		public static void SendPdfEmail(string email, string pdfFilePath)
		{
			if (!File.Exists(pdfFilePath))
			{
				Console.WriteLine("PDF файл не найден.");
				return;
			}

			try
			{

				MailAddress fromEmail = new MailAddress("mbydin@mail.ru", "Maksim");
				MailAddress toEmail = new MailAddress(email);
				MailMessage mail = new MailMessage(fromEmail, toEmail)
				{
					Subject = "Ваш PDF - Чек",
					Body = "Благодарим за покупку!",
					IsBodyHtml = true
				};

				Attachment attachment = new Attachment(pdfFilePath);
				mail.Attachments.Add(attachment);

				SmtpClient smtpClient = new SmtpClient("smtp.mail.ru", 587)
				{
					Credentials = new NetworkCredential("mbydin@mail.ru", "vAhkCaFp634Uyjww4htz"),
					EnableSsl = true
				};

				smtpClient.Send(mail);
				Console.WriteLine("PDF файл успешно отправлен на почту.");
			}
			catch (SmtpException ex)
			{
				Console.WriteLine($"Ошибка при отправке письма: {ex.Message}");
			}
		}

		private void ProductCartControl_ProductRemoved(PurchaseUserControl productControl)
		{
			productControl.ProductRemoved += (s, id) =>
			{
				var toRemove = _cartControls.FirstOrDefault(c => c._product?.ProductId == id);
				if (toRemove != null)
				{
					_cartControls.Remove(toRemove);
					UpdateBuyButtonState();
					UpdateTotalStockAndPrice();
				}
			};
		}
		private void OnDataPassedFromUserControl(object sender, DataPassedEventArgsPurchase e)
		{
			UpdateTotalStockAndPrice();
		}
		private void UpdateTotalStockAndPrice(IEnumerable<PurchaseUserControl> productControls)
		{
			int totalItems = 0;
			decimal totalPrice = 0;

			foreach (var control in productControls)
			{
				if (!int.TryParse(control.TextboxPoint.Text, out int quantity) || quantity <= 0)
					continue;

				if (control._product == null)
					continue;

				totalPrice += control._product.Price * quantity;
				totalItems += quantity;
			}

			stockProduct.Content = $"{totalItems} шт.";
			labelPrice.Content = $"{totalPrice:F2} Р";
		}
		private void UpdateTotalStockAndPrice()
		{
			if (_cartControls == null || !_cartControls.Any())
			{
				stockProduct.Content = "0 шт.";
				labelPrice.Content = "0,00 Р";
				return;
			}

			decimal totalPrice = 0;
			int totalItems = 0;

			foreach (var control in _cartControls)
			{
				if (!int.TryParse(control.TextboxPoint.Text, out int quantity) || quantity <= 0)
					continue;

				if (control._product == null)
					continue;

				totalPrice += control._product.Price * quantity;
				totalItems += quantity;
			}

			stockProduct.Content = $"{totalItems} шт.";
			labelPrice.Content = $"{totalPrice:F2} Р";
		}
		private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isCategoryInitialized)
				return;

			if (cmbCategory.SelectedItem is ComboBoxItemModel selected && selected.Id != 0)
			{
				new BuyerWindow().Show();
				this.Close();
			}
		}

		private void UpdateBuyButtonState()
		{
			if (_cartControls == null || _cartControls.Count == 0)
			{
				buttonbuyy.IsEnabled = false;
				buttonbuyy.Background = Brushes.Gray;
			}
			else
			{
				buttonbuyy.IsEnabled = true;
				buttonbuyy.Background = (Brush)new BrushConverter().ConvertFrom("#0356FF");
			}
		}
		private void ButtonAuth_Click(object sender, RoutedEventArgs e)
		{
			new AuthorizationWindow().Show();
			this.Close();
		}
		private void cmbPointPickUp_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cmbPointPickUp.SelectedItem is ComboBoxItemModel item && item.Id == -1)
			{
				if (_pickupPointItems.Any(x => x.Id == -1))
				{
					var filteredItems = _pickupPointItems.Where(i => i.Id != -1).ToList();
					cmbPointPickUp.ItemsSource = filteredItems;
				}
			}
		}
	}
}
