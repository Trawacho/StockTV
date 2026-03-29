namespace StockTvBlazor.Components.Models
{
	public class Debounce
	{
		private string? _lastValue;
		private long _lastTick;
		public bool IsDebounceOk(string? val)
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
