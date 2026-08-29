using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class PunkteEingabeTests : TestContext
{
	[Fact]
	public void PunkteEingabe_Renders()
	{
		var cut = RenderComponent<PunkteEingabe>();

		Assert.NotNull(cut.Markup);
		Assert.Contains("punkte-eingabe", cut.Markup);
	}

	[Fact]
	public void PunkteEingabe_HasInputElement()
	{
		var cut = RenderComponent<PunkteEingabe>();

		Assert.Contains("punkteInput", cut.Markup);
		Assert.Contains("punkte-input", cut.Markup);
	}

	[Fact]
	public void PunkteEingabe_RendersHelpText()
	{
		var cut = RenderComponent<PunkteEingabe>();

		Assert.Contains("Zahl eingeben", cut.Markup);
	}

	[Fact]
	public void PunkteEingabe_HasTableStructure()
	{
		var cut = RenderComponent<PunkteEingabe>();

		Assert.Contains("punkte-table", cut.Markup);
		Assert.Contains("punkte-links", cut.Markup);
		Assert.Contains("punkte-mitte", cut.Markup);
		Assert.Contains("punkte-rechts", cut.Markup);
	}
}
