using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class PunkteAnzeigeTests : BunitContext
{
	[Fact]
	public void PunkteAnzeige_RenderWithValues()
	{
		var cut = Render<PunkteAnzeige>();

		Assert.NotNull(cut.Markup);
		Assert.Contains("punkte-anzeige", cut.Markup);
	}

	[Fact]
	public void PunkteAnzeige_HasSeparator()
	{
		var cut = Render<PunkteAnzeige>();

		Assert.Contains(":", cut.Markup);
	}

	[Fact]
	public void PunkteAnzeige_HasCorrectClassNames()
	{
		var cut = Render<PunkteAnzeige>();

		Assert.Contains("leftpoint-sum", cut.Markup);
		Assert.Contains("rightpoint-sum", cut.Markup);
		Assert.Contains("trenner", cut.Markup);
	}
}
