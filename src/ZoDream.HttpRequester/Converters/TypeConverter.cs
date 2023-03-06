using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ZoDream.HttpRequester.Converters
{
    public class TypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return IsVisible(value, parameter) ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsVisible(object value, object parameter) 
        {
            if (value is null)
            {
                return false;
            }
            if (parameter is null)
            {
                if (value is int i)
                {
                    return i > 0;
                }
                return string.IsNullOrWhiteSpace(value.ToString());
            }
            var pStr = parameter.ToString();
            var vStr = value.ToString();
            if (pStr == vStr)
            {
                return true;
            }
            if (vStr is null || pStr is null)
            {
                return false;
            }
            var isRevert = false;
            if (pStr.StartsWith('^'))
            {
                isRevert = true;
                pStr = pStr[1..];
            }
            return pStr.Split(',').Contains(vStr) == !isRevert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
