using StockTvBlazor.Models;
using StockTvBlazor.Services;

namespace StockTvBlazor.Tests;

public class DebounceTests
{
	[Fact]
	public void IsDebounceOk_AllowsFirstPress()
	{
		var clock = new FakeSystemClock();
		var debounce = new Debounce(clock);

		var result = debounce.IsDebounceOk("5");

		Assert.True(result, "First press should always be allowed");
	}

	[Fact]
	public void IsDebounceOk_BlocksDuplicateWithinWindow()
	{
		var clock = new FakeSystemClock(initialTicks: 0);
		var debounce = new Debounce(clock);

		debounce.IsDebounceOk("5");
		clock.SetTicks(5_000_000);  // Advance 0.5s (< 1s window of 10,000,000 ticks)

		var result = debounce.IsDebounceOk("5");

		Assert.False(result, "Same value within 1s window should be blocked");
	}

	[Fact]
	public void IsDebounceOk_AllowsDuplicateAfterWindow()
	{
		var clock = new FakeSystemClock(initialTicks: 0);
		var debounce = new Debounce(clock);

		debounce.IsDebounceOk("5");
		clock.SetTicks(10_000_001);  // Advance 1.00000001s (> 1s window)

		var result = debounce.IsDebounceOk("5");

		Assert.True(result, "Same value after 1s window should be allowed");
	}

	[Fact]
	public void IsDebounceOk_AllowsDifferentValueWithinWindow()
	{
		var clock = new FakeSystemClock(initialTicks: 0);
		var debounce = new Debounce(clock);

		debounce.IsDebounceOk("5");
		clock.SetTicks(5_000_000);  // Advance 0.5s (< 1s window)

		var result = debounce.IsDebounceOk("6");  // Different value

		Assert.True(result, "Different value within 1s window should be allowed");
	}
}

internal class FakeSystemClock : ISystemClock
{
	private long _currentTicks;

	public FakeSystemClock(long initialTicks = 0)
	{
		_currentTicks = initialTicks;
	}

	public long GetTicks() => _currentTicks;

	public void SetTicks(long ticks) => _currentTicks = ticks;
}
