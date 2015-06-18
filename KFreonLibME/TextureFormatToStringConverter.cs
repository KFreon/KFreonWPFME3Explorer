using KFreonLibME.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace KFreonLibME
{
    public class TextureFormatToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(TextureFormat))
                return null;
            return ((TextureFormat)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.GetType() != typeof(string))
                return null;
            return Enum.Parse(typeof(TextureFormat), (string)value);
        }
    }
}
