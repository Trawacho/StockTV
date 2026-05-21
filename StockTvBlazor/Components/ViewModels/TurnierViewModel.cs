using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService) :
	BaseViewModel(settingsService, matchService)
{
	public string HeaderText => _isDemoMode
		? $"{HeaderTextBasis}   Spiel: {DemoData.SpielNummer}   Kehre: {DemoData.KehreAnzahl}"
		: (CurrentMatch.CurrentGame.GameNumber == 1 && CurrentMatch.CurrentGame.Turns.Count == 0)
			? HeaderTextBasis
			: $"{HeaderTextBasis}   Spiel: {CurrentMatch.CurrentGame.GameNumber}   Kehre: {CurrentMatch.CurrentGame.Turns.Count}";
}
