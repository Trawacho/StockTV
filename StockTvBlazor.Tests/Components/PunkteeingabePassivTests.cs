using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class PunkteeingabePassivTests : TestContext
{
	[Fact]
	public void PunkteeingabePassiv_Renders()
	{
		var cut = RenderComponent<PunkteeingabePassiv>();

		Assert.NotNull(cut.Markup);
		Assert.Contains("punkte-eingabe", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_DisplaysDefaultHelpText()
	{
		var cut = RenderComponent<PunkteeingabePassiv>();

		Assert.Contains("Zahl eingeben und / oder * drücken", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_HasTableStructure()
	{
		var cut = RenderComponent<PunkteeingabePassiv>();

		Assert.Contains("punkte-table", cut.Markup);
		Assert.Contains("punkte-links", cut.Markup);
		Assert.Contains("punkte-mitte", cut.Markup);
		Assert.Contains("punkte-rechts", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_HasHilfeCell()
	{
		var cut = RenderComponent<PunkteeingabePassiv>();

		Assert.Contains("hilfe-cell", cut.Markup);
		Assert.Contains("hilfe-text", cut.Markup);
	}
}
