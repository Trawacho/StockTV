using StockTvBlazor.Models;

namespace StockTvBlazor.Tests.Models;

public class AnspielWechselTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(5)]
	public void Ungerade_Kehre_laesst_die_gesendete_Markierung(int kehre)
	{
		Assert.Equal(("Alpha", "Beta »"), AnspielWechsel.FuerKehre("Alpha", "Beta »", kehre));
		Assert.Equal(("« Alpha", "Beta"), AnspielWechsel.FuerKehre("« Alpha", "Beta", kehre));
	}

	[Theory]
	[InlineData(2)]
	[InlineData(4)]
	[InlineData(6)]
	public void Gerade_Kehre_wechselt_die_Markierung_nach_aussen_auf_die_andere_Seite(int kehre)
	{
		Assert.Equal(("« Alpha", "Beta"), AnspielWechsel.FuerKehre("Alpha", "Beta »", kehre));
		Assert.Equal(("Alpha", "Beta »"), AnspielWechsel.FuerKehre("« Alpha", "Beta", kehre));
	}

	[Fact]
	public void Zeichen_auf_der_falschen_Seite_wird_trotzdem_erkannt()
	{
		// Je nach Richtung kommt die Markierung mal vorn, mal hinten an – entscheidend ist nur, wer sie trägt.
		Assert.Equal(("Alpha", "Beta »"), AnspielWechsel.FuerKehre("Alpha »", "Beta", 2));
	}

	[Theory]
	[InlineData("Alpha", "Beta")]
	[InlineData("« Alpha", "Beta »")]
	[InlineData("", "Beta »")]
	public void Ohne_eindeutige_Markierung_bleibt_alles(string links, string rechts)
	{
		Assert.Equal((links, rechts), AnspielWechsel.FuerKehre(links, rechts, 2));
	}

	[Theory]
	[InlineData("Beta »", true)]
	[InlineData("« Alpha", true)]
	[InlineData("Alpha", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void IstMarkiert_erkennt_beide_Zeichen(string? name, bool erwartet)
	{
		Assert.Equal(erwartet, AnspielWechsel.IstMarkiert(name));
	}

	[Fact]
	public void OhneZeichen_entfernt_Markierung_und_Leerraum()
	{
		Assert.Equal("Beta", AnspielWechsel.OhneZeichen("Beta »"));
		Assert.Equal("Alpha", AnspielWechsel.OhneZeichen("« Alpha"));
	}
}
