using Microsoft.Win32;
using Newtonsoft.Json;
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
using System.Windows.Shapes;

namespace NewWpfShop.Windows.DirectorWindows
{
	/// <summary>
	/// Логика взаимодействия для EditProductWindow.xaml
	/// </summary>
	public partial class EditProductWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private Product _product;
		private List<byte[]> _imageBytesList = new List<byte[]>();

		// Привязка элементов из XAML (по имени)
		private Image imagePreview1 => this.image1;
		private Image imagePreview2 => this.image2;
		private Image imagePreview3 => this.image3;

		public EditProductWindow(Product product)
		{
			InitializeComponent();
			_product = product;

			LoadProductData();
			LoadCategories();

		}
		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadProductImagesAsync(_product.ProductId);
		}

		private void LoadProductData()
		{
			textboxArticul.Text = _product.ProductId.ToString();
			textboxName.Text = _product.Name;
			textboxDesc.Text = _product.Description;
			textboxPrice.Text = _product.Price.ToString("F2");
			textboxStockProduct.Text = _product.Stock.ToString();
			textboxSupplier.Text = _product.Supplier;
			textboxCountryOfOrigin.Text = _product.CountryOfOrigin;

			_imageBytesList.Clear();

			if (_product.ProductImages != null && _product.ProductImages.Count > 0)
			{
				// Берём максимум 3 изображения
				var images = _product.ProductImages.Take(3).ToList();

				foreach (var img in images)
				{
					if (img.ImageProduct != null)
						_imageBytesList.Add(img.ImageProduct);
				}
			}

			RefreshImagePreviews(); // обновляем отображение
		}
		private async Task LoadProductImagesAsync(int productId)
		{
			try
			{
				var response = await httpClient.GetAsync($"http://localhost:5099/GetProductImages/{productId}");

				if (!response.IsSuccessStatusCode)
				{
					MessageBox.Show("Не удалось загрузить изображения товара.");
					return;
				}

				var json = await response.Content.ReadAsStringAsync();
				var base64List = JsonConvert.DeserializeObject<List<string>>(json);

				if (base64List == null || base64List.Count == 0)
					return;

				_imageBytesList.Clear(); // Добавим это!

				for (int i = 0; i < base64List.Count && i < 3; i++)
				{
					byte[] bytes = Convert.FromBase64String(base64List[i]);
					_imageBytesList.Add(bytes);
				}

				RefreshImagePreviews(); // Вместо прямой установки image1.Source = ...
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке изображений: {ex.Message}");
			}
		}
		private async void LoadCategories()
		{
			try
			{
				var categories = await httpClient.GetFromJsonAsync<List<Category>>("http://localhost:5099/GetAllCategories");

				if (categories != null)
				{
					categoryComboBox.ItemsSource = categories;
					categoryComboBox.DisplayMemberPath = "CategoryName";
					categoryComboBox.SelectedValuePath = "CategoryId";
					categoryComboBox.SelectedValue = _product.CategoryId;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
			}
		}
		private void categoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (categoryComboBox.SelectedItem is Category selectedCategory)
			{
				_product.CategoryId = selectedCategory.CategoryId;
			}
		}
		private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
			if (openFileDialog.ShowDialog() == true)
			{
				string filePath = openFileDialog.FileName;
				byte[] imageData = File.ReadAllBytes(filePath);

				if (_imageBytesList.Count < 3)
				{
					_imageBytesList.Add(imageData);
				}
				else
				{
					MessageBox.Show("Можно загрузить максимум 3 изображения.");
					return;
				}

				RefreshImagePreviews();
			}
		}
		private void DeleteImageButton1_Click(object sender, RoutedEventArgs e) => RemoveImage(0);
		private void DeleteImageButton2_Click(object sender, RoutedEventArgs e) => RemoveImage(1);
		private void DeleteImageButton3_Click(object sender, RoutedEventArgs e) => RemoveImage(2);

		private void RemoveImage(int index)
		{
			if (index >= 0 && index < _imageBytesList.Count)
			{
				_imageBytesList.RemoveAt(index);
				RefreshImagePreviews();
			}
		}
		private void RefreshImagePreviews()
		{
			imagePreview1.Source = _imageBytesList.Count > 0 ? LoadImage(_imageBytesList[0]) : null;
			imagePreview2.Source = _imageBytesList.Count > 1 ? LoadImage(_imageBytesList[1]) : null;
			imagePreview3.Source = _imageBytesList.Count > 2 ? LoadImage(_imageBytesList[2]) : null;

			// Показ/скрытие кнопок удаления и изменения
			DeleteImageButton1.Visibility = _imageBytesList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
			ChangeImageButton1.Visibility = _imageBytesList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

			DeleteImageButton2.Visibility = _imageBytesList.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
			ChangeImageButton2.Visibility = _imageBytesList.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

			DeleteImageButton3.Visibility = _imageBytesList.Count > 2 ? Visibility.Visible : Visibility.Collapsed;
			ChangeImageButton3.Visibility = _imageBytesList.Count > 2 ? Visibility.Visible : Visibility.Collapsed;

			// Управление позиционированием кнопки выбора файла
			switch (_imageBytesList.Count)
			{
				case 0:
					Grid.SetColumn(gridChooseFileButton, 2);
					gridChooseFileButton.Visibility = Visibility.Visible;
					break;
				case 1:
					Grid.SetColumn(gridChooseFileButton, 2);
					gridChooseFileButton.Visibility = Visibility.Visible;
					break;
				case 2:
					Grid.SetColumn(gridChooseFileButton, 4); // перемещение в 4-ю колонку
					gridChooseFileButton.Visibility = Visibility.Visible;
					break;
				default:
					gridChooseFileButton.Visibility = Visibility.Collapsed;
					break;
			}
		}
		private async void ButtonSave(object sender, RoutedEventArgs e)
		{
			if (!decimal.TryParse(textboxPrice.Text, out decimal price) || price <= 0)
			{
				MessageBox.Show("Введите корректное значение цены.");
				return;
			}

			if (!int.TryParse(textboxStockProduct.Text, out int stock) || stock < 0)
			{
				MessageBox.Show("Введите корректное количество.");
				return;
			}

			var updatedProduct = new Product
			{
				ProductId = _product.ProductId,
				Name = textboxName.Text.Trim(),
				Description = textboxDesc.Text.Trim(),
				Price = price,
				Stock = stock,
				Supplier = textboxSupplier.Text.Trim(),
				CountryOfOrigin = textboxCountryOfOrigin.Text.Trim(),
				CategoryId = (int)categoryComboBox.SelectedValue
			};

			try
			{
				// 1. Обновление самого товара
				var response = await httpClient.PutAsJsonAsync("http://localhost:5099/ChangeProduct", updatedProduct);

				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка при обновлении товара:\n{error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// 2. Удаление старых изображений
				var deleteResponse = await httpClient.DeleteAsync($"http://localhost:5099/DeleteImages?productId={updatedProduct.ProductId}");
				if (!deleteResponse.IsSuccessStatusCode)
				{
					MessageBox.Show("Не удалось удалить старые изображения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				// 3. Добавление новых изображений
				foreach (var img in _imageBytesList)
				{
					var content = new MultipartFormDataContent
			{
				{ new ByteArrayContent(img), "image", "image.jpg" }
			};

					var uploadResponse = await httpClient.PostAsync($"http://localhost:5099/AddImage?productId={updatedProduct.ProductId}", content);

					if (!uploadResponse.IsSuccessStatusCode)
					{
						var errorText = await uploadResponse.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка при загрузке изображения: {errorText}");
					}
				}

				MessageBox.Show("Товар успешно обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				this.DialogResult = true;
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}", "Ошибка сети", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ButtonBack(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void ChangeImageButton1_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
			if (openFileDialog.ShowDialog() == true)
			{
				byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);
				if (_imageBytesList.Count > 0) _imageBytesList[0] = imageData;
				else _imageBytesList.Add(imageData);
				RefreshImagePreviews();
			}
		}

		private void ChangeImageButton2_Click(object sender, RoutedEventArgs e)
		{
			if (_imageBytesList.Count < 2) _imageBytesList.Add(new byte[0]);

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
			if (openFileDialog.ShowDialog() == true)
			{
				byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);
				if (_imageBytesList.Count > 1) _imageBytesList[1] = imageData;
				else _imageBytesList.Add(imageData);
				RefreshImagePreviews();
			}
		}

		private void ChangeImageButton3_Click(object sender, RoutedEventArgs e)
		{
			if (_imageBytesList.Count < 3) _imageBytesList.Add(new byte[0]);

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
			if (openFileDialog.ShowDialog() == true)
			{
				byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);
				if (_imageBytesList.Count > 2) _imageBytesList[2] = imageData;
				else _imageBytesList.Add(imageData);
				RefreshImagePreviews();
			}
		}
		private BitmapImage LoadImage(byte[] imageData)
		{
			using (var ms = new MemoryStream(imageData))
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = ms;
				image.EndInit();
				image.Freeze(); // Чтобы избежать проблем с доступом из других потоков
				return image;
			}
		}

	}
}