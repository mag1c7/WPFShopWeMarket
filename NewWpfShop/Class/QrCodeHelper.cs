using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NewWpfShop.Class
{
	public static class QrCodeHelper
	{
		public static string GenerateQrCode(int orderId)
		{
			try
			{
				string tempFolder = Path.GetTempPath();
				string qrImagePath = Path.Combine(tempFolder, $"order_{orderId}_qr.png");

				// Новый метод для подтверждения заказа через QR
				string ip = GetLocalIpAddress();
				string payload = $"http://{ip}:5099/ConfirmDeliveryy?orderId={orderId}";

				using var qrGenerator = new QRCodeGenerator();
				using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
				using var qrCode = new BitmapByteQRCode(qrCodeData);

				byte[] qrImageBytes = qrCode.GetGraphic(10);
				File.WriteAllBytes(qrImagePath, qrImageBytes);

				return qrImagePath;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка генерации QR-кода: {ex.Message}");
				return null;
			}
		}

		public static string GetLocalIpAddress()
		{
			try
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						return ip.ToString();
					}
				}
				return "127.0.0.1"; // Возвращаем localhost, если IP не найден
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка получения IP-адреса: {ex.Message}");
				return "127.0.0.1";
			}
		}
	}
}
