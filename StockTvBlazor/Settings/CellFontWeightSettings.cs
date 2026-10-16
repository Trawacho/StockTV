namespace StockTvBlazor.Settings;

/// <summary>
/// Editierbare Schriftstärken (CSS font-weight) der bestehenden Punkte-/Team-/Ziel-Zellen von
/// Training, Turnier, BestOf und Ziel/Ziel2. Die Property-Defaults entsprechen den heute in den
/// .css-Dateien hart kodierten Werten und dienen als Reset-Ziel — mit einer Ausnahme:
/// ZielSpielernameWeight war zuvor 650, wurde aber auf 600 (nächste Standardstufe) vereinheitlicht,
/// da das Dropdown bewusst nur die 9 CSS-Standardstufen (100er-Schritte) anbietet. Rein lokale
/// UI-Einstellung, nicht Teil des NetMQ SetSettings/GetSettings-Protokolls.
/// </summary>
public class CellFontWeightSettings
{
	// .teamname — Turnier/BestOf
	public double TeamNameWeight { get; set; } = 400;

	// .score-top-row — Training/Turnier/BestOf
	public double HeaderRowWeight { get; set; } = 700;

	// .left-point-sum/.right-point-sum — Training/Turnier/BestOf (vormals Teil von .score-cell)
	public double PointsSumWeight { get; set; } = 700;

	// .left-points/.right-points — Training/Turnier/BestOf (vormals Teil von .score-cell)
	public double CurrentPointsWeight { get; set; } = 700;

	// .input-value — Training/Turnier/BestOf (vormals Teil von .score-cell)
	public double InputValueWeight { get; set; } = 700;

	// :root .seperator — Training/Turnier/BestOf
	public double SeparatorWeight { get; set; } = 600;

	// .bestof-mid-grid .left-match-points/.right-match-points — BestOf
	public double BestOfMatchPointsWeight { get; set; } = 200;

	// .ziel-spielername — Ziel/Ziel2
	public double ZielSpielernameWeight { get; set; } = 600;

	// .ziel-versuche — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielVersucheWeight { get; set; } = 500;

	// .ziel-gesamt — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielGesamtWeight { get; set; } = 500;

	// .ziel-col-a — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielLetzterWertWeight { get; set; } = 500;

	// .ziel-col-b — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielEingabeWeight { get; set; } = 500;

	// .ziel-col-c — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielGesamtpunkteWeight { get; set; } = 500;

	// .ziel-summe — Ziel/Ziel2 (vormals Teil von .ziel-cell)
	public double ZielSummeWeight { get; set; } = 500;

	/// <summary>
	/// Rendert alle Werte als CSS-Custom-Properties (";"-getrennt), zum Einhängen in ein
	/// style-Attribut. Wird von BaseViewModel und ZielViewModel gemeinsam genutzt, da
	/// ZielViewModel nicht von BaseViewModel erbt.
	/// </summary>
	public string ToCssVariables()
	{
		var ci = System.Globalization.CultureInfo.InvariantCulture;
		string W(double v) => v.ToString("0", ci);

		return $"--fontweight-teamname:{W(TeamNameWeight)};--fontweight-header-row:{W(HeaderRowWeight)};" +
			   $"--fontweight-points-sum:{W(PointsSumWeight)};--fontweight-current-points:{W(CurrentPointsWeight)};--fontweight-input-value:{W(InputValueWeight)};" +
			   $"--fontweight-separator:{W(SeparatorWeight)};--fontweight-bestof-matchpoints:{W(BestOfMatchPointsWeight)};" +
			   $"--fontweight-ziel-spielername:{W(ZielSpielernameWeight)};" +
			   $"--fontweight-ziel-versuche:{W(ZielVersucheWeight)};--fontweight-ziel-gesamt:{W(ZielGesamtWeight)};" +
			   $"--fontweight-ziel-letzter-wert:{W(ZielLetzterWertWeight)};--fontweight-ziel-eingabe:{W(ZielEingabeWeight)};" +
			   $"--fontweight-ziel-gesamtpunkte:{W(ZielGesamtpunkteWeight)};--fontweight-ziel-summe:{W(ZielSummeWeight)};";
	}
}
