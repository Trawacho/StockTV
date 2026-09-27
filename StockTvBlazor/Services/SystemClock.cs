namespace StockTvBlazor.Services;

public class SystemClock : ISystemClock
{
	public long GetTicks() => DateTime.Now.Ticks;
}
