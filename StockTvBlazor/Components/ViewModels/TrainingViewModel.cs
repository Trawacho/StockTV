using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;

namespace StockTvBlazor.Components.ViewModels
{

	public class TrainingViewModel(Settings configuration, NavigationManager navigationManager) : BaseViewModel(configuration, navigationManager)
	{
		public string HeaderText
		{
			get
			{
				if (Match.CurrentGame.GameNumber == 1 &&
					Match.CurrentGame.Turns.Count == 0)
				{
					return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.BahnNummer}";
				}
				else
				{
					return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.BahnNummer}   Kehre: {Match.CurrentGame.Turns.Count}";
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
