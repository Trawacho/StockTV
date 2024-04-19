using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace StockTV.Converter
{
    public class StringToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is string v && !(string.IsNullOrEmpty(v)))
            {
                return new GridLength(1, GridUnitType.Star);
            }
            else
            {
                return new GridLength(0, GridUnitType.Pixel);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
