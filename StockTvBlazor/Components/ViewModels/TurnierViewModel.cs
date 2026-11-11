using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService) :
	BaseViewModel(settingsService, matchService)
{
	// Im Turnier steht Spiel und Kehre von Anfang an im Kopf – auch vor der ersten Kehre und nach einem Neustart.
	// Nach dem letzten Spiel (leeres Spiel ohne Begegnung) steht dort „alle Spiele beendet“.
	public string HeaderText => _isDemoMode
		? $"{HeaderTextBasis}    Spiel: {DemoData.SpielNummer}    Kehre: {DemoData.KehreAnzahl}"
		: CurrentMatch.AlleBegegnungenGespielt
			? $"{HeaderTextBasis}    alle Spiele beendet"
			: $"{HeaderTextBasis}    Spiel: {CurrentMatch.CurrentGame.GameNumber}    Kehre: {LaufendeKehre}";
}
