using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class PunkteAnzeigeTests : TestContext
{
	[Fact]
	public void PunkteAnzeige_RenderWithValues()
	{
		var cut = RenderComponent<PunkteAnzeige>();

		Assert.NotNull(cut.Markup);
		Assert.Contains("punkte-anzeige", cut.Markup);
	}

	[Fact]
	public void PunkteAnzeige_HasSeparator()
	{
		var cut = RenderComponent<PunkteAnzeige>();

		Assert.Contains(":", cut.Markup);
	}

	[Fact]
	public void PunkteAnzeige_HasCorrectClassNames()
	{
		var cut = RenderComponent<PunkteAnzeige>();

		Assert.Contains("leftpoint-sum", cut.Markup);
		Assert.Contains("rightpoint-sum", cut.Markup);
		Assert.Contains("trenner", cut.Markup);
	}
}
