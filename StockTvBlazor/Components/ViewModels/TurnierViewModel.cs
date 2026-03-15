using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Components.ViewModels
{
	public class TurnierViewModel : BaseViewModel
	{

		public TurnierViewModel(Models.Settings settings, NavigationManager navigationManager) : base(settings, navigationManager)
		{
			Match.TurnsChanged += Match_TurnsChanged;
		}

		private void Match_TurnsChanged(object? sender, EventArgs e)
		{
			if (sender is Models.Match m)
			{
				//todo: implement broadcasting
				if (_settings.MessageVersion == 0)
				{
					//_settings.PublishGameResult(m.Serialize());
				}
				else if (_settings.MessageVersion == 1)
				{
					//_settings.PublishGameResult(m.SerializeJson());
				}
			}
		}

		public string HeaderText
		{
			get
			{
				if (Match.CurrentGame.GameNumber == 1 &&
					Match.CurrentGame.Turns.Count == 0)
				{
					if (_settings.SpielgruppeLetter == string.Empty)
						return $"T {(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.CourtNumber}";
					else
						return $"T {(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.SpielgruppeLetter}-{_settings.CourtNumber}";
				}

				if (_settings.SpielgruppeLetter == string.Empty)
					return $"T {(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.CourtNumber}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
				else
					return $"T {(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.SpielgruppeLetter}-{_settings.CourtNumber}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";

			}
		}
		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;

		public bool TeamNamesAvailable => !string.IsNullOrEmpty(LeftTeamName);
		public string LeftTeamName
		{
			get
			{
					return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
											?.TeamNameLeft(_settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left)
											?? string.Empty;
			}
		}

		public string RightTeamName
		{
			get
			{
				return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
										?.TeamNameRight(_settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Right)
										?? string.Empty;
			}
		}

		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();

	
		
	}
}

