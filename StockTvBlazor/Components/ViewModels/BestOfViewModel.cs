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
		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins
		{
			get
			{
				if(base.Match.CurrentGame.Turns.Count == 0 && base.Match.CurrentGame.GameNumber > 1)
				{
					return base.Match.LeftPointsOverAll.ToString();
				}
				else
				{
					return base.Match.CurrentGame.LeftPoints;
				}
			}
		}

		public string RightPoints
		{
			get
			{
				if (base.Match.CurrentGame.Turns.Count == 0 && base.Match.CurrentGame.GameNumber > 1)
				{
					return base.Match.RightPointsOverAll.ToString();
				}
				else
				{
					return base.Match.CurrentGame.RightPoints;
				}
			}
		}
		
		public int LeftMatchPoints => base.Match.MatchPointsLeft;
		public int RightMatchPoints => base.Match.MatchPointsRight;
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();
	}
}