using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NewWpfShop.Class
{
	public static class Session
	{
		public static bool IsAuthenticated { get; set; } = false;
		public static bool IsAdmin { get; set; } = false;
		public static int IdUser { get; set; }
		public static string EmailUser { get; set; }
		public static string CurrentUser { get; set; }
		public static void LoadProductImage(object imageData, System.Windows.Controls.Image imageControl)
		{
			if (imageData == null || imageControl == null)
			{
				imageControl.Source = null;
				return;
			}

			byte[] byteArray = imageData as byte[];
			if (byteArray == null || byteArray.Length == 0)
			{
				imageControl.Source = null;
				return;
			}

			var bitmapImage = new BitmapImage();
			using (var mem = new MemoryStream(byteArray))
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
			imageControl.Source = bitmapImage;
		}
	}
}
