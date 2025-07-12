using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NewWpfShop.AdminUserControls.UserControls;
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
    /// Логика взаимодействия для FavoriteWindow.xaml
    /// </summary>
    public partial class FavoriteWindow : Window
    {
		private bool _isCategoryInitialized = false;
		private List<FavoriteItemDto> _allFavoriteItems = new List<FavoriteItemDto>();
		private string _sortOrder;
		private static readonly HttpClient httpClient = new HttpClient();
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
		public FavoriteWindow()
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
			LoadCategoriesAsync();
			LoadFavoriteProducts();
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
		private async Task LoadFavoriteProducts(string sortByField = "addedDate", string sortOrder = "desc")
		{
			int userId = Session.IdUser;
			if (userId == 0)
			{
				MessageBox.Show("Вы не авторизованы.");
				return;
			}
			try
			{
				var url = $"http://localhost:5099/GetAllFavoritesWithSorting?userId={userId}&sortByField={sortByField}&sortOrder={sortOrder}";
				var response = await httpClient.GetAsync(url);
				if (!response.IsSuccessStatusCode)
				{
					var errorText = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка загрузки избранного: {response.ReasonPhrase}\n\n{errorText}");
					return;
				}
				var json = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<FavoriteResponseDto>(json);
				if (!result.Success || result.Data == null)
				{
					ListViewProducts.Items.Clear();
					MessageBox.Show("Избранное пусто.");
					return;
				}
				_allFavoriteItems = result.Data;
				ApplyFilters();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке избранного: " + ex.Message);
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
		private void ApplyFilters()
		{
			var filteredList = _allFavoriteItems;
			if (checkboxCoast.IsChecked == true)
			{
				filteredList = filteredList.Where(i => i.Quantity > 0).ToList();
			}
			string searchText = TextboxSearchFavorite.Text?.Trim().ToLower();
			if (!string.IsNullOrEmpty(searchText))
			{
				filteredList = filteredList
					.Where(i => i.ProductName.ToLower().Contains(searchText))
					.ToList();
			}
			ListViewProducts.Items.Clear();
			foreach (var item in filteredList)
			{
				var productControl = new FavoriteUserControl(new Product
				{
					ProductId = item.ProductId,
					Name = item.ProductName,
					Price = item.Price,
					Stock = item.Quantity,
					Description = item.Desc
				});
				ListViewProducts.Items.Add(productControl);
			}
		}
		private async void TextboxSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			new BuyerWindow().Show();
			this.Close();
		}
		private void ButtonAccountUser_Click(object sender, RoutedEventArgs e)
		{
			if (Session.IdUser == 0)
			{
				new AuthorizationWindow().Show();
				this.Close();
			}
			else
			{
				new AccountInformationWindow().ShowDialog();
			}
		}
		private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cmbSort.SelectedItem is ComboBoxItem selectedItem)
			{
				string selectedText = selectedItem.Content.ToString();

				string sortByField = "addedDate";
				string sortOrder = "desc";
				switch (selectedText)
				{
					case "По умолчанию":
						LoadFavoriteProducts().ConfigureAwait(false);
						return;
					case "По дате добавления ↑":
						sortByField = "addedDate";
						sortOrder = "asc";
						break;
					case "По дате добавления ↓":
						sortByField = "addedDate";
						sortOrder = "desc";
						break;
					case "По возрастанию цены":
						sortByField = "price";
						sortOrder = "asc";
						break;

					case "По убыванию цены":
						sortByField = "price";
						sortOrder = "desc";
						break;
					case "Сначала в наличии":
						sortByField = "stock";
						sortOrder = "desc";
						break;
					case "Сначала не в наличии":
						sortByField = "stock";
						sortOrder = "asc";
						break;
				}
				LoadFavoriteProducts(sortByField, sortOrder).ConfigureAwait(false);
			}
		}
		private void TextboxSearchFavorite_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilters();
		}
		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			ApplyFilters();
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
public class FavoriteItemDto
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public string Desc { get; set; }
	public DateTime AddedDate { get; set; }
}
public class FavoriteResponseDto
{
	public bool Success { get; set; }
	public List<FavoriteItemDto> Data { get; set; }
}
public class FavoriteResponse
{
	public int TotalQuantity { get; set; }
	public List<CartItemInCart> FavoriteItems { get; set; }
}