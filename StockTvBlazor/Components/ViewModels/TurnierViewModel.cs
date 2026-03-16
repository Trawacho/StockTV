using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Components.ViewModels;

public class TurnierViewModel(SettingsService settingsService, NavigationManager navigationManager) : BaseViewModel(settingsService, navigationManager)
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

			if (_currentSettings.SpielgruppeLetter == string.Empty)
				return $"{(_currentSettings.BlockLocalChanges ? "." : "")}Bahn: {_currentSettings.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
			else
				return $"{(_currentSettings.BlockLocalChanges ? "." : "")}Bahn: {_currentSettings.SpielgruppeLetter}-{_currentSettings.BahnNummer}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";

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
										?.TeamNameLeft(_currentSettings.Richtung == Settings.RICHTUNG.LINKS)
										?? string.Empty;
		}
	}

	public string RightTeamName
	{
		get
		{
			return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
									?.TeamNameLeft(_currentSettings.Richtung == Settings.RICHTUNG.RECHTS)
									?? string.Empty;
		}
	}

	public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();


	
}

