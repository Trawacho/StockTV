using StockTvBlazor.Services;

namespace StockTvBlazor.Components.ViewModels;

public class SettingsViewModel(SettingsService settingsService) : IDisposable
{
    private readonly Settings.Settings _currentSettings = settingsService.CurrentSettings;
    private readonly SettingsService _settingsService = settingsService;
   

    #region Active Flags (UI)

    public bool IsThemeActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.Theme;
    public bool IsRichtungActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.Richtung;
    public bool IsModusActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.Modus;
    public bool IsMaxPunkteProKehreActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.MaxPunkteProKehre;
    public bool IsMaxKehrenProSpielActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.MaxKehrenProSpiel;
    public bool IsBahnNummerActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.BahnNummer;
    public bool IsNetworkingActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.Networking;
    public bool IsSpielgruppeActive => _settingsService.CurrentSettingToChange == SettingsService.SettingsOptions.Spielgruppe;

    #endregion

    #region Display Values

    //public string ThemeValue => _currentSettings.UI.CurrentTheme.ToString();
    public string ThemeValue => _currentSettings.UI.ActiveTheme.Name;   

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