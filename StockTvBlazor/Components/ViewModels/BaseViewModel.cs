using StockTvBlazor.Models;
using StockTvBlazor.Networking;
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

	public string InputValue => _matchService.Inputvalue < 0 ? "" : _matchService.Inputvalue.ToString();

	public int LeftPointsSum => CurrentMatch.CurrentGame.LeftPointsSum;
	public int RightPointsSum => CurrentMatch.CurrentGame.RightPointsSum;

	public string LeftPoints => CurrentMatch.CurrentGame.LeftPoints;
	public string RightPoints => CurrentMatch.CurrentGame.RightPoints;

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

	public bool TeamNamesAvailable => !string.IsNullOrEmpty(LeftTeamName);

	public string LeftTeamName
	{
		get
		{
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
			var s = _settingsService.CurrentSettings;

			return CurrentMatch.Begegnungen
				.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber)
				?.TeamNameRight(s.UI.CurrentRichtung == UiSettings.Richtung.Links)
				?? string.Empty;
		}
	}

	#endregion





	[Obsolete("Wurde in den MatchService verschoben")]
	public async Task ProcessKeyAsync(string value)
	{
		throw new NotImplementedException("Diese Methode wurde in den MatchService verschoben. Bitte MatchService.ProcessKeyAsync verwenden.");
	}
}