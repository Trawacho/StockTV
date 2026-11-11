using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel : IPageViewModel
{
	protected readonly SettingsService _settingsService;

	private readonly MatchService _matchService;

	public event Action? OnViewModelChanged;

	public BaseViewModel(SettingsService settingsService, MatchService matchService)
	{
		_settingsService = settingsService;
		_matchService = matchService;

		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		_matchService.CurrentMatch.OnMatchChanged += HandleMatchChanged;
	}

	private bool _disposed;

	protected bool _isDemoMode;

	public void EnableDemoMode() => _isDemoMode = true;

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_matchService.CurrentMatch.OnMatchChanged -= HandleMatchChanged;
	}


	private void HandleMatchChanged() => OnViewModelChanged?.Invoke();

	private void HandleSettingsChanged() => OnViewModelChanged?.Invoke();

	protected Match CurrentMatch => _matchService.CurrentMatch;

	#region Header

	protected string HeaderTextBasis
	{
		get
		{
			var s = _settingsService.CurrentSettings;
			var prefix = s.General.BlockLocalChanges ? "◆ " : "";

			return string.IsNullOrEmpty(s.General.SpielgruppeLetter)
				? $"{prefix}Bahn: {s.General.BahnNummer}"
				: $"{prefix}Bahn: {s.General.SpielgruppeLetter}-{s.General.BahnNummer}";
		}
	}

	/// <summary>Die Kehre, die gerade gespielt wird: eingetragene Kehren + 1, höchstens die letzte Kehre des Spiels.</summary>
	protected int LaufendeKehre
	{
		get
		{
			var laufend = CurrentMatch.CurrentGame.Turns.Count + 1;
			var max = _settingsService.CurrentSettings.Game.MaxKehrenProSpiel;
			return max > 0 ? Math.Min(laufend, max) : laufend;
		}
	}

	#endregion

	#region Points

	public string InputValue => _isDemoMode ? DemoData.InputValue : (_matchService.Inputvalue < 0 ? "" : _matchService.Inputvalue.ToString());

	public int LeftPointsSum => _isDemoMode ? DemoData.LeftPointsSum : CurrentMatch.CurrentGame.LeftPointsSum;

	public int RightPointsSum => _isDemoMode ? DemoData.RightPointsSum : CurrentMatch.CurrentGame.RightPointsSum;

	public virtual string LeftPoints => _isDemoMode ? DemoData.LeftPoints : CurrentMatch.CurrentGame.LeftPoints;

	public virtual string RightPoints => _isDemoMode ? DemoData.RightPoints : CurrentMatch.CurrentGame.RightPoints;

	#endregion

	#region Layout

	public virtual string GetShellGridStyle()
	{
		if (!TeamNamesAvailable)
			return "grid-template-columns: 100%;";

		var s = _settingsService.CurrentSettings;
		var mid = s.UI.MidColumnWidth;
		var side = (100 - mid) / 2.0;

		return @$"grid-template-columns: {side.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}%
                                          {mid.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}%
                                          {side.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}%;";
	}

	/// <summary>
	/// CSS-Custom-Properties für alle editierbaren Zeilen-/Spaltenwerte aus
	/// UI.TableLayout, an einen Wrapper-Div gehängt und per var() in den .css-Dateien
	/// konsumiert. Enthält absichtlich alle Variablen für alle Modi — ungenutzte
	/// Variablen auf einer Seite sind harmlos.
	/// </summary>
	public string GetTableLayoutCssVariables() => _settingsService.CurrentSettings.UI.TableLayout.ToCssVariables();

	/// <summary>
	/// CSS-Custom-Properties für alle editierbaren Schriftstärken aus UI.CellFontWeight,
	/// analog zu GetTableLayoutCssVariables().
	/// </summary>
	public string GetCellFontWeightCssVariables() => _settingsService.CurrentSettings.UI.CellFontWeight.ToCssVariables();

	public bool TeamNamesAvailable => _isDemoMode || !string.IsNullOrEmpty(LeftTeamName);

	public string LeftTeamName => _isDemoMode ? DemoData.LeftTeamName : TeamNamenFuerKehre().Links;

	public string RightTeamName => _isDemoMode ? DemoData.RightTeamName : TeamNamenFuerKehre().Rechts;

	/// <summary>Namen der laufenden Begegnung; die Anspiel-Markierung wechselt mit jeder Kehre die Seite.</summary>
	private (string Links, string Rechts) TeamNamenFuerKehre()
	{
		var begegnung = CurrentMatch.Begegnungen.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber);
		if (begegnung is null) return (string.Empty, string.Empty);

		var rechtsNachLinks = _settingsService.CurrentSettings.UI.CurrentRichtung == UiSettings.Richtung.Links;
		return AnspielWechsel.FuerKehre(begegnung.TeamNameLeft(rechtsNachLinks), begegnung.TeamNameRight(rechtsNachLinks), LaufendeKehre);
	}

	#endregion

	#region Team Frame (Anspiel-Indikator)

	/// <summary>
	/// Prüft ob der Teamname mit » oder « markiert ist (Anspiel-Indikator)
	/// </summary>
	public bool HasAnspiel(string teamName)
	{
		return AnspielWechsel.IstMarkiert(teamName);
	}

	/// <summary>Rahmen statt Sonderzeichen - zentrale Einstellung, gilt für alle Themes.</summary>
	private bool ShowAnspielFrame => _settingsService.CurrentSettings.UI.AnspielFrame.ShowFrame;

	/// <summary>
	/// Gibt die CSS-Klasse für den Teamrahmen zurück, wenn Anspiel vorhanden UND Rahmen aktiviert
	/// </summary>
	public string GetTeamFrameClass(string teamName)
	{
		return HasAnspiel(teamName) && ShowAnspielFrame ? "team-has-anspiel" : "";
	}

	/// <summary>
	/// Gibt den bereinigten Teamnamen zurück (ohne » oder «)
	/// Nur wenn Rahmen aktiviert ist, sonst werden Sonderzeichen behalten
	/// </summary>
	public string GetCleanTeamName(string teamName)
	{
		if (string.IsNullOrEmpty(teamName)) return teamName;

		if (!ShowAnspielFrame) return teamName; // Sonderzeichen behalten wenn Rahmen nicht aktiv

		return AnspielWechsel.OhneZeichen(teamName);
	}

	#endregion
}