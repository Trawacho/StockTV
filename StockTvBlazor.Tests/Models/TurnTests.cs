using StockTvBlazor.Models;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Models;

public class TurnTests
{
	private const int TestValue = 42;

	[Fact]
	public void Create_GreenWithLinksRichtung_AssignsToRight()
	{
		var turn = Turn.Create(TestValue, UiSettings.Richtung.Links, isGreen: true);

		Assert.Equal(TestValue, turn.PointsRight);
		Assert.Equal(0, turn.PointsLeft);
	}

	[Fact]
	public void Create_GreenWithRechtsRichtung_AssignsToLeft()
	{
		var turn = Turn.Create(TestValue, UiSettings.Richtung.Rechts, isGreen: true);

		Assert.Equal(0, turn.PointsRight);
		Assert.Equal(TestValue, turn.PointsLeft);
	}

	[Fact]
	public void Create_RedWithLinksRichtung_AssignsToLeft()
	{
		var turn = Turn.Create(TestValue, UiSettings.Richtung.Links, isGreen: false);

		Assert.Equal(0, turn.PointsRight);
		Assert.Equal(TestValue, turn.PointsLeft);
	}

	[Fact]
	public void Create_RedWithRechtsRichtung_AssignsToRight()
	{
		var turn = Turn.Create(TestValue, UiSettings.Richtung.Rechts, isGreen: false);

		Assert.Equal(TestValue, turn.PointsRight);
		Assert.Equal(0, turn.PointsLeft);
	}
}
