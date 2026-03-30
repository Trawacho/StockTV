using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisherService) : 
	BaseViewModel(settingsService, matchService, navigationManager, publisherService)
{
	public string HeaderText
	{
		get
		{
			if (Match.CurrentGame.GameNumber == 1 &&
				Match.CurrentGame.Turns.Count == 0)
			{
				if (_settingsService.CurrentSettings.SpielgruppeLetter == string.Empty)
					return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.BahnNummer}";
				else
					return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.SpielgruppeLetter}-{_settingsService.CurrentSettings.BahnNummer}";
			}

			if (_settingsService.CurrentSettings.SpielgruppeLetter == string.Empty)
				return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
			else
				return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Bahn: {_settingsService.CurrentSettings.SpielgruppeLetter}-{_settingsService.CurrentSettings.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";

		}
	}
	
}