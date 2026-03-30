using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisherService) :
	BaseViewModel(settingsService, matchService, navigationManager, publisherService)
{
	public string HeaderText => (Match.CurrentGame.GameNumber == 1 && Match.CurrentGame.Turns.Count == 0)
			? HeaderTextBasis
			: $"{HeaderTextBasis}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
}