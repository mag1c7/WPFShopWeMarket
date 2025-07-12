using Newtonsoft.Json;
using NewWpfShop.Class;
using NewWpfShop.DataBase;
using NewWpfShop.Windows.DirectorWindows;
using NewWpfShop.Windows.UserWindows;
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
using System.Windows.Shapes;
namespace NewWpfShop.Windows.GeneralWindows
{
	/// <summary>
	/// Логика взаимодействия для AuthorizationWindow.xaml
	/// </summary>
	public partial class AuthorizationWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private string realPassword = string.Empty;
		private bool isPasswordVisible;
		public AuthorizationWindow()
		{
			InitializeComponent();
			SubscribeToTextBoxEvents();
		}
		private void SubscribeToTextBoxEvents()
		{
			TextboxPassword.PreviewTextInput += (s, e) =>
			{
				realPassword += e.Text;
				UpdateDisplayedPassword();
				e.Handled = true;
			};
			TextboxPassword.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Back && realPassword.Length > 0)
				{
					realPassword = realPassword.Substring(0, realPassword.Length - 1);
					UpdateDisplayedPassword();
					e.Handled = true;
				}
			};
		}
		private void UpdateDisplayedPassword()
		{

			TextboxPassword.Text = isPasswordVisible ? realPassword : new string('●', realPassword.Length);
			TextboxPassword.CaretIndex = TextboxPassword.Text.Length;
		}
		private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
		{
			isPasswordVisible = !isPasswordVisible;
			UpdateDisplayedPassword();
		}
		private async void ButtonAuth(object sender, RoutedEventArgs e)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(TextboxEmailOrLogin.Text) || string.IsNullOrWhiteSpace(realPassword))
				{
					MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				HttpResponseMessage registerResponse = await httpClient.GetAsync($"http://localhost:5099/Login?login={TextboxEmailOrLogin.Text}&password={realPassword}");

				if (registerResponse.IsSuccessStatusCode)
				{

					var userJson = await registerResponse.Content.ReadAsStringAsync();
					var user = JsonConvert.DeserializeObject<User>(userJson);
					if (user.RoleId == 1)
					{
						Session.CurrentUser = user.Login;
						Session.EmailUser = user.Email;
						Session.IsAuthenticated = true;
						Session.IdUser = user.UserId;
						MessageBox.Show("Вы успешно вошли в аккаунт.");
						new BuyerWindow().Show();
						this.Close();
					}
					else if (user.RoleId == 2)
					{
						Session.CurrentUser = user.Login;
						Session.EmailUser = user.Email;
						Session.IsAuthenticated = true;
						Session.IsAdmin = true;
						Session.IdUser = user.UserId;
						MessageBox.Show("Вы успешно вошли в аккаунт.");
						new DirectorWindow().Show();
						this.Close();
					}
				}
				else
				{
					MessageBox.Show("Неправильный логин или пароль.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось подключиться к серверу. Убедитесь, что он запущен.");
			}
		}
		private void ButtonForjotPassword(object sender, RoutedEventArgs e)
		{
			new ForjotPasswordWindow().Show();
			this.Close();
		}
		private void Button_Registration(object sender, RoutedEventArgs e)
		{
			new RegistrationWindow().Show();
			this.Close();
		}
		private void ButtonBack(object sender, RoutedEventArgs e)
		{
			new BuyerWindow().Show();
			this.Close();
		}
	}
}