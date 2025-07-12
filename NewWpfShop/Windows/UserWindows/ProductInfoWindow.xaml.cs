using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.GeneralWindows;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace NewWpfShop.Windows.UserWindows
{
	/// <summary>
	/// Логика взаимодействия для ProductInfoWindow.xaml
	/// </summary>
	public partial class ProductInfoWindow : Window
	{
		private bool _isCategoryInitialized = false;
		private ImageSource originalMainImageSource;
		int myproductid;
		private static readonly HttpClient httpClient = new HttpClient();
		private readonly Product _product;
		private bool isMenuVisible = false;
		private ProductshopwmContext _context = new ProductshopwmContext();
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
		public ProductInfoWindow(Product product)
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
				buttonbuyn.Visibility = Visibility.Collapsed;
				buttonaddc.Visibility = Visibility.Collapsed;
			}
			LoadCategoriesAsync();
			ButtonAuth.Visibility = Session.IdUser == 0 ? Visibility.Visible : Visibility.Collapsed;
			ButtonAccountUser.Visibility = Session.IdUser == 0 ? Visibility.Collapsed : Visibility.Visible;
			_product = product;
			myproductid = _product.ProductId;
			LoadProductData();

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
		private void LoadProductData()
		{
			labelName.Content = _product.Name;
			labelId.Content = $"Артикул: {_product.ProductId}";
			labelDesc.Text = $"Описание: {_product.Description}";
			labelPrice.Content = $"Цена: {_product.Price} Р";
			labelStock.Content = $"В наличии: {_product.Stock} шт.";
			labelSupplier.Content = $"Поставщик: {_product.Supplier}";
			labelCountryOfOrigin.Content = $"Страна производства: {_product.CountryOfOrigin}";
			LoadImages();
			
		}
		private async void LoadImages()
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductImages/{_product.ProductId}");

				if (response.IsSuccessStatusCode)
				{
					var images = await response.Content.ReadFromJsonAsync<List<byte[]>>();

					if (images != null && images.Any())
					{
						LoadImage(image, images.ElementAtOrDefault(0));
						LoadImage(imagep1, images.ElementAtOrDefault(1));
						LoadImage(imagep2, images.ElementAtOrDefault(2));
						LoadImage(imagep3, images.ElementAtOrDefault(3));
						originalMainImageSource = image.Source;
					}
				}
				else
				{
					MessageBox.Show("Ошибка при загрузке изображений.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		private void LoadImage(Image imageControl, byte[] imageData)
		{
			if (imageData == null) return;

			var bitmapImage = new BitmapImage();
			using (var mem = new MemoryStream(imageData))
			{
				mem.Position = 0;
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = mem;
				bitmapImage.EndInit();
			}
			bitmapImage.Freeze();
			imageControl.Source = bitmapImage;

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
		private void ButtonBuyNow(object sender, RoutedEventArgs e)
		{
			new PurchaseWindow(myproductid).Show();
			this.Close();
		}
		private async void ButtonAddToCart(object sender, RoutedEventArgs e)
		{
			int userId = Session.IdUser;
			//int productId = _product.ProductId;

			int quantity = 1;

			try
			{
				// Проверяем, есть ли товар уже в корзине
				var cartItemResponse = await httpClient.GetAsync($"http://localhost:5099/CheckCartItem?userId={userId}&productId={myproductid}");

				if (cartItemResponse.IsSuccessStatusCode)
				{
					var cartItemExists = await cartItemResponse.Content.ReadFromJsonAsync<bool>();

					if (cartItemExists)
					{
						MessageBox.Show("Товар добавлен в корзину.");
						//UpdateUIForInCartState();
						return;
					}
				}
				else if (cartItemResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					MessageBox.Show("Эндпоинт проверки товара в корзине не найден. Проверьте API.");
					return;
				}

				var response = await httpClient.PostAsync($"http://localhost:5099/AddToCart?userId={userId}&productId={myproductid}&quantity={quantity}", null);

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Товар добавлен в корзину.");
					//UpdateUIForInCartState();
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
		private void ButtonAccountUser_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser != 0)
			{
				new AccountInformationWindow().ShowDialog();
			}
		}
		private void Thumbnail_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is Image thumbnail && thumbnail.Source != null)
			{
				image.Source = thumbnail.Source;
			}
		}
		private void Thumbnail_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			image.Source = originalMainImageSource;
		}
		private void ButtonAuth_Click(object sender, RoutedEventArgs e)
		{
			new AuthorizationWindow().Show();
			this.Close();
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
	}
}
