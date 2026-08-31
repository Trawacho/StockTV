using StockTvBlazor.Extensions;
using StockTvBlazor.Models;
using StockTvBlazor.Settings;
using System.Text;
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

	private readonly Channel<SettingsScope> _saveSettingsQueue = Channel.CreateUnbounded<SettingsScope>();

	private readonly SemaphoreSlim _saveGate = new(1, 1);

	private readonly ILogger _logger;

	private readonly FileLoggerProvider _fileLoggerProvider;

	public bool SettingsPageActive = false;

	/// <summary>
	/// Relative Adresse der Seite, die gerade im Bedienfenster offen ist. Grundlage dafuer,
	/// dass die zweite Anzeige (Display2) dem Hauptfenster folgt. Gemeldet wird sie vom
	/// MainWindowTracker im MainLayout.
	/// </summary>
	public string? MainWindowUrl { get; private set; }

	public event Action? OnSettingsChanged;

	public event Action<string>? OnNavigationRequested;

	/// <summary>
	/// Feuert, wenn das Bedienfenster auf eine andere Seite gewechselt ist. Bewusst ein
	/// eigenes Ereignis und nicht <see cref="OnNavigationRequested"/>: auf letzteres reagieren
	/// die Spielseiten mit einer eigenen Navigation - das blosse Melden der aktuellen Seite
	/// wuerde darueber eine Endlosschleife ausloesen.
	/// </summary>
	public event Action? OnMainWindowUrlChanged;

	/// <summary>
	/// Fordert die Anzeige auf, zu einer Adresse zu wechseln - z.B. auf das Werbebild und wieder
	/// zurueck (NetMQ <c>GoToImage</c>/<c>ClearImage</c>).
	/// </summary>
	public void RequestNavigation(string url) => OnNavigationRequested?.Invoke(url);

	/// <summary>Meldet die im Bedienfenster offene Seite. Mehrfachmeldungen sind unschaedlich.</summary>
	public void ReportMainWindowUrl(string relativeUrl)
	{
		if (string.Equals(MainWindowUrl, relativeUrl, StringComparison.Ordinal))
			return;

		MainWindowUrl = relativeUrl;
		OnMainWindowUrlChanged?.Invoke();
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

		_fileLoggerProvider.Enabled = _settings.Device.FileLoggingEnabled;

		_logger.LogInformation("FileLogging initial: {State}",
			_settings.Device.FileLoggingEnabled ? "aktiviert" : "deaktiviert");
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

		s.Device.FileLoggingEnabled = !s.Device.FileLoggingEnabled;
		_fileLoggerProvider.Enabled = s.Device.FileLoggingEnabled;

		_logger.LogInformation("FileLogging wurde {State}",
			s.Device.FileLoggingEnabled ? "aktiviert" : "deaktiviert");

		// Eigener Speicheraufruf mit Device-Scope: bis zur Aufteilung hat das ExitSettingsPage
		// miterledigt, das jetzt nur noch die Betriebs-Konfiguration schreibt.
		RequestSaveSettings(SettingsScope.Device);
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
		CurrentSettings.Device.Network.Enabled =
			!CurrentSettings.Device.Network.Enabled;

		// Wie bei ToggleFileLogging: gehoert in die Geraetedatei, die ExitSettingsPage nicht anfasst.
		RequestSaveSettings(SettingsScope.Device);
	}

	#endregion

	#region Navigation

	internal static string GetModusUrl(GameSettings.Modus modus) => modus switch
	{
		GameSettings.Modus.Training => "/training",
		GameSettings.Modus.BestOf => "/bestof",
		GameSettings.Modus.Turnier => "/turnier",
		GameSettings.Modus.Ziel or GameSettings.Modus.Ziel2=> "/ziel",
		_ => "/settings"
	};

	#endregion

	#region Load / Save

	/// <summary>Aufteilung der Konfiguration auf zwei Dateien - siehe <see cref="Settings.Settings"/>.</summary>
	private static string GetFilePath(SettingsScope scope) => Path.Combine(
		AppContext.BaseDirectory,
		"_config",
		scope == SettingsScope.Device ? "stocktv.device.json" : "stocktv.config.json");

	/// <summary>
	/// Ablageort der Betriebs-Konfiguration. Statisch, weil ihn auch die REST-Schnittstelle
	/// braucht, wo der DI-Container nicht zur Verfuegung steht.
	/// </summary>
	public static string GetSettingsFilePath() => GetFilePath(SettingsScope.Config);

	/// <summary>Ablageort der Geraete-Konfiguration (enthaelt den API-Schluessel).</summary>
	public static string GetDeviceFilePath() => GetFilePath(SettingsScope.Device);

	private static readonly JsonSerializerOptions _fileJsonOptions = new() { WriteIndented = true };

	/// <summary>Was in stocktv.config.json steht - der Rest liegt in der Geraetedatei.</summary>
	private sealed class ConfigFile
	{
		public GeneralSettings General { get; set; } = new();
		public GameSettings Game { get; set; } = new();
		public UiSettings UI { get; set; } = new();
	}

	private async Task<Settings.Settings> LoadSettingsAsync()
	{
		await MigrateLegacyFileIfNeededAsync();

		var settings = new Settings.Settings();

		settings.Device = await ReadFileAsync<DeviceSettings>(SettingsScope.Device) ?? new DeviceSettings();

		var config = await ReadFileAsync<ConfigFile>(SettingsScope.Config);
		if (config is not null)
		{
			settings.General = config.General;
			settings.Game = config.Game;
			settings.UI = config.UI;
		}

		_logger.LogInformation(
			"Konfiguration geladen: BahnNummer={BahnNummer}, Modus={Modus}, RestApi={RestApi}",
			settings.General.BahnNummer, settings.Game.CurrentModus,
			settings.Device.RestApi.Enabled ? $"Port {settings.Device.RestApi.Port}" : "aus");

		return settings;
	}

	/// <summary>
	/// Liest eine der beiden Dateien. Fehlt sie, gelten Standardwerte; ist sie unlesbar, wird sie
	/// als <c>.corrupt</c> beiseitegelegt, damit sie nicht beim naechsten Speichern unbemerkt
	/// ueberschrieben wird - mit ihr verschwaende sonst auch der API-Schluessel.
	/// </summary>
	private async Task<T?> ReadFileAsync<T>(SettingsScope scope) where T : class
	{
		var path = GetFilePath(scope);

		// Reste eines abgebrochenen Schreibvorgangs (Absturz zwischen Schreiben und Umbenennen)
		// entfernen - die eigentliche Datei ist davon unberuehrt.
		var orphanedTemp = path + ".tmp";
		if (File.Exists(orphanedTemp))
		{
			try { File.Delete(orphanedTemp); }
			catch (IOException) { /* naechster Start versucht es erneut */ }
		}

		if (!File.Exists(path))
		{
			_logger.LogWarning("{File} nicht gefunden, Standardwerte werden verwendet.", Path.GetFileName(path));
			return null;
		}

		try
		{
			var json = await File.ReadAllTextAsync(path);
			return JsonSerializer.Deserialize<T>(json);
		}
		catch (Exception ex)
		{
			var brokenPath = path + ".corrupt";
			try
			{
				File.Move(path, brokenPath, overwrite: true);
				_logger.LogError(
					"{File} unlesbar ({Msg}). Die Datei wurde als {Path} gesichert, es gelten " +
					"Standardwerte.", Path.GetFileName(path), ex.Message, brokenPath);
			}
			catch (Exception moveEx)
			{
				_logger.LogError("{File} unlesbar ({Msg}) und nicht sicherbar ({MoveMsg}). " +
					"Es gelten Standardwerte.", Path.GetFileName(path), ex.Message, moveEx.Message);
			}

			return null;
		}
	}

	/// <summary>
	/// Einmalige Umstellung von der frueheren Einzeldatei auf zwei Dateien. Erkennungsmerkmal ist
	/// die fehlende Geraetedatei; die Altdatei bleibt als <c>.migrated</c> erhalten.
	/// </summary>
	private async Task MigrateLegacyFileIfNeededAsync()
	{
		var devicePath = GetDeviceFilePath();
		var configPath = GetSettingsFilePath();

		if (File.Exists(devicePath) || !File.Exists(configPath))
			return;

		try
		{
			var json = await File.ReadAllTextAsync(configPath);

			using var document = JsonDocument.Parse(json);
			var root = document.RootElement;

			var device = new DeviceSettings();

			// Aeltere Installationen haben weder Network noch RestApi - dann gelten Standardwerte.
			if (root.TryGetProperty("Network", out var network))
				device.Network = network.Deserialize<NetworkSettings>() ?? new NetworkSettings();

			if (root.TryGetProperty("RestApi", out var restApi))
				device.RestApi = restApi.Deserialize<RestApiSettings>() ?? new RestApiSettings();

			// FileLoggingEnabled stand frueher unter General.
			if (root.TryGetProperty("General", out var general)
				&& general.TryGetProperty("FileLoggingEnabled", out var fileLogging)
				&& fileLogging.ValueKind is JsonValueKind.True or JsonValueKind.False)
			{
				device.FileLoggingEnabled = fileLogging.GetBoolean();
			}

			var config = JsonSerializer.Deserialize<ConfigFile>(json) ?? new ConfigFile();

			await WriteFileAsync(devicePath, JsonSerializer.Serialize(device, _fileJsonOptions));

			var backupPath = configPath + ".migrated";
			File.Copy(configPath, backupPath, overwrite: true);
			await WriteFileAsync(configPath, JsonSerializer.Serialize(config, _fileJsonOptions));

			_logger.LogWarning(
				"Konfiguration auf zwei Dateien aufgeteilt: Geraete-Einstellungen stehen jetzt in " +
				"{Device}. Die bisherige Fassung wurde als {Backup} gesichert.",
				Path.GetFileName(devicePath), Path.GetFileName(backupPath));
		}
		catch (Exception ex)
		{
			// Nicht abbrechen: schlaegt die Umstellung fehl, startet die Anwendung mit
			// Standardwerten fuer das Geraet - die Altdatei bleibt dabei unberuehrt und kann
			// von Hand aufgeteilt werden.
			_logger.LogError(ex, "Die Konfiguration konnte nicht auf zwei Dateien aufgeteilt werden.");
		}
	}

	/// <summary>
	/// Schreibt atomar: erst vollstaendig in eine Temp-Datei (Write-Through, also am
	/// Betriebssystem-Cache vorbei), dann umbenennen.
	/// </summary>
	/// <remarks>
	/// <see cref="File.WriteAllTextAsync(string,string,CancellationToken)"/> kuerzt die Zieldatei
	/// zuerst und schreibt danach - ein Stromausfall dazwischen (Stecker am TV-Schrank)
	/// hinterlaesst eine halbe Datei. Beim naechsten Start gaelten dann Standardwerte, und weil
	/// ein leerer API-Schluessel mit BindAddress 0.0.0.0 den Start der REST-Schnittstelle
	/// verhindert, waere das Geraet aus der Ferne nicht mehr erreichbar.
	/// </remarks>
	private static async Task WriteFileAsync(string path, string json)
	{
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var tempPath = path + ".tmp";
		var bytes = Encoding.UTF8.GetBytes(json);

		await using (var stream = new FileStream(
			tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
			bufferSize: 4096, FileOptions.WriteThrough | FileOptions.Asynchronous))
		{
			await stream.WriteAsync(bytes);
			await stream.FlushAsync();
		}

		File.Move(tempPath, path, overwrite: true);
	}

	private async Task SaveSettingsInternalAsync(SettingsScope scope)
	{
		// Beide Schreibwege - die Warteschlange unten und SaveSettingsNowAsync - greifen auf
		// dieselben Dateien zu.
		await _saveGate.WaitAsync();

		try
		{
			object payload = scope == SettingsScope.Device
				? _settings.Device
				: new ConfigFile { General = _settings.General, Game = _settings.Game, UI = _settings.UI };

			await WriteFileAsync(GetFilePath(scope), JsonSerializer.Serialize(payload, payload.GetType(), _fileJsonOptions));
		}
		finally
		{
			_saveGate.Release();
		}
	}

	/// <summary>
	/// Reiht einen Speichervorgang ein. <paramref name="scope"/> entscheidet, welche Datei
	/// geschrieben wird - ohne diese Unterscheidung wuerde jede Kehre auch die Geraetedatei mit
	/// dem API-Schluessel neu schreiben.
	/// </summary>
	public void RequestSaveSettings(SettingsScope scope = SettingsScope.Config)
	{
		_saveSettingsQueue.Writer.TryWrite(scope);
	}

	/// <summary>
	/// Speichert sofort und meldet Fehler an den Aufrufer weiter, statt sie nur zu
	/// protokollieren.
	/// </summary>
	/// <remarks>
	/// Gegenstueck zu <see cref="RequestSaveSettings"/>: das reiht nur in die Warteschlange ein
	/// und der Aufrufer erfaehrt nie, ob geschrieben wurde. Fuer den Wechsel des API-Schluessels
	/// reicht das nicht - dort muss bei einem Fehler zurueckgenommen werden koennen, sonst
	/// gaelte im Betrieb ein Schluessel, der nirgends steht, und nach dem naechsten Start kaeme
	/// niemand mehr auf das Geraet.
	///
	/// Fuer den normalen Spielbetrieb weiterhin <see cref="RequestSaveSettings"/> verwenden.
	/// </remarks>
	public Task SaveSettingsNowAsync(SettingsScope scope = SettingsScope.Config)
		=> SaveSettingsInternalAsync(scope);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var scope in _saveSettingsQueue.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				await SaveSettingsInternalAsync(scope);
				_logger.LogDebug("{Scope}-Einstellungen gespeichert", scope);
			}
			catch (Exception ex)
			{
				_logger.LogError("Fehler beim Speichern ({Scope}): {Msg}", scope, ex.Message);
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

	#region Networking (Byte Array)

	public byte[] GetSettings()
	{
		var s = CurrentSettings;
		var activeTheme = s.UI.ActiveTheme;
		byte themeValue = 0;

		// Wenn aktives Theme ein Built-in ist, sende deren Wert
		// Wenn es ein Custom Theme ist, sende dessen BaseTheme (falls definiert)
		if (activeTheme is BuiltInTheme builtIn)
		{
			themeValue = (byte)builtIn.ThemeType;
		}
		else if (activeTheme is CustomTheme custom && custom.BaseTheme.HasValue)
		{
			themeValue = (byte)custom.BaseTheme.Value;
		}

		return
		[
			(byte)s.General.BahnNummer,
			(byte)s.General.Spielgruppe,
			(byte)s.Game.CurrentModus,
			(byte)s.UI.CurrentRichtung,
			themeValue,
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

		// Theme-Logik: Suche Custom Theme mit dieser BaseTheme, sonst Built-in
		var themeValue = (UiSettings.Theme)settings[4];
		var customThemeWithBase = s.UI.CustomThemes.FirstOrDefault(t => t.BaseTheme == themeValue);
		if (customThemeWithBase != null)
		{
			s.UI.ActivateTheme(customThemeWithBase.Id);
		}
		else if (themeValue == UiSettings.Theme.Hell)
		{
			s.UI.ActivateTheme(UiSettings.HellThemeId);
		}
		else
		{
			s.UI.ActivateTheme(UiSettings.DunkelThemeId);
		}

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
