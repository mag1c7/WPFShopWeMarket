using Microsoft.EntityFrameworkCore;
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
	/// Логика взаимодействия для DirectorOrdersWindow.xaml
	/// </summary>
	public partial class DirectorOrdersWindow : Window
	{
		private List<Order> _allOrders = new();
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
		public DirectorOrdersWindow()
		{
			InitializeComponent();
			if (Session.IsAdmin = true)
			{
				ButtonAuth.Visibility = Visibility.Visible;
				ButtonAccountUser.Visibility = Visibility.Collapsed;
			}
			LoadCategoriesAsync();
			LoadAllOrders();
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
		public async Task LoadAllOrders()
		{
			try
			{
				_allOrders = await _context.Orders
					.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
					.Include(o => o.PickupPoint).Include(o => o.User).ToListAsync();
				ApplyFilters();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке заказов: " + ex.Message);
			}
		}
		private void ApplyFilters()
		{
			string searchText = txtSearchOrders.Text?.Trim().ToLower() ?? "";
			string selectedSort = (cmbSortOrders.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
			var filtered = _allOrders
				.Where(o =>
					o.OrderId.ToString().Contains(searchText) ||
					(o.PickupPoint?.Address?.ToLower().Contains(searchText) ?? false) ||
					o.PaymentStatus.ToLower().Contains(searchText) ||
					o.OrderItems.Any(oi => oi.Product.Name.ToLower().Contains(searchText))
				)
				.ToList();
			switch (selectedSort)
			{
				case "Сначала выданные":
					filtered = filtered
						.Where(o => o.PaymentStatus == "Выдан")
						.OrderByDescending(o => o.OrderId)
						.ToList();
					break;
				case "Сначала отменённые":
					filtered = filtered
						.Where(o => o.PaymentStatus == "Отменён")
						.OrderByDescending(o => o.OrderId)
						.ToList();
					break;
				case "Сначала новые":
					filtered = filtered
						.Where(o => o.PaymentStatus == "В процессе")
						.OrderByDescending(o => o.OrderId)
						.ToList();
					break;
				default:
					filtered = filtered.OrderByDescending(o => o.OrderDate).ToList();
					break;
			}
			ListViewProducts.Items.Clear();
			foreach (var order in filtered)
			{
				ListViewProducts.Items.Add(new OrderUserControl(order));
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
			new AuthorizationWindow().Show();
			this.Close();
		}
		private void ButtonClose(object sender, RoutedEventArgs e)
		{
			isMenuVisible = false;
		}
		private async void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
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
		private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//if (cmbCategory.SelectedItem is ComboBoxItemModel item)
			//{
			//	if (item.Id == -1)
			//	{
			//		return;
			//	}
			//	if (_categoryItems.Any(x => x.Id == -1))
			//	{
			//		var itemsCopy = new List<ComboBoxItemModel>(_categoryItems.Where(i => i.Id != -1));
			//		_categoryItems = itemsCopy;
			//		var selectedId = item.Id;
			//		cmbCategory.ItemsSource = null;
			//		cmbCategory.ItemsSource = _categoryItems;
			//		cmbCategory.SelectedValue = selectedId;
			//	}
			//	if (item.Id == 0)
			//	{
			//		_selectedCategoryId = null;
			//	}
			//	else
			//	{
			//		_selectedCategoryId = item.Id;
			//	}
			//}
			new DirectorWindow().Show();
			this.Close();
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
		private void cmbSortOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ApplyFilters();
		}
		private void txtSearchOrders_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilters();
		}
	}
}