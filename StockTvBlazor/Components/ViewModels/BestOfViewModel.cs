using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{
	public class BestOfViewModel(SettingsService settingsService, NavigationManager navigationManager) : BaseViewModel(settingsService, navigationManager)
	{
		public string HeaderText
		{
			get
			{
				if (Match.CurrentGame.GameNumber == 1 &&
					Match.CurrentGame.Turns.Count == 0)
				{
					if (_currentSettings.SpielgruppeLetter == string.Empty)
						return $"{(_currentSettings.BlockLocalChanges ? "." : "")}Bahn: {_currentSettings.BahnNummer}";
					else
						return $"{(_currentSettings.BlockLocalChanges ? "." : "")}Bahn: {_currentSettings.SpielgruppeLetter}-{_currentSettings.BahnNummer}";
				}

				return $"0" +
					$"{(_currentSettings.BlockLocalChanges ? "." : "")}Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
			}
		}

		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();
		
	}
}