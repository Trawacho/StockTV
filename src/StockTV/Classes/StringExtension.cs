using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public static class StringExtensions
    {
        public static T ToEnum<T>(this string value)
        {
            if (Enum.TryParse(typeof(T), value, out _))
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            return default;

        }
    }
}
