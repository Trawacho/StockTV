using StockTvBlazor.Extensions;
using StockTvBlazor.Models;
using StockTvBlazor.Settings;
using System.Text.Json;
using System.Threading.Channels;

namespace StockTvBlazor.Services;

public class SettingsService : BackgroundService
{
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

	internal SettingsOptions CurrentSettingToChange = SettingsOptions.Theme;

	private Settings.Settings _settings = null!;

	private readonly string _settingsFileName = "stocktv.config.json";

	private readonly Channel<bool> _saveSettingsQueue = Channel.CreateUnbounded<bool>();

	private readonly ILogger _logger;

	private readonly FileLoggerProvider _fileLoggerProvider;

	public bool SettingsPageActive = false;

	public event Action? OnSettingsChanged;

	public event Action<string>? OnNavigationRequested;

	private string _settingsFilePath
	{
		get
		{
			string appDataPath = AppContext.BaseDirectory;
			string settingsFolderPath = Path.Combine(appDataPath, "_config");
			return Path.Combine(settingsFolderPath, _settingsFileName);
		}
	}

	public SettingsService(
		ILogger<SettingsService> logger,
		FileLoggerProvider fileLoggerProvider)
	{
		_logger = logger;
		_fileLoggerProvider = fileLoggerProvider;
	}

	public override void Dispose()
	{
		_saveSettingsQueue.Writer.TryComplete();
		base.Dispose();
	}

	public async Task InitializeAsync()
	{
		_settings = await LoadSettingsAsync();

		_fileLoggerProvider.Enabled = _settings.General.FileLoggingEnabled;

		_logger.LogInformation("FileLogging initial: {State}",
			_settings.General.FileLoggingEnabled ? "aktiviert" : "deaktiviert");
	}

	public Settings.Settings CurrentSettings
	{
		get
		{
			if (_settings == null)
				throw new InvalidOperationException("SettingsService wurde nicht initialisiert.");

			return _settings;
		}
	}

	private void NotifyChanged()
	{
		OnSettingsChanged?.Invoke();
	}

	#region Change Methods

	public void ToggleFileLogging()
	{
		var s = CurrentSettings;

		s.General.FileLoggingEnabled = !s.General.FileLoggingEnabled;
		_fileLoggerProvider.Enabled = s.General.FileLoggingEnabled;

		_logger.LogInformation("FileLogging wurde {State}",
			s.General.FileLoggingEnabled ? "aktiviert" : "deaktiviert");

		NotifyChanged();
	}

	public void ChangeModus(bool forward)
	{
		var s = CurrentSettings;

		var newModus = forward
			? s.Game.CurrentModus.Next()
			: s.Game.CurrentModus.Previous();

		if (newModus == GameSettings.Modus.Training)
		{
			s.Game.MaxKehrenProSpiel = 30;
			s.Game.MaxPunkteProKehre = 15;
		}
		else
		{
			s.Game.MaxKehrenProSpiel = 6;
			s.Game.MaxPunkteProKehre = 10;
		}

		s.Game.CurrentModus = newModus;
	}

	public void ChangeTheme(bool forward)
	{
		var s = CurrentSettings;

		var themes = s.UI.AllThemes;
		var currentIndex = themes.ToList().FindIndex(t => t.Id == s.UI.ActiveThemeId);

		// Falls nichts aktiv, bei 0 starten
		if (currentIndex < 0) currentIndex = 0;

		var nextIndex = forward
			? (currentIndex + 1) % themes.Count
			: (currentIndex - 1 + themes.Count) % themes.Count;

		s.UI.ActivateTheme(themes[nextIndex].Id);

	}

	public void ChangeRichtung(bool forward)
	{
		var s = CurrentSettings;

		s.UI.CurrentRichtung = forward
			? s.UI.CurrentRichtung.Next()
			: s.UI.CurrentRichtung.Previous();
	}

	public void ChangeBlockLocalChanges()
	{
		CurrentSettings.General.BlockLocalChanges =
			!CurrentSettings.General.BlockLocalChanges;
		NotifyChanged();
	}
	public void ChangeBlockLocalChanges(bool block)
	{
		CurrentSettings.General.BlockLocalChanges = block;
		NotifyChanged();
	}

	public void ChangeSpielgruppe(bool forward)
	{
		var s = CurrentSettings.General;

		if (forward && s.Spielgruppe < 10)
			s.Spielgruppe++;
		else if (!forward && s.Spielgruppe > 0)
			s.Spielgruppe--;
	}

	public void ChangeBahnNummer(bool forward)
	{
		var s = CurrentSettings.General;

		if (forward && s.BahnNummer < 30)
			s.BahnNummer++;
		else if (!forward && s.BahnNummer > 1)
			s.BahnNummer--;
	}

	public void ChangeMaxKehrenProSpiel(bool forward)
	{
		var settings = CurrentSettings.Game;

		const int DefaultKehrenMin = 4;
		const int DefaultKehrenMax = 30;
		const int ZielVersucheMin = 6;
		const int ZielVersucheMax = 12;

		if (settings.CurrentModus == GameSettings.Modus.Ziel)
		{
			settings.MaxKehrenProSpiel = forward ? ZielVersucheMax : ZielVersucheMin;
			return;
		}

		settings.MaxKehrenProSpiel = Math.Clamp(
			settings.MaxKehrenProSpiel + (forward ? 1 : -1),
			DefaultKehrenMin,
			DefaultKehrenMax
		);
	}

	public void ChangeMaxPunkteProKehre(bool forward)
	{
		var s = CurrentSettings.Game;

		if (forward && s.MaxPunkteProKehre < 15)
			s.MaxPunkteProKehre++;
		else if (!forward && s.MaxPunkteProKehre > 0)
			s.MaxPunkteProKehre--;
	}

	public void ChangeNetworking()
	{
		CurrentSettings.Network.Enabled =
			!CurrentSettings.Network.Enabled;
	}

	#endregion

	#region Navigation

	internal static string GetModusUrl(GameSettings.Modus modus) => modus switch
	{
		GameSettings.Modus.Training => "/training",
		GameSettings.Modus.BestOf => "/bestof",
		GameSettings.Modus.Turnier => "/turnier",
		GameSettings.Modus.Ziel => "/ziel",
		_ => "/settings"
	};

	#endregion

	#region Load / Save

	private async Task<Settings.Settings> LoadSettingsAsync()
	{
		if (!File.Exists(_settingsFilePath))
		{
			_logger.LogWarning("Keine Config-Datei gefunden, Standardwerte werden verwendet.");
			return new Settings.Settings();
		}

		try
		{
			var json = await File.ReadAllTextAsync(_settingsFilePath);
			return JsonSerializer.Deserialize<Settings.Settings>(json) ?? new Settings.Settings();
		}
		catch (Exception ex)
		{
			_logger.LogError("Fehler beim Laden der Config: {Msg}", ex.Message);
			return new Settings.Settings();
		}
	}

	private async Task SaveSettingsInternalAsync()
	{
		var directory = Path.GetDirectoryName(_settingsFilePath);

		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var options = new JsonSerializerOptions { WriteIndented = true };
		var json = JsonSerializer.Serialize(_settings, options);

		await File.WriteAllTextAsync(_settingsFilePath, json);
	}

	public void RequestSaveSettings()
	{
		_saveSettingsQueue.Writer.TryWrite(true);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var _ in _saveSettingsQueue.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				await SaveSettingsInternalAsync();
				_logger.LogDebug("Settings gespeichert");
			}
			catch (Exception ex)
			{
				_logger.LogError("Fehler beim Speichern: {Msg}", ex.Message);
			}
		}
	}

	#endregion

	#region Custom Themes

	public void AddOrUpdateCustomTheme(CustomTheme theme)
	{
		var ui = CurrentSettings.UI;
		var idx = ui.CustomThemes.FindIndex(t => t.Id == theme.Id);
		if (idx >= 0)
			ui.CustomThemes[idx] = theme;
		else
			ui.CustomThemes.Add(theme);

		RequestSaveSettings();
		NotifyChanged();
	}

	public void DeleteCustomTheme(Guid id)
	{
		CurrentSettings.UI.RemoveCustomTheme(id);
		RequestSaveSettings();
		NotifyChanged();
	}

	public void ActivateTheme(Guid id)
	{
		CurrentSettings.UI.ActivateTheme(id);
		RequestSaveSettings();
		NotifyChanged();
	}

	#endregion

	#region Turns

	public async Task SaveTurnsAsync(List<Turn> turns)
	{
		var s = CurrentSettings;

		if (s.Game.CurrentModus == GameSettings.Modus.Training)
			return;

		s.Game.Kehren.Clear();
		s.Game.Kehren.AddRange(turns);

		RequestSaveSettings();
	}

	#endregion

	#region Networking (Byte Array)

	public byte[] GetSettings()
	{
		var s = CurrentSettings;

		return
		[
			(byte)s.General.BahnNummer,
			(byte)s.General.Spielgruppe,
			(byte)s.Game.CurrentModus,
			(byte)s.UI.CurrentRichtung,
			0,//(byte)s.UI.CurrentTheme,
            (byte)s.Game.MaxPunkteProKehre,
			(byte)s.Game.MaxKehrenProSpiel,
			(byte)s.UI.MidColumnWidth,
			(byte)s.General.MessageVersion,
			0
		];
	}

	public void SetSettings(byte[] settings)
	{
		if (settings == null || settings.Length < 10)
		{
			_logger.LogWarning("Ungültiges Settings-Array");
			return;
		}

		var s = CurrentSettings;

		if (!Enum.IsDefined(typeof(GameSettings.Modus), (int)settings[2]) ||
			!Enum.IsDefined(typeof(UiSettings.Richtung), (int)settings[3]) ||
			!Enum.IsDefined(typeof(UiSettings.Theme), (int)settings[4]))
		{
			_logger.LogWarning("Ungültige Enum-Werte im Netzwerkpaket");
			return;
		}

		s.General.BahnNummer = settings[0];
		s.General.Spielgruppe = settings[1];

		var newModus = (GameSettings.Modus)settings[2];
		if (s.Game.CurrentModus != newModus)
		{
			s.Game.CurrentModus = newModus;
			OnNavigationRequested?.Invoke(GetModusUrl(newModus));
		}

		s.UI.CurrentRichtung = (UiSettings.Richtung)settings[3];
		//s.UI.CurrentTheme = (UiSettings.Theme)settings[4];
		s.Game.MaxPunkteProKehre = settings[5];
		s.Game.MaxKehrenProSpiel = settings[6];
		s.UI.MidColumnWidth = settings[7];

		NotifyChanged();
		RequestSaveSettings();
	}

	#endregion

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

		NotifyChanged();
	}

	private void ChangeCurrentSetting(bool forward)
	{
		switch (CurrentSettingToChange)
		{
			case SettingsOptions.Theme:
				ChangeTheme(forward);
				break;

			case SettingsOptions.Richtung:
				ChangeRichtung(forward);
				break;

			case SettingsOptions.Modus:
				ChangeModus(forward);
				break;

			case SettingsOptions.MaxPunkteProKehre:
				ChangeMaxPunkteProKehre(forward);
				break;

			case SettingsOptions.MaxKehrenProSpiel:
				ChangeMaxKehrenProSpiel(forward);
				break;

			case SettingsOptions.BahnNummer:
				ChangeBahnNummer(forward);
				break;

			case SettingsOptions.Spielgruppe:
				ChangeSpielgruppe(forward);
				break;

			case SettingsOptions.Networking:
				ChangeNetworking();
				break;
		}
		NotifyChanged();
	}

	#endregion

	#region Navigation

	private void GoToNextSettings()
	{
		if (CurrentSettingToChange < Enum.GetValues<SettingsOptions>().Max())
			CurrentSettingToChange++;
	}

	private void GoToPreviousSettings()
	{
		if (CurrentSettingToChange > Enum.GetValues<SettingsOptions>().Min())
			CurrentSettingToChange--;
	}

	private void ExitSettingsPage()
	{
		RequestSaveSettings();

		var modus = CurrentSettings.Game.CurrentModus;
		SettingsPageActive = false;

		var url = GetModusUrl(modus);
		OnNavigationRequested?.Invoke(url);
	}

	#endregion
}
