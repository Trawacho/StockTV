using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    /// <summary>
    /// Extension-Class for String
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts the given string to an Enumeration
        /// </summary>
        /// <typeparam name="T">type of the target enumeration</typeparam>
        /// <param name="value">Value as string</param>
        /// <returns></returns>
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
