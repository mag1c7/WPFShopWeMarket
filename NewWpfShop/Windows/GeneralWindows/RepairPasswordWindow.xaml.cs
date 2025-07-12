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
	/// Логика взаимодействия для RepairPasswordWindow.xaml
	/// </summary>
	public partial class RepairPasswordWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		string email;
		string password;
		public RepairPasswordWindow(string emailCode,string passwordCode)
		{
			InitializeComponent();
			email = emailCode;
			password = passwordCode;
			textblockemail.Text += $" {email}";
			SendCodeToEmail();
		}
		private void ChangeEmail_Click(object sender, RoutedEventArgs e)
		{
			new ForjotPasswordWindow().Show();
			this.Close();
		}
		private void ButtonAgainSendCode(object sender, RoutedEventArgs e)
		{
			SendCodeToEmail();
		}

		private async void ButtonConfirm(object sender, RoutedEventArgs e)
		{
			string enteredCode = TextboxCode.Text.Trim();
			if (string.IsNullOrEmpty(enteredCode))
			{
				MessageBox.Show("Пожалуйста, введите код подтверждения.");
				return;
			}
			try// начало метода
			{
				HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5099/ConfirmCodeForNewPassword?email={email}&enteredCode={enteredCode}&newpassword={password}", null);

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Вы успешно изменили пароль.");
					new AuthorizationWindow().Show();
					this.Close();
					return;
				}
				else
				{
					MessageBox.Show("Неверный код подтверждения.");
					return;
				}
			}
			catch (HttpRequestException ex)
			{
				MessageBox.Show($"Ошибка соединения: {ex.Message}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Неизвестная ошибка: {ex.Message}");
			}
		}

		private async void SendCodeToEmail()
		{
			string enteredCode = TextboxCode.Text.Trim();

			try
			{
				HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5099/SendCode?email={email}", null);

				if (response.IsSuccessStatusCode)
				{
					//MessageBox.Show("Код подтверждения отправлен.");
				}
				else
				{
					string errorMsg = await response.Content.ReadAsStringAsync();
					MessageBox.Show($"Ошибка отправки кода: {errorMsg}");
				}
			}
			catch (HttpRequestException ex)
			{
				MessageBox.Show($"Ошибка соединения: {ex.Message}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Неизвестная ошибка: {ex.Message}");
			}
		}
	}
}
