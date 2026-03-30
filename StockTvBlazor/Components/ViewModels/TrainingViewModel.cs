using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{

	public class TrainingViewModel(SettingsService settingsSerivce, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisherService) : 
		BaseViewModel(settingsSerivce, matchService, navigationManager, publisherService)
	{
		public string HeaderText
		{
			get
			{
				if (Match.CurrentGame.GameNumber == 1 &&
					Match.CurrentGame.Turns.Count == 0)
				{
					return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.BahnNummer}";
				}
				else
				{
					return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.BahnNummer}   Kehre: {Match.CurrentGame.Turns.Count}";
				}
			}
		}
	}
}
