using Microsoft.Win32;
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
	/// Логика взаимодействия для AddEditProductWindow.xaml
	/// </summary>
	public partial class AddEditProductWindow : Window
	{
		private List<ComboBoxItemModel> _pickupPointItems;
		private static readonly HttpClient httpClient = new HttpClient();
		private int currentImageIndex = 0;
		private Product product;
		private bool isEditMode = false;
		public AddEditProductWindow()
		{
			InitializeComponent();
			LoadCategoriesAsync();
			currentImageIndex = 0;
			ChooseFileButton.Visibility = Visibility.Visible;
			Grid.SetColumn(ChooseFileButton, 2);
			HideImageControls(image1, ChangeImageButton1, DeleteImageButton1);
			HideImageControls(image2, ChangeImageButton2, DeleteImageButton2);
			HideImageControls(image3, ChangeImageButton3, DeleteImageButton3);
		}
		public async Task LoadCategoriesAsync()
		{
			try
			{
				using var httpclient = new HttpClient();
				var categories = await httpclient.GetFromJsonAsync<List<Category>>($"http://localhost:5099/GetAllCategories");

				if (categories != null)
				{
					categoryComboBox.ItemsSource = categories;
					categoryComboBox.DisplayMemberPath = "CategoryName"; 
					categoryComboBox.SelectedValuePath = "CategoryId";  
				}
				else
				{
					MessageBox.Show("Категории не найдены.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке категорий: " + ex.Message);
			}
		}
		private async void ChooseFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Image Files (*.bmp, *.jpg, *.jpeg, *.png)|*.BMP;*.JPG;*.JPEG;*.PNG",
				Multiselect = true
			};

			if (openFileDialog.ShowDialog() == true)
			{
				string[] selectedFiles = openFileDialog.FileNames;

				try
				{
					for (int i = 0; i < selectedFiles.Length && currentImageIndex < 3; i++)
					{
						string filePath = selectedFiles[i];

						using var stream = File.OpenRead(filePath);
						var imageBytes = new byte[stream.Length];
						await stream.ReadAsync(imageBytes, 0, (int)stream.Length);

						switch (currentImageIndex)
						{
							case 0:
								LoadProductImage(imageBytes, image1);
								ShowImageControls(image1, ChangeImageButton1, DeleteImageButton1);
								break;
							case 1:
								LoadProductImage(imageBytes, image2);
								ShowImageControls(image2, ChangeImageButton2, DeleteImageButton2);
								break;
							case 2:
								LoadProductImage(imageBytes, image3);
								ShowImageControls(image3, ChangeImageButton3, DeleteImageButton3);
								break;
						}

						currentImageIndex++;
						UpdateChooseFileButtonPosition();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}");
				}
			}
		}
		private void UpdateChooseFileButtonPosition()
		{
			gridChooseFileButton.Visibility = currentImageIndex < 3 ? Visibility.Visible : Visibility.Collapsed;

			switch (currentImageIndex)
			{
				case 0:
					Grid.SetColumn(gridChooseFileButton, 2);
					break;
				case 1:
					Grid.SetColumn(gridChooseFileButton, 2);
					break;
				case 2:
					Grid.SetColumn(gridChooseFileButton, 4);
					break;
			}
		}
		private void ShowImageControls(Image image, Button changeButton, Button deleteButton)
		{
			if (image.Source != null)
			{
				changeButton.Visibility = Visibility.Visible;
				deleteButton.Visibility = Visibility.Visible;
			}
		}
		private void HideImageControls(Image image, Button changeButton, Button deleteButton)
		{
			if (image.Source == null)
			{
				changeButton.Visibility = Visibility.Collapsed;
				deleteButton.Visibility = Visibility.Collapsed;
			}
		}
		private void LoadProductImage(byte[] imageData, Image targetImage)
		{
			if (imageData == null || imageData.Length == 0)
			{
				targetImage.Source = null;
				return;
			}

			var bitmapImage = new BitmapImage();
			using (var mem = new MemoryStream(imageData))
			{
				mem.Position = 0;
				bitmapImage.BeginInit();
				bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.UriSource = null;
				bitmapImage.StreamSource = mem;
				bitmapImage.EndInit();
			}
			bitmapImage.Freeze();
			targetImage.Source = bitmapImage;
		}
		private async void ChangeImageButton1_Click(object sender, RoutedEventArgs e)
		{
			await ChangeImageAsync(image1);
		}
		private void DeleteImageButton1_Click(object sender, RoutedEventArgs e)
		{
			DeleteImage(image1, ChangeImageButton1, DeleteImageButton1);
		}
		private async void ChangeImageButton2_Click(object sender, RoutedEventArgs e)
		{
			await ChangeImageAsync(image2);
		}
		private void DeleteImageButton2_Click(object sender, RoutedEventArgs e)
		{
			DeleteImage(image2, ChangeImageButton2, DeleteImageButton2);
		}
		private async void ChangeImageButton3_Click(object sender, RoutedEventArgs e)
		{
			await ChangeImageAsync(image3);
		}
		private void DeleteImageButton3_Click(object sender, RoutedEventArgs e)
		{
			DeleteImage(image3, ChangeImageButton3, DeleteImageButton3);
		}
		private async Task ChangeImageAsync(Image image)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files (*.bmp, *.jpg, *.jpeg, *.png)|*.BMP;*.JPG;*.JPEG;*.PNG";
			if (openFileDialog.ShowDialog() == true)
			{
				string filePath = openFileDialog.FileName;
				try
				{
					using (var stream = File.OpenRead(filePath))
					{
						var imageBytes = new byte[stream.Length];
						await stream.ReadAsync(imageBytes, 0, (int)stream.Length);
						LoadProductImage(imageBytes, image);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при изменении изображения: {ex.Message}");
				}
			}
		}
		private void DeleteImage(Image image, Button changeButton, Button deleteButton)
		{
			image.Source = null;
			HideImageControls(image, changeButton, deleteButton);
			if (currentImageIndex > 0)
			{
				currentImageIndex--;
			}
			UpdateChooseFileButtonPosition();
		}
		private byte[] ConvertImageSourceToByteArray(ImageSource imageSource)
		{
			if (imageSource is BitmapSource bitmapSource)
			{
				var encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
				using (var stream = new MemoryStream())
				{
					encoder.Save(stream);
					return stream.ToArray();
				}
			}
			return null;
		}
		private async void ButtonSave(object sender, RoutedEventArgs e)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(textboxName.Text) ||
	string.IsNullOrWhiteSpace(textboxDesc.Text) ||
	string.IsNullOrWhiteSpace(textboxPrice.Text) ||
	string.IsNullOrWhiteSpace(textboxStockProduct.Text) ||
	string.IsNullOrWhiteSpace(textboxSupplier.Text) ||
	string.IsNullOrWhiteSpace(textboxCountryOfOrigin.Text))
				{
					MessageBox.Show("Все текстовые поля должны быть заполнены.");
					return;
				}
				if (categoryComboBox.SelectedValue == null)
				{
					MessageBox.Show("Пожалуйста, выберите категорию.");
					return;
				}
				if (!decimal.TryParse(textboxPrice.Text, out decimal price) || price <= 0)
				{
					MessageBox.Show("Введите корректное значение цены (положительное число).");
					return;
				}
				if (!int.TryParse(textboxStockProduct.Text, out int stock) || stock < 0)
				{
					MessageBox.Show("Введите корректное значение количества (неотрицательное целое число).");
					return;
				}
				if (image1.Source == null && image2.Source == null && image3.Source == null)
				{
					MessageBox.Show("Загрузите хотя бы одно изображение товара.");
					return;
				}
				var formData = new MultipartFormDataContent();
				formData.Add(new StringContent(textboxName.Text), "Name");
				formData.Add(new StringContent(textboxDesc.Text), "Description");
				formData.Add(new StringContent(textboxPrice.Text), "Price");
				formData.Add(new StringContent(textboxStockProduct.Text), "Stock");
				formData.Add(new StringContent(categoryComboBox.SelectedValue.ToString()), "CategoryId");
				formData.Add(new StringContent(textboxSupplier.Text), "Supplier");
				formData.Add(new StringContent(textboxCountryOfOrigin.Text), "CountryOfOrigin");
				if (image1.Source != null)
				{
					var image1Bytes = ConvertImageSourceToByteArray(image1.Source);
					formData.Add(new ByteArrayContent(image1Bytes), "images", "image1.jpg");
				}
				if (image2.Source != null)
				{
					var image2Bytes = ConvertImageSourceToByteArray(image2.Source);
					formData.Add(new ByteArrayContent(image2Bytes), "images", "image2.jpg");
				}
				if (image3.Source != null)
				{
					var image3Bytes = ConvertImageSourceToByteArray(image3.Source);
					formData.Add(new ByteArrayContent(image3Bytes), "images", "image3.jpg");
				}
				var response = await httpClient.PostAsync("http://localhost:5099/CreateProduct", formData);
				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Продукт успешно добавлен!");
				}
				else
				{
					var errorMessage = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка: {errorMessage}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при отправке данных: {ex.Message}");
			}
		}
		private void ButtonBack(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void categoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (categoryComboBox.SelectedItem is ComboBoxItemModel item)
			{
				if (item.Id == -1)
				{
					return;
				}
				if (_pickupPointItems.Any(x => x.Id == -1))
				{
					var itemsCopy = new List<ComboBoxItemModel>(_pickupPointItems.Where(i => i.Id != -1));
					_pickupPointItems = itemsCopy;
					var selectedId = item.Id;
					categoryComboBox.ItemsSource = null;
					categoryComboBox.ItemsSource = _pickupPointItems;
					categoryComboBox.SelectedValue = selectedId;
				}
			}
		}
	}
}
