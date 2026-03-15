using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(Settings configuration, NavigationManager navigationManager) : BaseViewModel(configuration, navigationManager)
{
	public string HeaderText
	{
		get
		{
			if (Match.CurrentGame.GameNumber == 1 &&
				Match.CurrentGame.Turns.Count == 0)
			{
				if (_configuration.SpielgruppeLetter == string.Empty)
					return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.BahnNummer}";
				else
					return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.SpielgruppeLetter}-{_configuration.BahnNummer}";
			}

			if (_configuration.SpielgruppeLetter == string.Empty)
				return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
			else
				return $"{(_configuration.BlockLocalChanges ? "." : "")}Bahn: {_configuration.SpielgruppeLetter}-{_configuration.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";

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
										?.TeamNameLeft(_configuration.Richtung == Settings.RICHTUNG.LINKS)
										?? string.Empty;
		}
	}

	public string RightTeamName
	{
		get
		{
			return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
									?.TeamNameLeft(_configuration.Richtung == Settings.RICHTUNG.RECHTS)
									?? string.Empty;
		}
	}

	public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();


	
}

