using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Settings;

public class CellFontWeightSettingsTests
{
	[Fact]
	public void Defaults_MatchCurrentHardcodedCssValues()
	{
		var weights = new CellFontWeightSettings();

		Assert.Equal(400, weights.TeamNameWeight);
		Assert.Equal(700, weights.HeaderRowWeight);

		Assert.Equal(700, weights.PointsSumWeight);
		Assert.Equal(700, weights.CurrentPointsWeight);
		Assert.Equal(700, weights.InputValueWeight);

		Assert.Equal(600, weights.SeparatorWeight);
		Assert.Equal(200, weights.BestOfMatchPointsWeight);

		Assert.Equal(600, weights.ZielSpielernameWeight);
		Assert.Equal(500, weights.ZielVersucheWeight);
		Assert.Equal(500, weights.ZielGesamtWeight);
		Assert.Equal(500, weights.ZielLetzterWertWeight);
		Assert.Equal(500, weights.ZielEingabeWeight);
		Assert.Equal(500, weights.ZielGesamtpunkteWeight);
		Assert.Equal(500, weights.ZielSummeWeight);
	}

	[Fact]
	public void ToCssVariables_ContainsExpectedNamesAndValues()
	{
		var weights = new CellFontWeightSettings();
		var css = weights.ToCssVariables();

		Assert.Contains("--fontweight-teamname:400;", css);
		Assert.Contains("--fontweight-header-row:700;", css);
		Assert.Contains("--fontweight-points-sum:700;", css);
		Assert.Contains("--fontweight-current-points:700;", css);
		Assert.Contains("--fontweight-input-value:700;", css);
		Assert.Contains("--fontweight-separator:600;", css);
		Assert.Contains("--fontweight-bestof-matchpoints:200;", css);
		Assert.Contains("--fontweight-ziel-spielername:600;", css);
		Assert.Contains("--fontweight-ziel-versuche:500;", css);
		Assert.Contains("--fontweight-ziel-gesamt:500;", css);
		Assert.Contains("--fontweight-ziel-letzter-wert:500;", css);
		Assert.Contains("--fontweight-ziel-eingabe:500;", css);
		Assert.Contains("--fontweight-ziel-gesamtpunkte:500;", css);
		Assert.Contains("--fontweight-ziel-summe:500;", css);
	}

	[Fact]
	public void ToCssVariables_ReflectsChangedValues()
	{
		var weights = new CellFontWeightSettings { ZielSummeWeight = 900 };

		Assert.Contains("--fontweight-ziel-summe:900;", weights.ToCssVariables());
	}
}
