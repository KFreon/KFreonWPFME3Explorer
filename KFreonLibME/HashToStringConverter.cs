using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace KFreonLibME
{
    public class HashToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string hash = null;
            if (value.GetType() == typeof(uint))
                hash = KFreonLibME.Textures.Methods.FormatTexmodHashAsString((uint)value);

            return hash;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            uint hash = 0;
            if (value.GetType() == typeof(String))
                hash = KFreonLibME.Textures.Methods.FormatTexmodHashAsUint((string) value);

            return hash;
        }
    }
}
