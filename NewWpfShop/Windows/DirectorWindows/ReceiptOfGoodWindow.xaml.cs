using Microsoft.Win32;
using NewWpfShop.AdminUserControls.AdminControls;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.GeneralWindows;
using NewWpfShop.Windows.UserWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.ObjectModel;
using System.Windows;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Windows.Documents;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using Xceed.Words.NET;
using NewWpfShop.AdminUserControls.UserControls;
using Category = NewWpfShop.DataBase.Category;
using Newtonsoft.Json;
namespace NewWpfShop.Windows.DirectorWindows
{
	/// <summary>
	/// Логика взаимодействия для ReceiptOfGoodWindow.xaml
	/// </summary>
	public partial class ReceiptOfGoodWindow : Window
	{
		private static readonly HashSet<string> UploadedReceipts = new HashSet<string>();

		private bool _isCategoryInitialized = false;
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
		private static readonly HttpClient httpClient = new HttpClient();
		private bool isMenuVisible = false;
		private ProductshopwmContext _context = new ProductshopwmContext();
		public ReceiptOfGoodWindow()
		{
			InitializeComponent();
			if (Session.IsAdmin = true)
			{
				ButtonAuth.Visibility = Visibility.Visible;
				ButtonAccountUser.Visibility = Visibility.Collapsed;
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
		private void BackToHome_Click(object sender, RoutedEventArgs e)
		{
			new DirectorWindow().Show();
			this.Close();
		}
		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			double targetRight = isMenuVisible ? -260 : 0;
			DoubleAnimation animation = new DoubleAnimation
			{
				From = Canvas.GetRight(SlidingMenu),
				To = targetRight,
				Duration = new Duration(TimeSpan.FromSeconds(0.3))
			};
			SlidingMenu.BeginAnimation(Canvas.RightProperty, animation);
			isMenuVisible = !isMenuVisible;
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
		private void ButtonAccountUser_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser != 0)
			{
				new AccountInformationWindow().ShowDialog();
			}
		}
		private void ButtonAuth_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser == 0)
			{
				new AuthorizationWindow().Show();
				this.Close();
			}
		}
		private void ButtonAddProduct_click(object sender, RoutedEventArgs e)
		{
			new AddEditProductWindow().ShowDialog();
		}
		private void ButtonAddCategory_click(object sender, RoutedEventArgs e)
		{
			new AddEditCategoryWindow().ShowDialog();
		}
		private void ButtonReceiptOfGoods_Click(object sender, RoutedEventArgs e)
		{
			new ReceiptOfGoodWindow().Show();
			this.Close();
		}
		private ObservableCollection<ProductDataGrid> ReadTableDataFromDocx(string filePath)
		{
			var products = new ObservableCollection<ProductDataGrid>();

			try
			{
				using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
				{
					var body = doc.MainDocumentPart.Document.Body;

					foreach (var element in body.ChildElements)
					{
						if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
						{
							// Получаем все строки таблицы
							var rows = table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();

							// Пропускаем первые 2 строки — заголовки
							for (int i = 2; i < rows.Count - 2; i++) // Пропускаем первые 2 и последние 2
							{
								var row = rows[i];

								var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
											   .Select(c => c.InnerText.Trim())
											   .ToList();

								if (cells.Count < 15)
									continue;

								string numText = cells[0];          // №
								string name = cells[1];            // Наименование
								string quantityText = cells[9];     // Количество (столбец 10)
								string priceText = cells[10];       // Цена без НДС (столбец 11)
								string vatPercentText = cells[12].Replace("%", "").Trim(); // % НДС (столбец 13)

								// Фильтруем ненужные строки
								if (string.IsNullOrWhiteSpace(name) ||
									name.Contains("наименование", StringComparison.OrdinalIgnoreCase) ||
									name.Contains("характеристик", StringComparison.OrdinalIgnoreCase) ||
									name.Contains("итого", StringComparison.OrdinalIgnoreCase) ||
									name.Contains("всего", StringComparison.OrdinalIgnoreCase))
								{
									continue;
								}

								// Парсим номер
								int num = int.TryParse(numText, out int parsedNum) ? parsedNum : products.Count + 1;

								// Парсим количество и цену
								decimal quantity = ParseDecimal(quantityText);
								decimal price = ParseDecimal(priceText);

								if (quantity <= 0 || price <= 0)
									continue;

								// Парсим ставку НДС
								decimal vatRate = decimal.TryParse(vatPercentText, out decimal vat) ? vat : 20;

								// Рассчитываем суммы
								decimal amount = quantity * price;
								decimal vatAmount = amount * (vatRate / 100);
								decimal totalAmount = amount + vatAmount;

								// Добавляем товар в коллекцию
								products.Add(new ProductDataGrid
								{
									Num = num,
									Nomenclature = name,
									Quantity = quantity,
									PurchasePrice = price,
									Amount = amount,
									VATPercentage = vatRate.ToString(),
									VATAmount = vatAmount,
									TotalAmount = totalAmount
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			return products;
		}

		private void ButtonAddFromFile(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "Word Documents (*.docx)|*.docx",
				Title = "Выберите файл Word"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					// 1. Извлекаем данные из документа: поставщик, получатель, номер накладной, дата
					ExtractDocumentData(openFileDialog.FileName);

					// 2. Загружаем товары из документа
					var allProducts = ReadTableDataFromDocx(openFileDialog.FileName);

					// 3. Удаляем первые 2 строки (заголовки)
					var afterHeaderRemoved = RemoveFirstTwoRows(allProducts);

					// 4. Удаляем последние 2 строки (итоги)
					var finalProducts = RemoveLastTwoRows(afterHeaderRemoved);

					// 5. Привязываем к DataGrid
					dataGrid.ItemsSource = finalProducts;

					MessageBox.Show("Данные успешно загружены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		private ObservableCollection<ProductDataGrid> RemoveFirstTwoRows(ObservableCollection<ProductDataGrid> products)
		{
			if (products == null || products.Count == 0)
				return products;

			products.RemoveAt(0);
			return products;
		}
		private ObservableCollection<ProductDataGrid> RemoveLastTwoRows(ObservableCollection<ProductDataGrid> products)
		{
			if (products == null || products.Count == 0)
				return products;

			products.RemoveAt(products.Count - 1);
			return products;
		}
		private void ExtractDocumentData(string filePath)
		{
			try
			{
				using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
				{
					var body = doc.MainDocumentPart.Document.Body;

					string receiptNumber = null; // Номер документа
					string date = null;         // Дата составления

					// Ищем таблицу с нужными заголовками
					foreach (var element in body.ChildElements)
					{
						if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
						{
							bool foundHeaderRow = false;

							foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
							{
								var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
											   .Select(c => c.InnerText.Trim())
											   .ToList();

								if (!foundHeaderRow)
								{
									// Проверяем, является ли эта строка заголовочной
									if (cells.Contains("Номер документа") && cells.Contains("Дата составления"))
									{
										foundHeaderRow = true;
										continue;
									}
								}
								else
								{
									// Это строка с данными
									if (cells.Count >= 2)
									{
										receiptNumber = cells[1];//number
										date = cells[2];//data
										
										break;
									}
								}
							}
						}
					}

					textboxReceipt.Text = receiptNumber ?? "Не найден";
					textboxNum.Text = date ?? "Не найдена";
					textboxProvider.Text = "ООО Поставщик";
					textboxRecipient.Text = "ООО WeMarker";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка чтения файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private decimal ParseDecimal(string value)
		{
			value = value?.Replace(" ", "").Replace(",", ".") ?? "0";
			return decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result) ? result : 0;
		}
		private async void ButtonSave(object sender, RoutedEventArgs e)
		{
			try
			{
				var productsToSave = dataGrid.ItemsSource as ObservableCollection<ProductDataGrid>;
				if (productsToSave == null || productsToSave.Count == 0)
				{
					MessageBox.Show("Таблица пуста. Нечего сохранять.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// --- Проверка на дубликат по номеру накладной и дате ---
				string receiptKey = $"{textboxReceipt.Text.Trim()}|{textboxNum.Text.Trim()}";
				string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeMarket");
				string uploadedFilePath = System.IO.Path.Combine(appFolder, "uploaded_receipts.txt");

				if (!Directory.Exists(appFolder))
				{
					Directory.CreateDirectory(appFolder);
				}

				if (File.Exists(uploadedFilePath))
				{
					var lines = File.ReadAllLines(uploadedFilePath);
					if (lines.Contains(receiptKey))
					{
						MessageBox.Show("Эта накладная уже была загружена ранее.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
						return;
					}
				}

				string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
				string templatePath = System.IO.Path.Combine(baseDirectory, "Assets", "Documents", "PatternRegistrationProducts.docx");

				if (!File.Exists(templatePath))
				{
					MessageBox.Show($"Шаблон не найден по пути:\n{templatePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				string outputDirectory = System.IO.Path.Combine(baseDirectory, "SavedDocuments");
				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}
				string outputPath = System.IO.Path.Combine(outputDirectory, $"Оприходование_товаров_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

				using (var document = DocX.Load(templatePath))
				{
					foreach (var paragraph in document.Paragraphs)
					{
						if (paragraph.Text.Contains("Оприходование товаров №"))
						{
							string receiptNumber = textboxReceipt.Text.Trim();
							string date = textboxNum.Text.Trim();
							string newText = $"Оприходование товаров № {receiptNumber} от {date}";
							paragraph.ReplaceText("Оприходование товаров № ", newText);
						}
					}

					var table = document.Tables.FirstOrDefault();
					if (table == null)
					{
						MessageBox.Show("Таблица не найдена в шаблоне!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
						return;
					}

					foreach (var product in productsToSave)
					{
						var row = table.InsertRow();
						row.Cells[0].Paragraphs.First().Append(product.Num.ToString());
						row.Cells[1].Paragraphs.First().Append(product.Nomenclature ?? "Не указано");
						row.Cells[2].Paragraphs.First().Append(product.Quantity.ToString("N2"));
						row.Cells[3].Paragraphs.First().Append(product.PurchasePrice.ToString("C2"));
						row.Cells[4].Paragraphs.First().Append(product.TotalAmount.ToString("C2"));
					}

					decimal totalAmount = productsToSave.Sum(p => p.TotalAmount);
					int totalItems = productsToSave.Count;

					foreach (var paragraph in document.Paragraphs)
					{
						if (paragraph.Text.Contains("Итого:"))
						{
							paragraph.ReplaceText("Итого: 0 руб.", $"Итого: {totalAmount:C2}");
						}

						if (paragraph.Text.Contains("Всего наименований"))
						{
							paragraph.ReplaceText("Всего наименований 0, на сумму руб.",
								$"Всего наименований {totalItems}, на сумму {totalAmount:C2}");
						}

						if (paragraph.Text.Contains("Ноль рублей 00 копеек"))
						{
							string amountInWords = ConvertToWords(totalAmount);
							paragraph.ReplaceText("Ноль рублей 00 копеек", amountInWords);
						}
					}

					document.SaveAs(outputPath);
					MessageBox.Show($"Документ успешно создан:\n{outputPath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				}

				await SaveProductsToApi();

				File.AppendAllLines(uploadedFilePath, new[] { receiptKey });
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private async Task SaveProductsToApi()
		{
			try
			{
				if (dataGrid.Items.Count == 0)
				{
					MessageBox.Show("Таблица пуста. Нечего сохранять.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				string imagePath = @"Assets\DefaultImage.png";
				string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);

				foreach (var item in dataGrid.Items)
				{
					var type = item.GetType();

					int productId = GetPropertyValue<int>(type.GetProperty("Num")?.GetValue(item));
					string name = GetPropertyValue<string>(type.GetProperty("Nomenclature")?.GetValue(item));
					decimal price = GetPropertyValue<decimal>(type.GetProperty("PurchasePrice")?.GetValue(item));
					int stock = GetPropertyValue<int>(type.GetProperty("Quantity")?.GetValue(item));

					if (productId <= 0 || string.IsNullOrWhiteSpace(name) || price <= 0 || stock < 0)
					{
						continue;
					}

					// Проверка наличия товара по ID
					var stockResponse = await httpClient.GetAsync($"http://localhost:5099/GetProductStock?productId={productId}");

					if (stockResponse.IsSuccessStatusCode)
					{
						// Товар уже существует — обновляем
						var updatedProduct = new
						{
							ProductId = productId,
							Name = name,
							Description = "Обновлён через накладную",
							Price = price,
							Stock = stock,
							CategoryId = 1,
							Supplier = "Обновлён через накладную",
							CountryOfOrigin = "Обновлён через накладную"
						};

						var json = JsonConvert.SerializeObject(updatedProduct);
						var content = new StringContent(json, Encoding.UTF8, "application/json");

						await httpClient.PutAsync("http://localhost:5099/ChangeProduct", content);
					}
					else
					{
						var multipart = new MultipartFormDataContent
				{
					{ new StringContent(name), "Name" },
					{ new StringContent("Добавлен через накладную"), "Description" },
					{ new StringContent(price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price" },
					{ new StringContent(stock.ToString()), "Stock" },
					{ new StringContent("Добавлен через накладную"), "Supplier" },
					{ new StringContent("Добавлен через накладную"), "CountryOfOrigin" },
					{ new StringContent(DateTime.UtcNow.ToString("yyyy-MM-dd")), "ExpirationDate" },
					{ new StringContent("1"), "CategoryId" }
				};

						if (File.Exists(fullPath))
						{
							byte[] imageBytes = File.ReadAllBytes(fullPath);
							var byteContent = new ByteArrayContent(imageBytes);
							byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
							multipart.Add(byteContent, "Image", "default.jpg");
						}

						await httpClient.PostAsync("http://localhost:5099/AddSimpleProduct", multipart);
					}
				}

				MessageBox.Show("Все товары успешно обработаны.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}




		private T GetPropertyValue<T>(object value)
		{
			if (value == null)
				return default;

			// Если требуется строка — безопасно вызываем ToString()
			if (typeof(T) == typeof(string))
				return (T)(object)value.ToString();

			// Для числовых типов используем Convert
			if (typeof(T) == typeof(decimal) || typeof(T) == typeof(int) ||
				typeof(T) == typeof(double) || typeof(T) == typeof(float))
				return (T)Convert.ChangeType(value, typeof(T));

			// Во всех остальных случаях возвращаем как есть
			return (T)value;
		}
		private string ConvertToWords(decimal amount)
		{
			string[] units = { "ноль", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
			string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
			string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
			string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

			string ConvertPart(int part)
			{
				if (part == 0) return "";

				int h = part / 100;
				int t = (part % 100) / 10;
				int u = part % 10;

				string result = "";

				if (h > 0) result += hundreds[h] + " ";
				if (t == 1)
				{
					result += teens[u] + " ";
				}
				else
				{
					if (t > 1) result += tens[t] + " ";
					if (u > 0) result += units[u] + " ";
				}

				return result.Trim();
			}

			int rubles = (int)Math.Floor(amount);
			int kopecks = (int)Math.Round((amount - rubles) * 100);

			string rublesInWords = ConvertPart(rubles);
			string kopecksInWords = ConvertPart(kopecks);

			string rublesWord = rubles == 1 ? "рубль" : (rubles >= 2 && rubles <= 4 ? "рубля" : "рублей");
			string kopecksWord = kopecks == 1 ? "копейка" : (kopecks >= 2 && kopecks <= 4 ? "копейки" : "копеек");

			return $"{rublesInWords} {rublesWord} {kopecksInWords} {kopecksWord}";
		}
		private void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			new DirectorWindow().Show();
			this.Close();
		}
		private void ButtonDear(object sender, RoutedEventArgs e)
		{
			var sorted = FilteredProducts.OrderByDescending(p => p.Price);
			DisplayProducts(sorted);
		}

		private void ButtonDefault(object sender, RoutedEventArgs e)
		{
			DisplayProducts(FilteredProducts);
		}

		private void ButtonLow(object sender, RoutedEventArgs e)
		{
			var sorted = FilteredProducts.OrderBy(p => p.Price);
			DisplayProducts(sorted);
		}

		private void DisplayProducts(IEnumerable<Product> products)
		{
		}
		private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			new DirectorWindow().Show();
			this.Close();
		}
		private void ButtonMakePurchase_Click(object sender, RoutedEventArgs e)
		{
			new DirectorOrdersWindow().Show();
			this.Close();
		}
	}
}
public class SimpleProductModel
{
	public string Name { get; set; }
	public decimal Price { get; set; }
	public int Stock { get; set; }
	public string Description { get; set; } = "Добавлен через приходную накладную";
	public string Supplier { get; set; } = "1";
	public string CountryOfOrigin { get; set; } = "1";
	public string ExpirationDate { get; set; } = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd");
	public int CategoryId { get; set; } = 1;
}