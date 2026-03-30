using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{

	public class TrainingViewModel(SettingsService settingsSerivce, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisherService) :
		BaseViewModel(settingsSerivce, matchService, navigationManager, publisherService)
	{
		public string HeaderText => Match.CurrentGame.Turns.Count == 0
			? base.HeaderTextBasis
			: $"{HeaderTextBasis}     Kehre: {Match.CurrentGame.Turns.Count}";
	}
}
