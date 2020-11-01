using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public static class Debounce
    {
        public static uint lastValue;
        public static long lastTick;
        public static bool IsDebounceOk(uint val)
        {
            System.Diagnostics.Debug.WriteLine($"calling Debounce with lastTick: {lastTick} and lastval {lastValue} ");

            if (val == lastValue
                && DateTime.Now.Ticks - lastTick < 10000000)
            {
                System.Diagnostics.Debug.WriteLine($"to return Ticks: {DateTime.Now.Ticks - lastTick} ");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"before set new Ticks: {DateTime.Now.Ticks - lastTick} ");
            lastTick = DateTime.Now.Ticks;
            lastValue = val;
            return true;
        }
    }
}
