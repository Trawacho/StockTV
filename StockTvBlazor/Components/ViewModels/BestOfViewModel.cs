using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{
	public class BestOfViewModel(SettingsService settingsService, NavigationManager navigationManager, NetMqPublisherService publisherService) : BaseViewModel(settingsService, navigationManager, publisherService)
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

				return $"0" +
					$"{(_settingsService.CurrentSettings.BlockLocalChanges ? "." : "")}Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
			}
		}

		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;

		public string LeftTeamName
		{
			get
			{
				return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
										?.TeamNameLeft(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.LINKS)
										?? string.Empty;
			}
		}

		public string RightTeamName
		{
			get
			{
				return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
										?.TeamNameLeft(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.RECHTS)
										?? string.Empty;
			}
		}
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();

	}
}