using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class PunkteeingabePassivTests : BunitContext
{
	[Fact]
	public void PunkteeingabePassiv_Renders()
	{
		var cut = Render<PunkteeingabePassiv>();

		Assert.NotNull(cut.Markup);
		Assert.Contains("punkte-eingabe", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_DisplaysDefaultHelpText()
	{
		var cut = Render<PunkteeingabePassiv>();

		Assert.Contains("Zahl eingeben und / oder * drücken", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_HasTableStructure()
	{
		var cut = Render<PunkteeingabePassiv>();

		Assert.Contains("punkte-table", cut.Markup);
		Assert.Contains("punkte-links", cut.Markup);
		Assert.Contains("punkte-mitte", cut.Markup);
		Assert.Contains("punkte-rechts", cut.Markup);
	}

	[Fact]
	public void PunkteeingabePassiv_HasHilfeCell()
	{
		var cut = Render<PunkteeingabePassiv>();

		Assert.Contains("hilfe-cell", cut.Markup);
		Assert.Contains("hilfe-text", cut.Markup);
	}
}
