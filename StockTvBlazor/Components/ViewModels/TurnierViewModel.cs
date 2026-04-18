using StockTvBlazor.Services;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService, NetMqPublisherService publisherService) :
	BaseViewModel(settingsService, matchService, publisherService)
{
	public string HeaderText => (CurrentMatch.CurrentGame.GameNumber == 1 && CurrentMatch.CurrentGame.Turns.Count == 0)
			? HeaderTextBasis
			: $"{HeaderTextBasis}   Spiel: {CurrentMatch.CurrentGame.GameNumber}   Kehre: {CurrentMatch.CurrentGame.Turns.Count}";
}