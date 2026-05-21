using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel : IDisposable
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
			var prefix = s.General.BlockLocalChanges ? "." : "";

			return string.IsNullOrEmpty(s.General.SpielgruppeLetter)
				? $"{prefix}Bahn: {s.General.BahnNummer}"
				: $"{prefix}Bahn: {s.General.SpielgruppeLetter}-{s.General.BahnNummer}";
		}
	}

	#endregion

	#region Points

	public string InputValue => _isDemoMode ? DemoData.InputValue : (_matchService.Inputvalue < 0 ? "" : _matchService.Inputvalue.ToString());

	public int LeftPointsSum => _isDemoMode ? DemoData.LeftPointsSum : CurrentMatch.CurrentGame.LeftPointsSum;

	public int RightPointsSum => _isDemoMode ? DemoData.RightPointsSum : CurrentMatch.CurrentGame.RightPointsSum;

	public string LeftPoints => _isDemoMode ? DemoData.LeftPoints : CurrentMatch.CurrentGame.LeftPoints;

	public string RightPoints => _isDemoMode ? DemoData.RightPoints : CurrentMatch.CurrentGame.RightPoints;

	#endregion

	#region Layout

	public string GetShellGridStyle()
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

	public bool TeamNamesAvailable => _isDemoMode || !string.IsNullOrEmpty(LeftTeamName);

	public string LeftTeamName
	{
		get
		{
			if (_isDemoMode) return DemoData.LeftTeamName;

			var s = _settingsService.CurrentSettings;

			return CurrentMatch.Begegnungen
				.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber)
				?.TeamNameLeft(s.UI.CurrentRichtung == UiSettings.Richtung.Links)
				?? string.Empty;
		}
	}

	public string RightTeamName
	{
		get
		{
			if (_isDemoMode) return DemoData.RightTeamName;

			var s = _settingsService.CurrentSettings;

			return CurrentMatch.Begegnungen
				.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber)
				?.TeamNameRight(s.UI.CurrentRichtung == UiSettings.Richtung.Links)
				?? string.Empty;
		}
	}

	#endregion
}