using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UsefulThings.WPF;

namespace Textures
{
	public static class ImageEngine
	{
		public static bool IsWICSupported { get; private set; }
		
		static ImageEngine()
		{
			// KFreon: Determine if WIC is supported
			try
			{
				UsefulThings.WPF.Imaging.CreateBitmap("Little.dds");
				IsWICSupported = true;
			}
			catch(NoEncoderFound e)
			{
				IsWICSupported = false;
			}
		}
		
		
		public static BitmapImage GetImage(byte[] imgData, bool isNormal, out ResILImage resILImage, int width = -1, int height = -1)
		{
			if (imgData == null)
				return null;
				
			BitmapImage img = null;
				
			if (IsWICSupported && !isNormal)
				img = UsefulThings.WPF.Imaging.CreateBitmap(imgData, width, height);
			else
			{
				resILImage = new ResILImage(imgData);
				img = image.ToBitmap(width, height);
			}
					
			return img;
		}
		
		
		public static bool GetAndSaveThumbnail(byte[] imgData, string savepath, bool isNormal, int width = -1, int height = -1)
		{
			bool success = false;
			ResILImage resILImage = null;
			BitmapImage img = GetImage(imgData, isNormal, out resILImage, width, height);
			
			
			if (img != null)
			{
				try
				{
					if (IsWICSupported && !isNormal)
						UsefulThings.WPF.Imaging.SaveAsJPG(img);
					else
						resILImage.Convertandsave();
					success = true;	
				}
				catch (exception e)
				{
					success = false;
				}
			}
			else
				success = false;
				
			return success;
		}
	}
}