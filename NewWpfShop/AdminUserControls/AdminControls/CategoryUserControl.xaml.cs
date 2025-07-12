using NewWpfShop.DataBase;
using System;
using System.Collections.Generic;
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

namespace NewWpfShop.AdminUserControls.AdminControls
{
	/// <summary>
	/// Логика взаимодействия для CategoryUserControl.xaml
	/// </summary>
	public partial class CategoryUserControl : UserControl
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private Category category;
		public Category _category
		{
			get => category;
			set
			{
				category = value;
				if (category != null)
				{
					labelId.Content = category.CategoryId;
					labelName.Content = category.CategoryName;
				}
			}
		}
		public CategoryUserControl(Category category)
		{
			InitializeComponent();
			_category = category;
		}
		private async void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (_category == null) return;
			var result = MessageBox.Show(
				$"Вы уверены, что хотите удалить категорию \"{_category.CategoryName}\"?",
				"Подтверждение удаления",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					var response = await httpClient.DeleteAsync($"http://localhost:5099/DeleteCategory?categoryId={_category.CategoryId}");
					if (response.IsSuccessStatusCode)
					{
						MessageBox.Show("Категория успешно удалена.");
						var parent = Parent as Panel;
						if (parent != null)
						{
							parent.Children.Remove(this);
						}
					}
					else
					{
						var errorText = await response.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка при удалении категории:\n{errorText}");
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Не удалось подключиться к серверу:\n{ex.Message}");
				}
			}
		}
		private async void ChangeButton_Click(object sender, RoutedEventArgs e)
		{
			if (_category == null) return;
			var newName = Microsoft.VisualBasic.Interaction.InputBox(
				"Введите новое название категории:",
				"Редактирование категории",
				_category.CategoryName);
			if (string.IsNullOrWhiteSpace(newName)) return;
			try
			{
				var response = await httpClient.PutAsync(
					$"http://localhost:5099/UpdateCategory?categoryId={_category.CategoryId}&newCategoryName={Uri.EscapeDataString(newName)}",null);
				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Название категории успешно изменено.");
					_category.CategoryName = newName;
					labelName.Content = newName;    
				}
				else
				{
					var errorText = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка при обновлении категории:\n{errorText}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Не удалось подключиться к серверу:\n{ex.Message}");
			}
		}
	}
}