using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.ViewModels;

public class SettingsViewModel(SettingsService settingsService, NavigationManager navigationManager) : IDisposable
{
    private readonly Settings.Settings _currentSettings = settingsService.CurrentSettings;
    private readonly SettingsService _settingsService = settingsService;
    private readonly NavigationManager _navigationManager = navigationManager;

    private SettingsOptions _currentSetting = SettingsOptions.Theme;

    public enum SettingsOptions
    {
        Theme,
        Richtung,
        Modus,
        BahnNummer,
        MaxPunkteProKehre,
        MaxKehrenProSpiel,
        Spielgruppe,
        Networking
    }

    #region Input Handling

    public async Task ProcessKeyAsync(string value)
    {
        switch (value)
        {
            case "+":
                ExitSettingsPage();
                break;

            case "8" or "ArrowUp":
                GoToPreviousSettings();
                break;

            case "2" or "ArrowDown":
                GoToNextSettings();
                break;

            case "4" or "ArrowLeft":
                ChangeCurrentSetting(false);
                break;

            case "6" or "ArrowRight":
                ChangeCurrentSetting(true);
                break;
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
                _settingsService.ChangeMaxKehrenProSpiel(forward);
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
        }
    }

    #endregion

    #region Navigation

    private void GoToNextSettings()
    {
        if (_currentSetting < Enum.GetValues<SettingsOptions>().Max())
            _currentSetting++;
    }

    private void GoToPreviousSettings()
    {
        if (_currentSetting > Enum.GetValues<SettingsOptions>().Min())
            _currentSetting--;
    }

    private void ExitSettingsPage()
    {
        _settingsService.RequestSaveSettings();

        var modus = _currentSettings.Game.CurrentModus;

        // 👉 BESSER: zentrale Methode nutzen
        var url = SettingsService.GetModusUrl(modus);
        _navigationManager.NavigateTo(url);
    }

    #endregion

    #region Active Flags (UI)

    public bool IsThemeActive => _currentSetting == SettingsOptions.Theme;
    public bool IsRichtungActive => _currentSetting == SettingsOptions.Richtung;
    public bool IsModusActive => _currentSetting == SettingsOptions.Modus;
    public bool IsMaxPunkteProKehreActive => _currentSetting == SettingsOptions.MaxPunkteProKehre;
    public bool IsMaxKehrenProSpielActive => _currentSetting == SettingsOptions.MaxKehrenProSpiel;
    public bool IsBahnNummerActive => _currentSetting == SettingsOptions.BahnNummer;
    public bool IsNetworkingActive => _currentSetting == SettingsOptions.Networking;
    public bool IsSpielgruppeActive => _currentSetting == SettingsOptions.Spielgruppe;

    #endregion

    #region Display Values

    public string ThemeValue => _currentSettings.UI.CurrentTheme.ToString();

    public string RichtungValue => _currentSettings.UI.CurrentRichtung.ToString();

    public string ModusValue => _currentSettings.Game.CurrentModus.ToString();

    public string MaxPunkteProKehreValue => _currentSettings.Game.MaxPunkteProKehre.ToString();

    public string MaxKehrenProSpielValue => _currentSettings.Game.MaxKehrenProSpiel.ToString();

    public string SpielgruppeValue => _currentSettings.General.SpielgruppeLetter;

    public string BahnNummerValue => _currentSettings.General.BahnNummer.ToString();

    public string NetworkingValue => _currentSettings.Network.Enabled ? "An" : "Aus";

    #endregion

    public void Dispose()
    {
        // aktuell nichts nötig
    }
}