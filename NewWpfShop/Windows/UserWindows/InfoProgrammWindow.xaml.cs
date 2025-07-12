using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.GeneralWindows;
using System;
using System.Collections.Generic;
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
	/// Логика взаимодействия для InfoProgrammWindow.xaml
	/// </summary>
	public partial class InfoProgrammWindow : Window
	{
		private bool _isCategoryInitialized = false;
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
		public InfoProgrammWindow()
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
			LoadCategoriesAsync();
			ButtonAuth.Visibility = Session.IdUser == 0 ? Visibility.Visible : Visibility.Collapsed;
			ButtonAccountUser.Visibility = Session.IdUser == 0 ? Visibility.Collapsed : Visibility.Visible;
		}
		private void UpdateUIForAuthenticationStatus(bool isAuthenticated, string name = null, string surname = null, string email = null, byte[] imageBytes = null)
		{
			if (Session.IdUser != 0)
			{
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
					_isCategoryInitialized = false;

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

					_isCategoryInitialized = true; 
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
			new AccountInformationWindow().ShowDialog();
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