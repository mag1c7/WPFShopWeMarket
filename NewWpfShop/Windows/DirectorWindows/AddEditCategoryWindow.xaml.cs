using NewWpfShop.AdminUserControls.AdminControls;
using NewWpfShop.AdminUserControls.UserControls;
using NewWpfShop.DataBase;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NewWpfShop.Windows.DirectorWindows
{
	/// <summary>
	/// Логика взаимодействия для AddEditCategoryWindow.xaml
	/// </summary>
	public partial class AddEditCategoryWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private int? _selectedCategoryId = null;

		public AddEditCategoryWindow()
		{
			InitializeComponent();
			LoadCategories();
		}

		private async void LoadCategories()
		{
			try
			{
				var categories = await httpClient.GetFromJsonAsync<List<Category>>("http://localhost:5099/GetAllCategories");

				if (categories != null)
				{
					foreach (var category in categories)
					{
						var categoryControl = new CategoryUserControl(category);
						ListViewProducts.Items.Add(categoryControl);
					}
				}
				else
				{
					MessageBox.Show("Нет категорий для отображения.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка подключения к серверу: {ex.Message}");
			}
		}
		private async void ButtonSave(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(textboxName.Text))
			{
				MessageBox.Show("Введите название категории.");
				return;
			}

			try
			{
				var response = await httpClient.PostAsync(
					$"http://localhost:5099/AddCategory?categoryName={Uri.EscapeDataString(textboxName.Text)}",
					null);

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Категория успешно добавлена.");
					LoadCategories();
				}
				else
				{
					var errorText = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка: {errorText}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}");
			}
		}

		private void ButtonBack(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
