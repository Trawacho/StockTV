using StockTV.Classes;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace StockTV.Converter
{
    class GameModusToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if((GameSettings.Modis)value == GameSettings.Modis.Lkms)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
