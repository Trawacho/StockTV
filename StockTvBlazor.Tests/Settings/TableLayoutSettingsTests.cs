using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Settings;

public class TableLayoutSettingsTests
{
	[Fact]
	public void Defaults_MatchCurrentHardcodedCssValues()
	{
		var layout = new TableLayoutSettings();

		Assert.Equal(13, layout.CenterRowHeaderHeight);
		Assert.Equal(69, layout.CenterRowMidHeight);
		Assert.Equal(18, layout.CenterRowBottomHeight);

		Assert.Equal(42.5, layout.BottomGridLeftWidth);
		Assert.Equal(15, layout.BottomGridMidWidth);
		Assert.Equal(42.5, layout.BottomGridRightWidth);

		Assert.Equal(47, layout.MidGrid3LeftWidth);
		Assert.Equal(6, layout.MidGrid3MidWidth);
		Assert.Equal(47, layout.MidGrid3RightWidth);

		Assert.Equal(32, layout.BestOfLeftWidth);
		Assert.Equal(15, layout.BestOfLeftMatchWidth);
		Assert.Equal(6, layout.BestOfSeparatorWidth);
		Assert.Equal(15, layout.BestOfRightMatchWidth);
		Assert.Equal(32, layout.BestOfRightWidth);

		Assert.Equal(10, layout.ZielNameRowHeight);
		Assert.Equal(90, layout.ZielBlockRowHeight);

		Assert.Equal(20, layout.ZielBlockTopHeight);
		Assert.Equal(60, layout.ZielBlockMidHeight);
		Assert.Equal(20, layout.ZielBlockBottomHeight);

		Assert.Equal(70, layout.ZielRowTopVersucheWidth);
		Assert.Equal(30, layout.ZielRowTopGesamtWidth);

		Assert.Equal(35, layout.ZielRowMidAWidth);
		Assert.Equal(35, layout.ZielRowMidBWidth);
		Assert.Equal(30, layout.ZielRowMidCWidth);
	}

	[Theory]
	[InlineData(nameof(TableLayoutSettings.CenterRowHeaderHeight), nameof(TableLayoutSettings.CenterRowMidHeight), nameof(TableLayoutSettings.CenterRowBottomHeight))]
	[InlineData(nameof(TableLayoutSettings.BottomGridLeftWidth), nameof(TableLayoutSettings.BottomGridMidWidth), nameof(TableLayoutSettings.BottomGridRightWidth))]
	[InlineData(nameof(TableLayoutSettings.MidGrid3LeftWidth), nameof(TableLayoutSettings.MidGrid3MidWidth), nameof(TableLayoutSettings.MidGrid3RightWidth))]
	public void Defaults_ThreeColumnGroups_SumTo100(string a, string b, string c)
	{
		var layout = new TableLayoutSettings();
		var type = typeof(TableLayoutSettings);
		double Get(string name) => (double)type.GetProperty(name)!.GetValue(layout)!;

		Assert.Equal(100, Get(a) + Get(b) + Get(c), precision: 6);
	}

	[Fact]
	public void ToCssVariables_ContainsExpectedNamesAndValues()
	{
		var layout = new TableLayoutSettings();
		var css = layout.ToCssVariables();

		Assert.Contains("--layout-center-row-header-height:13%;", css);
		Assert.Contains("--layout-center-row-mid-height:69%;", css);
		Assert.Contains("--layout-center-row-bottom-height:18%;", css);
		Assert.Contains("--layout-bestof-sep-width:6%;", css);
		Assert.Contains("--layout-ziel-row-mid-c-width:30%;", css);
	}

	[Fact]
	public void ToCssVariables_ReflectsChangedValues()
	{
		var layout = new TableLayoutSettings { CenterRowHeaderHeight = 25.5 };

		Assert.Contains("--layout-center-row-header-height:25.5%;", layout.ToCssVariables());
	}
}
