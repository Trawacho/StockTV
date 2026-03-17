using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{

	public class TrainingViewModel(SettingsService settingsSerivce, NavigationManager navigationManager) : BaseViewModel(settingsSerivce, navigationManager)
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
		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();
		
	}
}
