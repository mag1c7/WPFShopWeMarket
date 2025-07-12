using System;
using System.Collections.Generic;
using System.Linq;
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
	/// Логика взаимодействия для ForjotPasswordWindow.xaml
	/// </summary>
	public partial class ForjotPasswordWindow : Window
	{
		private string realPassword = string.Empty; 
		private bool isPasswordVisible;
		public ForjotPasswordWindow()
		{
			InitializeComponent();
			SetupMaskedTextBox(TextboxNewPassword, () => realPassword, value => realPassword = value);
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
			// Для первого TextBox учитываем флаг isPasswordVisible
			if (textBox == TextboxNewPassword)
			{
				textBox.Text = isPasswordVisible ? realText : new string('●', realText.Length);
			}
			else
			{
				// Для второго TextBox всегда скрываем текст
				textBox.Text = new string('●', realText.Length);
			}

			// Перемещаем курсор в конец текста
			textBox.CaretIndex = textBox.Text.Length;
		}

		private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
		{
			// Переключаем состояние видимости пароля только для первого TextBox
			isPasswordVisible = !isPasswordVisible;
			UpdateDisplayedPassword(TextboxNewPassword, realPassword);
		}
		private void ButtonRepairPassword(object sender, RoutedEventArgs e)
		{
			string email = TextboxEmail.Text;
			if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(realPassword))
			{
				MessageBox.Show("Код отправлен на почту");
				new RepairPasswordWindow(email, realPassword).Show();
				this.Close();
			}
			else
			{
				MessageBox.Show("Не все поля заполненны.");
			}
		}

		private void ButtonBackToAuth(object sender, RoutedEventArgs e)
		{
			new AuthorizationWindow().Show();
			this.Close();
		}
	}
}
