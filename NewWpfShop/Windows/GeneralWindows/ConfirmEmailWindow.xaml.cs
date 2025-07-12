using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace NewWpfShop.Windows.GeneralWindows
{
	/// <summary>
	/// Логика взаимодействия для ConfirmEmailWindow.xaml
	/// </summary>
	public partial class ConfirmEmailWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		string email;
		public ConfirmEmailWindow(string emailUser)
		{
			InitializeComponent();
			email = emailUser;
			SendCodeToEmail();
		}
		private void ButtonBack(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}
		private async void ButtonConfirm(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(textboxCode.Text))
			{
				MessageBox.Show("Пожалуйста, введите код подтверждения.");
				return;
			}
			string enteredCode = textboxCode.Text.Trim();
			try
			{
				HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5099/ConfirmCode?email={email}&enteredCode={enteredCode}", null);

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Вы успешно подтвердили почту.");
					this.DialogResult = true;
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
			string enteredCode = textboxCode.Text.Trim();
			try
			{
				HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5099/SendCode?email={email}", null);

				if (response.IsSuccessStatusCode)
				{
					MessageBox.Show("Код подтверждения отправлен.");
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