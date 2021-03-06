﻿using System;
using Windows.UI.Xaml.Data;

namespace StockTV.Converter
{
    public class DoubleDivisionConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var d = System.Convert.ToInt32(parameter);

            return (double)value / d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
