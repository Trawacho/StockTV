using System;

namespace StockTV.Classes
{
    public static class Debounce
    {
        public static uint lastValue;
        public static long lastTick;
        public static bool IsDebounceOk(object sender, uint val)
        {
            if (val == lastValue
                && DateTime.Now.Ticks - lastTick < 10000000)
            {
                return false;
            }

            lastTick = DateTime.Now.Ticks;
            lastValue = val;
            return true;

        }
    }
}
