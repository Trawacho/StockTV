using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace StockTvBlazor.Services;

public record UpdateCheckResult(string CurrentVersion, string? LatestVersion, bool UpdateAvailable, string? ErrorMessage);

/// <summary>
/// Prüft auf GitHub, ob ein neueres StockTV-Release existiert, und stößt bei Bedarf ein Update an.
/// Der eigentliche Update-Vorgang läuft über die feste, in build/rpi/install.sh per sudoers
/// freigegebene Unit "stocktv-update" (systemd-run), da das darin gestartete install.sh den
/// eigenen stocktv-Dienst neu startet - siehe StartUpdateAsync.
/// </summary>
public class UpdateService
{
	private const string GitHubReleaseUrl = "https://api.github.com/repos/Trawacho/StockTV/releases/latest";
	private const string HttpClientName = "GitHub";
	private const string UpdateScriptPath = "/usr/local/sbin/stocktv-run-update.sh";
	private const string OfflineUpdateScriptPath = "/usr/local/sbin/stocktv-run-offline-update.sh";
	private const string OfflineUpdateFileName = "upload.zip";

	// Marge oberhalb der reinen Datei-/Entpackgroesse, da z.B. Logs zwischenzeitlich weiterwachsen.
	private const long OfflineUpdateSafetyMarginBytes = 100L * 1024 * 1024;

	// Client (InputFile.OpenReadStream) UND Server (SignalR-Hub in Program.cs) muessen denselben
	// Wert kennen, siehe dort.
	public const long MaxOfflineUpdateUploadBytes = 150L * 1024 * 1024;

	// Vor Ort im selben WLAN sollten 150 MB deutlich schneller uebertragen sein - grosszuegige
	// Grenze, die primaer einen haengenden Upload (z.B. abgerissene Verbindung) faengt, damit die
	// Setup-Seite nicht unbegrenzt im "wird hochgeladen"-Zustand haengen bleibt.
	public static readonly TimeSpan OfflineUpdateUploadTimeout = TimeSpan.FromMinutes(5);

	private static string UpdateDirectory => Path.Combine(AppContext.BaseDirectory, "_update");
	private static string OfflineUpdateZipPath => Path.Combine(UpdateDirectory, OfflineUpdateFileName);
	private const string OfflineUpdateTempSearchPattern = OfflineUpdateFileName + ".*.tmp";

	// Eigener, zufaelliger Name pro Aufruf (statt eines festen Temp-Pfads) - verhindert, dass zwei
	// gleichzeitige Uploads (zwei Browser-Tabs/Personen) sich gegenseitig die Temp-Datei ueberschreiben.
	private static string NewOfflineUpdateTempPath() => Path.Combine(UpdateDirectory, $"{OfflineUpdateFileName}.{Guid.NewGuid():N}.tmp");

	private readonly ILogger<UpdateService> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	// 0/1 statt bool, damit der Belegungsversuch unten per Interlocked.CompareExchange atomar ist -
	// zwei Browser-Tabs/Personen koennten SaveOfflineUpdatePackageAsync sonst gleichzeitig durchlaufen
	// (ein einfaches "if (Feld) return; Feld = true;" waere hier eine echte Race Condition, da beide
	// Aufrufe den Check passieren koennten, bevor einer das Flag setzt). Aus demselben Grund auch fuer
	// UpdateInProgress selbst (siehe RunUpdateUnitAsync) - zwei gleichzeitige Update-Starts (Online
	// und/oder Offline, aus zwei Tabs) sollen sich nicht beide fuer "nicht belegt" halten koennen.
	private int _offlineUploadInProgressFlag;
	private int _updateInProgressFlag;

	public bool UpdateInProgress => Volatile.Read(ref _updateInProgressFlag) != 0;

	public UpdateService(ILogger<UpdateService> logger, IHttpClientFactory httpClientFactory)
	{
		_logger = logger;
		_httpClientFactory = httpClientFactory;
	}

	public string GetCurrentVersion()
	{
		var version = Assembly.GetExecutingAssembly().GetName().Version;
		return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
	}

	public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
	{
		var currentVersion = GetCurrentVersion();
		var client = _httpClientFactory.CreateClient(HttpClientName);

		try
		{
			using var response = await client.GetAsync(GitHubReleaseUrl, ct);

			if (response.StatusCode == HttpStatusCode.Forbidden)
			{
				_logger.LogWarning("GitHub API antwortete mit 403 (vermutlich Rate-Limit ueberschritten)");
				return new UpdateCheckResult(currentVersion, null, false,
					"GitHub-Anfragelimit erreicht, bitte später erneut versuchen.");
			}

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("GitHub-Release-Abfrage fehlgeschlagen: {Status}", response.StatusCode);
				return new UpdateCheckResult(currentVersion, null, false, $"GitHub antwortete mit {(int)response.StatusCode}.");
			}

			await using var stream = await response.Content.ReadAsStreamAsync(ct);
			using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

			var tagName = doc.RootElement.GetProperty("tag_name").GetString();
			if (string.IsNullOrWhiteSpace(tagName))
				return new UpdateCheckResult(currentVersion, null, false, "GitHub-Antwort enthielt keinen Tag-Namen.");

			var latestVersionText = tagName.TrimStart('v', 'V');
			var updateAvailable = IsNewerVersion(latestVersionText, currentVersion);

			return new UpdateCheckResult(currentVersion, latestVersionText, updateAvailable, null);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Update-Check fehlgeschlagen");
			return new UpdateCheckResult(currentVersion, null, false, "Update-Check fehlgeschlagen: " + ex.Message);
		}
	}

	private static bool IsNewerVersion(string latestText, string currentText)
	{
		if (!Version.TryParse(NormalizeToThreeParts(latestText), out var latest))
			return false;
		if (!Version.TryParse(NormalizeToThreeParts(currentText), out var current))
			return false;

		return latest > current;
	}

	// Nur Major.Minor.Build vergleichen - die csproj-Version haengt immer eine ".0"-Revision an,
	// die GitHub-Tags (z.B. "v1.7.1") nicht kennen.
	private static string NormalizeToThreeParts(string text)
	{
		var parts = text.Split('.');
		return parts.Length >= 3 ? string.Join('.', parts[0], parts[1], parts[2]) : text;
	}

	public async Task<NetworkOperationResult> StartUpdateAsync(CancellationToken ct = default)
	{
		if (UpdateInProgress)
			return new NetworkOperationResult(false, "Es läuft bereits ein Update.");

		return await RunUpdateUnitAsync(UpdateScriptPath, ct);
	}

	/// <summary>
	/// Speichert ein manuell hochgeladenes Release-Zip fuer ein Offline-Update. Prueft dabei
	/// (a) vorab groben Speicherplatz fuer den reinen Upload, (b) dass es sich um eine gueltige,
	/// zu StockTV passende ZIP-Datei handelt, und (c) danach den tatsaechlich fuer das Entpacken
	/// noetigen Speicherplatz anhand der unkomprimierten Groesse der Eintraege. Die Datei landet
	/// erst nach erfolgreicher Pruefung unter OfflineUpdateZipPath - bei jedem Fehlschlag (und in
	/// jedem Nicht-Erfolgsfall ueber das finally) wird die temporaere Datei sofort wieder entfernt.
	/// </summary>
	public async Task<NetworkOperationResult> SaveOfflineUpdatePackageAsync(Stream uploadStream, long uploadSizeBytes, CancellationToken ct = default)
	{
		if (UpdateInProgress)
			return new NetworkOperationResult(false, "Es läuft bereits ein Update.");

		// Atomarer Belegungsversuch: verhindert, dass zwei gleichzeitige Uploads (zwei Tabs/Personen)
		// sich unbemerkt gegenseitig ueberholen - ohne diese Sperre koennten beide Uploads erfolgreich
		// validiert werden, und welcher am Ende unter OfflineUpdateZipPath landet, haette schlicht vom
		// Zufall (wer zuletzt fertig wird) abgehangen, ohne dass irgendjemand eine Meldung bekommt.
		if (Interlocked.CompareExchange(ref _offlineUploadInProgressFlag, 1, 0) != 0)
			return new NetworkOperationResult(false, "Es läuft bereits ein anderer Upload. Bitte warten, bis dieser abgeschlossen ist.");

		try
		{
			return await SaveOfflineUpdatePackageCoreAsync(uploadStream, uploadSizeBytes, ct);
		}
		finally
		{
			Volatile.Write(ref _offlineUploadInProgressFlag, 0);
		}
	}

	private async Task<NetworkOperationResult> SaveOfflineUpdatePackageCoreAsync(Stream uploadStream, long uploadSizeBytes, CancellationToken ct)
	{
		Directory.CreateDirectory(UpdateDirectory);

		if (!HasEnoughFreeSpace(uploadSizeBytes + OfflineUpdateSafetyMarginBytes))
			return new NetworkOperationResult(false, "Nicht genügend freier Speicherplatz für den Upload.");

		var tempPath = NewOfflineUpdateTempPath();
		try
		{
			await using (var fileStream = File.Create(tempPath))
			{
				await uploadStream.CopyToAsync(fileStream, ct);
			}

			long uncompressedSize;
			try
			{
				using var archive = ZipFile.OpenRead(tempPath);
				if (archive.Entries.Count == 0 || archive.GetEntry("StockTvBlazor") is null)
					return new NetworkOperationResult(false, "Die Datei ist kein gültiges StockTV-Update-Paket.");

				// Zip-Slip-Schutz: das Skript auf dem Pi entpackt diese Datei spaeter als root
				// (siehe stocktv-run-offline-update.sh) - ein Eintrag mit ".." oder absolutem Pfad
				// koennte sonst Dateien ausserhalb von $INSTALL_DIR ueberschreiben.
				if (archive.Entries.Any(e => HasUnsafeEntryPath(e.FullName)))
					return new NetworkOperationResult(false, "Die ZIP-Datei enthält unzulässige Pfade und wurde abgelehnt.");

				uncompressedSize = archive.Entries.Sum(e => e.Length);
			}
			catch (InvalidDataException)
			{
				return new NetworkOperationResult(false, "Die hochgeladene Datei ist keine gültige ZIP-Datei.");
			}

			var zipSize = new FileInfo(tempPath).Length;
			if (!HasEnoughFreeSpace(zipSize + uncompressedSize + OfflineUpdateSafetyMarginBytes))
				return new NetworkOperationResult(false, "Nicht genügend freier Speicherplatz zum Entpacken des Updates.");

			File.Move(tempPath, OfflineUpdateZipPath, overwrite: true);
			return new NetworkOperationResult(true, null);
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Offline-Update-Upload abgebrochen (Timeout nach {Timeout} oder Verbindungsabbruch)", OfflineUpdateUploadTimeout);
			return new NetworkOperationResult(false,
				$"Der Upload wurde abgebrochen (Zeitüberschreitung nach {OfflineUpdateUploadTimeout.TotalMinutes:0} Minuten oder Verbindungsabbruch). Bitte erneut versuchen.");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Offline-Update-Paket konnte nicht gespeichert werden");
			return new NetworkOperationResult(false, "Fehler beim Speichern der Datei: " + ex.Message);
		}
		finally
		{
			// Nach einem erfolgreichen File.Move existiert die temporaere Datei nicht mehr (no-op),
			// bei jedem anderen Ausgang (Fehler/Exception) wird hier zuverlaessig aufgeraeumt -
			// es soll nie eine ungueltige/halb geschriebene Datei liegen bleiben.
			if (File.Exists(tempPath))
				File.Delete(tempPath);
		}
	}

	/// <summary>
	/// Entfernt ein zuvor hochgeladenes, aber nie angewendetes Offline-Update-Paket. Wird beim
	/// Betreten der Setup-Seite aufgerufen, damit nach einem Reload immer ein frischer Upload
	/// noetig ist statt stillschweigend ein evtl. alte Datei aus einer vergangenen Sitzung
	/// anzuwenden.
	/// </summary>
	public void ClearStaleOfflineUpdatePackage()
	{
		try
		{
			if (File.Exists(OfflineUpdateZipPath))
				File.Delete(OfflineUpdateZipPath);

			if (Directory.Exists(UpdateDirectory))
			{
				foreach (var tempFile in Directory.EnumerateFiles(UpdateDirectory, OfflineUpdateTempSearchPattern))
					File.Delete(tempFile);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Alte Offline-Update-Datei konnte nicht entfernt werden");
		}
	}

	// Verhindert "Zip Slip": ein Eintrag mit absolutem Pfad oder ".."-Segment koennte beim Entpacken
	// (auf dem Pi als root, siehe StartOfflineUpdateAsync) Dateien ausserhalb des Zielverzeichnisses
	// ueberschreiben. Prueft String-basiert statt ueber Path.GetFullPath, da Zip-Eintraege immer
	// "/" als Trenner nutzen und unabhaengig vom Laufzeit-Betriebssystem gleich behandelt werden sollen.
	private static bool HasUnsafeEntryPath(string entryFullName)
	{
		if (string.IsNullOrEmpty(entryFullName))
			return true;

		if (entryFullName.StartsWith('/') || entryFullName.StartsWith('\\'))
			return true;

		// Windows-Laufwerksangabe wie "C:\..." - auf dem Zielsystem (Linux) irrelevant, schadet aber
		// nicht, das defensiv mitzupruefen.
		if (entryFullName.Length >= 2 && entryFullName[1] == ':')
			return true;

		return entryFullName.Split('/', '\\').Any(segment => segment == "..");
	}

	public async Task<NetworkOperationResult> StartOfflineUpdateAsync(CancellationToken ct = default)
	{
		if (UpdateInProgress)
			return new NetworkOperationResult(false, "Es läuft bereits ein Update.");

		if (!File.Exists(OfflineUpdateZipPath))
			return new NetworkOperationResult(false, "Es wurde keine gültige Update-Datei hochgeladen.");

		return await RunUpdateUnitAsync(OfflineUpdateScriptPath, ct);
	}

	private static bool HasEnoughFreeSpace(long requiredBytes)
	{
		var drive = new DriveInfo(UpdateDirectory);
		return drive.AvailableFreeSpace >= requiredBytes;
	}

	// Startet das uebergebene, per sudoers fest freigegebene Skript als transiente systemd-Unit.
	// Online- und Offline-Update teilen sich bewusst denselben Unit-Namen "stocktv-update": der
	// atomare Belegungsversuch unten verhindert ohnehin, dass beide gleichzeitig starten - ein
	// zweiter Unit-Name haette hier keinen Mehrwert, aber eine zusaetzliche sudoers-Zeile zur Folge.
	private async Task<NetworkOperationResult> RunUpdateUnitAsync(string scriptPath, CancellationToken ct)
	{
		// Atomarer Belegungsversuch statt des vorherigen "if (UpdateInProgress) return; UpdateInProgress
		// = true;" in den Aufrufern - das waere eine echte Race Condition zwischen zwei gleichzeitigen
		// Update-Ausloesern (z.B. Online-Update aus einem Tab, Offline-Update aus einem anderen).
		if (Interlocked.CompareExchange(ref _updateInProgressFlag, 1, 0) != 0)
			return new NetworkOperationResult(false, "Es läuft bereits ein Update.");

		var psi = new ProcessStartInfo
		{
			FileName = "sudo",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		psi.ArgumentList.Add("-n");
		psi.ArgumentList.Add("/usr/bin/systemd-run");
		psi.ArgumentList.Add("--unit=stocktv-update");
		psi.ArgumentList.Add("--collect");
		psi.ArgumentList.Add(scriptPath);

		try
		{
			using var process = Process.Start(psi);
			if (process == null)
			{
				Volatile.Write(ref _updateInProgressFlag, 0);
				return new NetworkOperationResult(false, "Prozess konnte nicht gestartet werden.");
			}

			await process.WaitForExitAsync(ct);
			var stdErr = await process.StandardError.ReadToEndAsync(ct);

			if (process.ExitCode != 0)
			{
				_logger.LogWarning("systemd-run fuer Update fehlgeschlagen: {StdErr}", stdErr);
				Volatile.Write(ref _updateInProgressFlag, 0);
				return new NetworkOperationResult(false, stdErr);
			}

			// UpdateInProgress bleibt bewusst true: die transiente "stocktv-update"-Unit laeuft jetzt
			// unabhaengig weiter und wird in Kuerze den stocktv-Dienst (diesen Prozess) neu starten.
			_logger.LogInformation("Update-Unit stocktv-update gestartet ({ScriptPath})", scriptPath);
			return new NetworkOperationResult(true, null);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Update konnte nicht gestartet werden");
			Volatile.Write(ref _updateInProgressFlag, 0);
			return new NetworkOperationResult(false, ex.Message);
		}
	}
}
