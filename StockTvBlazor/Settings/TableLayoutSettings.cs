namespace StockTvBlazor.Settings;

/// <summary>
/// Editierbare Zeilenhöhen/Spaltenbreiten (in %) der bestehenden CSS-Grids von
/// Training, Turnier, BestOf und Ziel/Ziel2. Die Property-Defaults entsprechen exakt
/// den heute in den .css-Dateien hart kodierten Werten und dienen als Reset-Ziel.
/// Rein lokale UI-Einstellung, nicht Teil des NetMQ SetSettings/GetSettings-Protokolls.
/// </summary>
public class TableLayoutSettings
{
	// Zeilen (Height) — .scoreboard-center, Training/Turnier/BestOf
	public double CenterRowHeaderHeight { get; set; } = 13;
	public double CenterRowMidHeight { get; set; } = 69;
	public double CenterRowBottomHeight { get; set; } = 18;

	// Spalten (Width) — .score-bottom-grid, Training/Turnier/BestOf
	public double BottomGridLeftWidth { get; set; } = 42.5;
	public double BottomGridMidWidth { get; set; } = 15;
	public double BottomGridRightWidth { get; set; } = 42.5;

	// Spalten (Width) — .mid-grid-3, Training + Turnier (geteilt)
	public double MidGrid3LeftWidth { get; set; } = 47;
	public double MidGrid3MidWidth { get; set; } = 6;
	public double MidGrid3RightWidth { get; set; } = 47;

	// Spalten (Width) — .bestof-mid-grid, BestOf
	public double BestOfLeftWidth { get; set; } = 32;
	public double BestOfLeftMatchWidth { get; set; } = 15;
	public double BestOfSeparatorWidth { get; set; } = 6;
	public double BestOfRightMatchWidth { get; set; } = 15;
	public double BestOfRightWidth { get; set; } = 32;

	// Zeilen (Height) — .ziel-main-grid, Ziel/Ziel2
	public double ZielNameRowHeight { get; set; } = 10;
	public double ZielBlockRowHeight { get; set; } = 90;

	// Zeilen (Height) — .ziel-block, Ziel/Ziel2
	public double ZielBlockTopHeight { get; set; } = 20;
	public double ZielBlockMidHeight { get; set; } = 60;
	public double ZielBlockBottomHeight { get; set; } = 20;

	// Spalten (Width) — .ziel-row-top, Ziel/Ziel2
	public double ZielRowTopVersucheWidth { get; set; } = 70;
	public double ZielRowTopGesamtWidth { get; set; } = 30;

	// Spalten (Width) — .ziel-row-mid, Ziel/Ziel2
	public double ZielRowMidAWidth { get; set; } = 35;
	public double ZielRowMidBWidth { get; set; } = 35;
	public double ZielRowMidCWidth { get; set; } = 30;

	/// <summary>
	/// Rendert alle Werte als CSS-Custom-Properties (";"-getrennt), zum Einhängen in ein
	/// style-Attribut. Wird von BaseViewModel und ZielViewModel gemeinsam genutzt, da
	/// ZielViewModel nicht von BaseViewModel erbt.
	/// </summary>
	public string ToCssVariables()
	{
		var ci = System.Globalization.CultureInfo.InvariantCulture;
		string P(double v) => v.ToString("0.####", ci) + "%";

		return $"--layout-center-row-header-height:{P(CenterRowHeaderHeight)};--layout-center-row-mid-height:{P(CenterRowMidHeight)};--layout-center-row-bottom-height:{P(CenterRowBottomHeight)};" +
			   $"--layout-bottom-left-width:{P(BottomGridLeftWidth)};--layout-bottom-mid-width:{P(BottomGridMidWidth)};--layout-bottom-right-width:{P(BottomGridRightWidth)};" +
			   $"--layout-midgrid3-left-width:{P(MidGrid3LeftWidth)};--layout-midgrid3-mid-width:{P(MidGrid3MidWidth)};--layout-midgrid3-right-width:{P(MidGrid3RightWidth)};" +
			   $"--layout-bestof-left-width:{P(BestOfLeftWidth)};--layout-bestof-left-match-width:{P(BestOfLeftMatchWidth)};--layout-bestof-sep-width:{P(BestOfSeparatorWidth)};--layout-bestof-right-match-width:{P(BestOfRightMatchWidth)};--layout-bestof-right-width:{P(BestOfRightWidth)};" +
			   $"--layout-ziel-name-row-height:{P(ZielNameRowHeight)};--layout-ziel-block-row-height:{P(ZielBlockRowHeight)};" +
			   $"--layout-ziel-block-top-height:{P(ZielBlockTopHeight)};--layout-ziel-block-mid-height:{P(ZielBlockMidHeight)};--layout-ziel-block-bottom-height:{P(ZielBlockBottomHeight)};" +
			   $"--layout-ziel-row-top-versuche-width:{P(ZielRowTopVersucheWidth)};--layout-ziel-row-top-gesamt-width:{P(ZielRowTopGesamtWidth)};" +
			   $"--layout-ziel-row-mid-a-width:{P(ZielRowMidAWidth)};--layout-ziel-row-mid-b-width:{P(ZielRowMidBWidth)};--layout-ziel-row-mid-c-width:{P(ZielRowMidCWidth)};";
	}
}
