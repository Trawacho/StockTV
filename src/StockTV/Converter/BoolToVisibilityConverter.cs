using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace StockTV.Converter
{

    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts TRUE to Visible, all ohter to Collapsed
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
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
