namespace StockTvBlazor.Components.ViewModels
{
	public static class Debounce
	{
		private static string? _lastValue;
		private static long _lastTick;
		public static bool IsDebounceOk(string? val)
		{
			if (val == _lastValue
				&& DateTime.Now.Ticks - _lastTick < 10000000)
			{
				return false;
			}

			_lastTick = DateTime.Now.Ticks;
			_lastValue = val;
			return true;

		}
	}
}
