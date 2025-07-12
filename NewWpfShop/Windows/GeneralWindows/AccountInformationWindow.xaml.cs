	using Microsoft.Win32;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net.Http.Headers;
	using System.Net.Http;
	using System.Reflection.Metadata;
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
	using NewWpfShop.DataBase;
	using NewWpfShop.Class;
using Newtonsoft.Json;

namespace NewWpfShop.Windows.GeneralWindows
{
	/// <summary>
	/// Логика взаимодействия для AccountInformationWindow.xaml
	/// </summary>
	public partial class AccountInformationWindow : Window
	{
		public AccountInformationWindow()
		{
			InitializeComponent();
			if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				_ = LoadUserData();
		}
		private async Task LoadUserData()
		{
			try
			{
				var response = await new HttpClient().GetAsync($"http://localhost:5099/GetUserById?userId={Session.IdUser}");
				if (!response.IsSuccessStatusCode)
				{
					MessageBox.Show("Ошибка при получении данных пользователя.");
					return;
				}
				var json = await response.Content.ReadAsStringAsync();
				var user = JsonConvert.DeserializeObject<User>(json);
				textboxName.Text = user.Name;
				textboxSurname.Text = user.Surname;
				textboxEmail.Text = user.Email;
				textboxPassword.Text = "";
				if (user.ImageUser != null && user.ImageUser.Length > 0)
				{
					using (var ms = new MemoryStream(user.ImageUser))
					{
						var bitmap = new BitmapImage();
						bitmap.BeginInit();
						bitmap.CacheOption = BitmapCacheOption.OnLoad;
						bitmap.StreamSource = ms;
						bitmap.EndInit();
						bitmap.Freeze();
						userImageBrush.ImageSource = bitmap;
					}
				}
				else
				{
					userImageBrush.ImageSource = new BitmapImage(new Uri("/Assets/DefaultImage.png", UriKind.Relative));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка: " + ex.Message);
			}
		}
		private async void ButtonSaveData(object sender, RoutedEventArgs e)
		{
			try
			{
				string name = textboxName.Text.Trim();
				string surname = textboxSurname.Text.Trim();
				string password = textboxPassword.Text.Trim();
				string email = textboxEmail.Text.Trim();
				if (string.IsNullOrWhiteSpace(name) ||
					string.IsNullOrWhiteSpace(surname) ||
					string.IsNullOrWhiteSpace(password) ||
					string.IsNullOrWhiteSpace(email))
				{
					MessageBox.Show("Все поля обязательны.");
					return;
				}
				string url = $"http://localhost:5099/UpdateUserData?" +
							 $"idUser={Uri.EscapeDataString(Session.IdUser.ToString())}" +
							 $"&login={Uri.EscapeDataString(name)}" +
							 $"&surname={Uri.EscapeDataString(surname)}" +
							 $"&password={Uri.EscapeDataString(password)}" +
							 $"&email={Uri.EscapeDataString(email)}";
				using var httpClient = new HttpClient();
				var response = await httpClient.PostAsync(url, null);
				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Данные успешно обновлены.");
				}
				else
				{
					string error = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка: {error}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
			}
		}
		private async void ButtonChangeImage_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files (*.bmp, *.jpg, *.jpeg, *.png)|*.BMP;*.JPG;*.JPEG;*.PNG";
			if (openFileDialog.ShowDialog() == true)
			{
				string filePath = openFileDialog.FileName;
				try
				{
					using (var httpClient = new HttpClient())
					{
						var fileInfo = new FileInfo(filePath);
						using (var fileStream = fileInfo.OpenRead())
						{
							var streamContent = new StreamContent(fileStream);
							streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
							var multipartContent = new MultipartFormDataContent();
							multipartContent.Add(streamContent, "image", fileInfo.Name);
							multipartContent.Add(new StringContent(Session.IdUser.ToString()), "userId");
							var response = await httpClient.PostAsync("http://localhost:5099/UploadUserImage", multipartContent);
							if (response.IsSuccessStatusCode)
							{
								MessageBox.Show("Изображение успешно загружено.");
							}
							else
							{
								MessageBox.Show($"Ошибка при загрузке изображения: {await response.Content.ReadAsStringAsync()}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка: {ex.Message}");
				}
			}
		}
	}
}