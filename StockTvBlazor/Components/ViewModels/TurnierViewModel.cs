using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService) :
	BaseViewModel(settingsService, matchService)
{
	// Im Turnier steht Spiel und Kehre von Anfang an im Kopf – auch vor der ersten Kehre und nach einem Neustart.
	public string HeaderText => _isDemoMode
		? $"{HeaderTextBasis}    Spiel: {DemoData.SpielNummer}    Kehre: {DemoData.KehreAnzahl}"
		: $"{HeaderTextBasis}    Spiel: {CurrentMatch.CurrentGame.GameNumber}    Kehre: {LaufendeKehre}";
}
