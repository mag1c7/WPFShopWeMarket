using NewWpfShop.Class;
using NewWpfShop.DataBase;
using Newtonsoft.Json;
using NewWpfShop.Windows.UserWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
	/// Логика взаимодействия для RegistrationWindow.xaml
	/// </summary>
	public partial class RegistrationWindow : Window
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private string realPassword = string.Empty;
		private string realRepeatPassword = string.Empty;
		private bool isPasswordVisible;

		public RegistrationWindow()
		{
			InitializeComponent();
			SetupMaskedTextBox(TextboxPassword, () => realPassword, value => realPassword = value);
			SetupMaskedTextBox(TextboxRepeatPassword, () => realRepeatPassword, value => realRepeatPassword = value);
		}

		private void SetupMaskedTextBox(TextBox textBox, Func<string> getRealText, Action<string> setRealText)
		{
			textBox.PreviewTextInput += (s, e) =>
			{
				var currentText = getRealText();
				currentText += e.Text;
				setRealText(currentText);
				UpdateDisplayedPassword(textBox, currentText);
				e.Handled = true;
			};

			textBox.PreviewKeyDown += (s, e) =>
			{
				var currentText = getRealText();
				if (e.Key == Key.Back && currentText.Length > 0)
				{
					currentText = currentText.Substring(0, currentText.Length - 1);
					setRealText(currentText);
					UpdateDisplayedPassword(textBox, currentText);
					e.Handled = true;
				}
			};

			textBox.TextChanged += (s, e) =>
			{
				var currentText = getRealText();
				if (textBox.Text != new string('●', currentText.Length))
				{
					UpdateDisplayedPassword(textBox, currentText);
				}
			};
		}

		private void UpdateDisplayedPassword(TextBox textBox, string realText)
		{
			if (textBox == TextboxPassword)
			{
				textBox.Text = isPasswordVisible ? realText : new string('●', realText.Length);
			}
			else
			{
				textBox.Text = new string('●', realText.Length);
			}

			// Перемещаем курсор в конец текста
			textBox.CaretIndex = textBox.Text.Length;
		}

		private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
		{
			isPasswordVisible = !isPasswordVisible;
			UpdateDisplayedPassword(TextboxPassword, realPassword);
		}
		private bool IsValidEmail(string email)
		{
			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			}
			catch
			{
				return false;
			}
		}
		private async void ButtonReg(object sender, RoutedEventArgs e)
		{
			string login = TextboxLogin.Text.Trim();
			string password = TextboxPassword.Text;
			string repeatPassword = TextboxRepeatPassword.Text;
			string email = TextboxEmail.Text.Trim();

			if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
				string.IsNullOrEmpty(repeatPassword) || string.IsNullOrEmpty(email))
			{
				MessageBox.Show("Все поля должны быть заполнены.");
				return;
			}
			if (realPassword != realRepeatPassword)
			{
				MessageBox.Show("Пароли не совпадают.");
				return;
			}
			if (!IsValidEmail(email))
			{
				MessageBox.Show("Введите корректный адрес электронной почты.");
				return;
			}
			using var context = new ProductshopwmContext();
			bool emailExists = context.Users.Any(u => u.Email == email);

			if (emailExists)
			{
				MessageBox.Show("Пользователь с такой почтой уже зарегистрирован.");
				return;
			}
			MessageBox.Show("Код подтверждения отправлен на почту");
			var confirmEmailCode = new ConfirmEmailWindow(email);
			bool? result = confirmEmailCode.ShowDialog();
			if (result == true)
			{
				try
				{
					HttpResponseMessage registerResponse = await httpClient.PostAsync(
						$"http://localhost:5099/CreateUser?login={login}&password={password}&repeatpassword={repeatPassword}&email={email}",
						null);

					if (registerResponse.IsSuccessStatusCode)
					{
						var userJson = await registerResponse.Content.ReadAsStringAsync();
						var user = JsonConvert.DeserializeObject<User>(userJson);

						Session.CurrentUser = login;
						Session.EmailUser = email;
						Session.IsAuthenticated = true;
						Session.IdUser = user.UserId;

						MessageBox.Show("Вы успешно создали аккаунт.");
						new BuyerWindow().Show();
						this.Close();
					}
					else
					{
						string errorMsg = await registerResponse.Content.ReadAsStringAsync();
						MessageBox.Show($"Ошибка регистрации: {errorMsg}");
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
			else
			{
				MessageBox.Show("Код неверный или отменен. Регистрация прервана.");
			}
		}
		private void Button_Auth(object sender, RoutedEventArgs e)
		{
			new AuthorizationWindow().Show();
			this.Close();
		}
	}
}