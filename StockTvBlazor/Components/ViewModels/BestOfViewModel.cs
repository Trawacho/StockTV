using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{
	public class BestOfViewModel(SettingsService settingsService, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisherService) :
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

				return $"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
			}
		}

		public new string LeftPoints => 
			(base.Match.CurrentGame.Turns.Count == 0 && base.Match.CurrentGame.GameNumber > 1) 
				? base.Match.LeftPointsOverAll.ToString() 
				: base.LeftPoints;

		public new string RightPoints => 
			(base.Match.CurrentGame.Turns.Count == 0 && base.Match.CurrentGame.GameNumber > 1) 
				? base.Match.RightPointsOverAll.ToString() 
				: base.RightPoints;
		
		public int LeftMatchPoints => base.Match.MatchPointsLeft;
		
		public int RightMatchPoints => base.Match.MatchPointsRight;
	}
}