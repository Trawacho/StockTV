using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;

namespace StockTvBlazor.Components.ViewModels
{
	public class ConfigurationViewModel(Settings settings, NavigationManager navigationManager)
	{
		private readonly Settings _settings = settings;
		private readonly NavigationManager _navigationManager = navigationManager;
		private ActiveSettings _currentSetting = ActiveSettings.GameMous;

		public enum ActiveSettings
		{
			ColorScheme = 0,
			ColorSchemeRightToLeft = 1,
			GameMous = 2,
			MaxPointsPerTurn = 3,
			MaxCountOfTurnsPerGame = 4,
			CourtNumber = 5,
			Spielgruppe = 6,
			Networking = 7,

		}

		public void ProcessKey(string value)
		{
			switch (value)
			{
				case "+":					ExitSettingsPage();			break;
				case "8" or "ArrowUp":		GoToPreviousSettings();		break;
				case "2" or "ArrowDown":	GotToNextSettings();		break;
				case "4" or "ArrowLeft":	DecreaseCurrentSetting();	break;
				case "6" or "ArrowRight":	IncreaseCurrentSetting();	break;
			}
		}

		private void IncreaseCurrentSetting()
		{
			switch (_currentSetting)
			{
				case ActiveSettings.ColorScheme:
					_settings.ColorScheme.ColorSchemeUp();
					break;
				case ActiveSettings.ColorSchemeRightToLeft:
					_settings.ColorScheme.RightToLeftUp();
					break;
				case ActiveSettings.GameMous:
					_settings.GameSettings.ModusChange();
					break;
				case ActiveSettings.MaxCountOfTurnsPerGame:
					_settings.GameSettings.TurnsPerGameChange();
					break;
				case ActiveSettings.MaxPointsPerTurn:
					_settings.GameSettings.PointsPerTurnChange();
					break;
				case ActiveSettings.CourtNumber:
					_settings.CourtNumberChange();
					break;
				case ActiveSettings.Spielgruppe:
					_settings.SpielgruppeChange();
					break;
				case ActiveSettings.Networking:
					_settings.NetworkOnOffChange();
					break;
				default:
					break;
			}
			_settings.PublishSettings();
		}

		private void DecreaseCurrentSetting()
		{
			switch (_currentSetting)
			{
				case ActiveSettings.ColorScheme:
					_settings.ColorScheme.ColorSchemeDown();
					break;
				case ActiveSettings.ColorSchemeRightToLeft:
					_settings.ColorScheme.RightToLeftDown();
					break;
				case ActiveSettings.GameMous:
					_settings.GameSettings.ModusChange(false);
					break;
				case ActiveSettings.MaxCountOfTurnsPerGame:
					_settings.GameSettings.TurnsPerGameChange(false);
					break;
				case ActiveSettings.MaxPointsPerTurn:
					_settings.GameSettings.PointsPerTurnChange(false);
					break;
				case ActiveSettings.CourtNumber:
					_settings.CourtNumberChange(false);
					break;
				case ActiveSettings.Spielgruppe:
					_settings.SpielgruppeChange(false);
					break;
				case ActiveSettings.Networking:
					_settings.NetworkOnOffChange();
					break;
				default:
					break;
			}
			_settings.PublishSettings();
		}

		private void GotToNextSettings()
		{
			if (_currentSetting < Enum.GetValues<ActiveSettings>().Cast<ActiveSettings>().Max())
			{
				_currentSetting += 1;
			}
		}

		private void GoToPreviousSettings()
		{
			if (_currentSetting > Enum.GetValues<ActiveSettings>().Cast<ActiveSettings>().Min())
			{
				_currentSetting -= 1;
			}
		}

		private void ExitSettingsPage()
		{
			switch(_settings.GameSettings.GameModus)
				{
				case GameSettings.GameModis.BestOf:
					_navigationManager.NavigateTo("/bestof");
					break;
				case GameSettings.GameModis.Training:
					_navigationManager.NavigateTo("/training");
					return;
				case GameSettings.GameModis.Turnier:
					_navigationManager.NavigateTo("/turnier");
					return;
			}
		}



		public bool IsColorSchemeActive => _currentSetting == ActiveSettings.ColorScheme;
		public bool IsColorSchemeRightToLeftActive => _currentSetting == ActiveSettings.ColorSchemeRightToLeft;
		public bool IsGameMousActive => _currentSetting == ActiveSettings.GameMous;
		public bool IsMaxPointsPerTurnActive => _currentSetting == ActiveSettings.MaxPointsPerTurn;
		public bool IsMaxCountOfTurnsPerGameActive => _currentSetting == ActiveSettings.MaxCountOfTurnsPerGame;
		public bool IsCourtNumberActive => _currentSetting == ActiveSettings.CourtNumber;
		public bool IsNetworkingActive => _currentSetting == ActiveSettings.Networking;
		public bool IsSpielgruppeActive => _currentSetting == ActiveSettings.Spielgruppe;



		public string ColorSchemeValue => _settings.ColorScheme.ColorModus.ToString();
		public string RightToLeftValue => _settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left ? "Links" : "Rechts";
		public string GameModusValue => _settings.GameSettings.GameModus.ToString();
		public string MaxPointsPerTurnValue => _settings.GameSettings.PointsPerTurn.ToString();
		public string MaxCountOfTurnsPerGameValue => _settings.GameSettings.TurnsPerGame.ToString();
		public string SpielgruppeValue => _settings.SpielgruppeLetter;
		public string CourtNumberValue => _settings.CourtNumber.ToString();
		public string NetworkingValue => _settings.Networking ? "An" : "Aus";

	}
}
