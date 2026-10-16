using StockTvBlazor.Services;

namespace StockTvBlazor.Models;

public class Debounce
{
	private string? _lastValue;
	private long _lastTick;
	private readonly ISystemClock _clock;

	public Debounce(ISystemClock? clock = null)
	{
		_clock = clock ?? new SystemClock();
	}

	public bool IsDebounceOk(string? val)
	{
		long currentTick = _clock.GetTicks();
		if (val == _lastValue
			&& currentTick - _lastTick < 10000000)
		{
			return false;
		}

		_lastTick = currentTick;
		_lastValue = val;
		return true;
	}
}