using NewWpfShop.AdminUserControls.AdminControls;
using NewWpfShop.AdminUserControls.UserControls;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.GeneralWindows;
using NewWpfShop.Windows.UserWindows;
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

namespace NewWpfShop.Windows.DirectorWindows
{
	/// <summary>
	/// Логика взаимодействия для DirectorWindow.xaml
	/// </summary>
	public partial class DirectorWindow : Window
	{
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
		public DirectorWindow()
		{
			InitializeComponent();
			if (Session.IsAdmin = true)
			{
				ButtonAuth.Visibility = Visibility.Visible;
				ButtonAccountUser.Visibility = Visibility.Collapsed;
			}
			LoadCategoriesAsync();
			LoadProducts();
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
		public async Task LoadProducts()
		{
			try
			{
				var products = await httpClient.GetFromJsonAsync<List<Product>>("http://localhost:5099/GetAllProducts");
				if (products != null)
				{
					var filteredProducts = _selectedCategoryId switch
					{
						null => products.Where(p => p.Stock > 0),
						int id when id > 0 => products.Where(p => p.CategoryId == id && p.Stock > 0),
						_ => products.Where(p => p.Stock > 0)
					};
					ListViewProducts.Items.Clear();

					foreach (var product in filteredProducts)
					{
						ListViewProducts.Items.Add(new DirectorUserControl(product));
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка, не удалось подключиться к серверу");
			}
		}
		private void BackToHome_Click(object sender, RoutedEventArgs e)
		{
			new DirectorWindow().Show();
			this.Close();
		}
		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			double targetRight = isMenuVisible ? -200 :0;
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
			new AuthorizationWindow().Show();
			this.Close();
		}
		private void ButtonClose(object sender, RoutedEventArgs e)
		{
			isMenuVisible = false;
		}
		private async void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			var searchQuery = ((TextBox)sender).Text.Trim();
			if (string.IsNullOrWhiteSpace(searchQuery))
			{
				await LoadProducts();
				return;
			}
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/SearchProduct?query={Uri.EscapeDataString(searchQuery)}");
				if (response.IsSuccessStatusCode)
				{
					var products = await response.Content.ReadFromJsonAsync<List<Product>>();
					ListViewProducts.Items.Clear();
					if (products != null && products.Count > 0)
					{
						foreach (var product in products)
						{
							ListViewProducts.Items.Add(new DirectorUserControl(product));
						}
					}
					else
					{
					}
				}
				else
				{
					ListViewProducts.Items.Clear();
				}
			}
			catch (Exception ex)
			{
			}
		}
		private void ButtonAccountUser_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser != 0)
			{
				new AccountInformationWindow().ShowDialog();
			}
		}
		private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cmbCategory.SelectedValue is int selectedCategoryId)
			{
				_selectedCategoryId = selectedCategoryId == 0 ? null : selectedCategoryId;
				rowSort.Height = _selectedCategoryId != null
					? new GridLength(0.5, GridUnitType.Star)
					: new GridLength(0.01, GridUnitType.Star);
				rowSort.MaxHeight = 40;
				DisplayProducts(GetFilteredProducts());
			}
		}
		private List<Product> GetFilteredProducts()
		{
			var filtered = _allProducts;

			if (_selectedCategoryId != null)
				filtered = filtered.Where(p => p.CategoryId == _selectedCategoryId).ToList();
			return filtered;
		}

		private void ButtonAuth_Click(object sender, RoutedEventArgs e)
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
		private void ButtonMakePurchase_Click(object sender, RoutedEventArgs e)
		{
			new DirectorOrdersWindow().Show();
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
			ListViewProducts.Items.Clear();
			foreach (var product in products)
			{
				ListViewProducts.Items.Add(new ProductUserControl(product));
			}
		}
	}
}