using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace StockTV.Converter
{
    public class BooleanToGridlengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
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
