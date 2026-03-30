using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels;

public class SettingsViewModel(SettingsService settingsService, NavigationManager navigationManager): IDisposable
{
	private readonly Settings _currentSettings = settingsService.CurrentSettings;
	private readonly SettingsService _settingsService = settingsService;
	private readonly NavigationManager _navigationManager = navigationManager;
	private SettingsOptions _currentSetting = SettingsOptions.Theme;

	public enum SettingsOptions
	{
		Theme = 0,
		Richtung = 1,
		Modus = 2,
		BahnNummer = 3,
		MaxPunkteProKehre = 4,
		MaxKehrenProSpiel = 5,
		Spielgruppe = 6,
		Networking = 7,

	}

	public async Task ProcessKeyAsync(string value)
	{
		switch (value)
		{
			case "+":					ExitSettingsPage();						break;
			case "8" or "ArrowUp":		GoToPreviousSettings();					break;
			case "2" or "ArrowDown":	GoToNextSettings();						break;
			case "4" or "ArrowLeft":	ChangeCurrentSetting(forward: false);	break;
			case "6" or "ArrowRight":   ChangeCurrentSetting(forward: true);	break;
		}
	}

	private void ChangeCurrentSetting(bool forward)
	{
		switch (_currentSetting)
		{
			case SettingsOptions.Theme:
				_settingsService.ChangeTheme(forward);
				break;
			case SettingsOptions.Richtung:
				_settingsService.ChangeRichtung(forward);
				break;
			case SettingsOptions.Modus:
				_settingsService.ChangeModus(forward);
				break;
			case SettingsOptions.MaxPunkteProKehre:
				_settingsService.ChangeMaxPunkteProKehre(forward);
				break;
			case SettingsOptions.MaxKehrenProSpiel:
				_settingsService.ChangeMaxMaxKehrenProSpiel(forward);
				break;
			case SettingsOptions.BahnNummer:
				_settingsService.ChangeBahnNummer(forward);
				break;
			case SettingsOptions.Spielgruppe:
				_settingsService.ChangeSpielgruppe(forward);
				break;
			case SettingsOptions.Networking:
				_settingsService.ChangeNetworking();
				break;
			default:
				break;
		}
	}

	private void GoToNextSettings()
	{
		if (_currentSetting < Enum.GetValues<SettingsOptions>().Cast<SettingsOptions>().Max())
		{
			_currentSetting += 1;
		}
	}

	private void GoToPreviousSettings()
	{
		if (_currentSetting > Enum.GetValues<SettingsOptions>().Cast<SettingsOptions>().Min())
		{
			_currentSetting -= 1;
		}
	}

	private void ExitSettingsPage()
	{
		_ = _settingsService.SaveSettingsAsync();
		switch (_currentSettings.Modus)
			{
			case Settings.MODUS.BESTOF:
				_navigationManager.NavigateTo("/bestof");
				break;
			case Settings.MODUS.TRAINING:
				_navigationManager.NavigateTo("/training");
				return;
			case Settings.MODUS.TURNIER:
				_navigationManager.NavigateTo("/turnier");
				return;
		}
	}

	public void Dispose()
	{
		// Dispose logic if needed
	}

	public bool IsThemeActive => _currentSetting == SettingsOptions.Theme;
	public bool IsRichtungActive => _currentSetting == SettingsOptions.Richtung;
	public bool IsModusActive => _currentSetting == SettingsOptions.Modus;
	public bool IsMaxPunkteProKehreActive => _currentSetting == SettingsOptions.MaxPunkteProKehre;
	public bool IsMaxKehrenProSpielActive => _currentSetting == SettingsOptions.MaxKehrenProSpiel;
	public bool IsBahnNummerActive => _currentSetting == SettingsOptions.BahnNummer;
	public bool IsNetworkingActive => _currentSetting == SettingsOptions.Networking;
	public bool IsSpielgruppeActive => _currentSetting == SettingsOptions.Spielgruppe;



	public string ThemeValue => _currentSettings.Theme.ToString();
	public string RichtungValue => _currentSettings.Richtung.ToString();
	public string ModusValue => _currentSettings.Modus.ToString();
	public string MaxPunkteProKehreValue => _currentSettings.MaxPunkteProKehre.ToString();
	public string MaxKehrenProSpielValue => _currentSettings.MaxKehrenProSpiel.ToString();
	public string SpielgruppeValue => _currentSettings.SpielgruppeLetter;
	public string BahnNummerValue => _currentSettings.BahnNummer.ToString();
	public string NetworkingValue => _currentSettings.Networking ? "An" : "Aus";

}
