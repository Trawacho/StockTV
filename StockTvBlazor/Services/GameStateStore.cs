using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Services;

/// <summary>
/// Der laufende Spielstand auf der Platte (<c>_config/stocktv.state.json</c>).
/// </summary>
/// <remarks>
/// Bewusst eine eigene Datei und ein eigener Dienst, nicht Teil von
/// <see cref="Settings.Settings"/>: ein Spielstand ist keine Einstellung. Er wird als einziger
/// Bestand im Spielbetrieb dauernd geschrieben - waehrend die Geraetedatei mit dem
/// API-Schluessel unberuehrt bleibt. Genau diese Trennung ist der Zweck der Aufteilung.
/// </remarks>
public sealed class GameStateStore(ILogger<GameStateStore> logger) : BackgroundService
{
	/// <summary>
	/// Wie alt ein gespeicherter Spielstand hoechstens sein darf, um noch geladen zu werden.
	/// </summary>
	/// <remarks>
	/// Startet die Anwendung mitten im Turnier neu, soll der Spielstand zurueckkommen. Wird das
	/// Geraet dagegen naechste Woche wieder eingeschaltet, darf der alte Stand nicht ins neue
	/// Turnier geraten. Zwoelf Stunden decken einen Turniertag ab und enden vor dem naechsten.
	/// </remarks>
	public static readonly TimeSpan MaxAge = TimeSpan.FromHours(12);

	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	private readonly SemaphoreSlim _saveGate = new(1, 1);

	private readonly Channel<bool> _saveQueue = Channel.CreateUnbounded<bool>();

	private Func<GameStateFile>? _snapshotFactory;

	/// <summary>
	/// Waehrend des Wiederherstellens unterdrueckt, sonst loesen die Aenderungsereignisse der
	/// Modelle sofort ein Schreiben genau des Standes aus, den wir gerade gelesen haben.
	/// </summary>
	private bool _suppressSave;

	public static string FilePath =>
		Path.Combine(AppContext.BaseDirectory, "_config", "stocktv.state.json");

	/// <summary>
	/// Meldet die Quelle fuer kuenftige Schnappschuesse an. Ab hier fuehrt jedes
	/// <see cref="RequestSave"/> zu einem Schreibvorgang.
	/// </summary>
	public void StartAutoSave(Func<GameStateFile> snapshotFactory)
		=> _snapshotFactory = snapshotFactory;

	/// <summary>
	/// Reiht einen Schreibvorgang ein. Bewusst nicht blockierend: das haengt sonst an jeder
	/// Punkteingabe, und auf einem Pi mit SD-Karte ist das spuerbar.
	/// </summary>
	public void RequestSave()
	{
		if (_suppressSave || _snapshotFactory is null)
			return;

		_saveQueue.Writer.TryWrite(true);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			await foreach (var _ in _saveQueue.Reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					if (_snapshotFactory is { } factory)
						await SaveAsync(factory());
				}
				catch (Exception ex)
				{
					// Ein misslungener Schreibvorgang darf das Spiel nicht stoeren - der naechste
					// Punkt schreibt den vollstaendigen Stand ohnehin erneut.
					logger.LogError("Spielstand konnte nicht gespeichert werden: {Msg}", ex.Message);
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Regulaeres Herunterfahren.
		}
	}

	/// <summary>
	/// Laedt den gespeicherten Spielstand, sofern er zur aktuellen Konfiguration passt und nicht
	/// zu alt ist. Liefert sonst <c>null</c> und raeumt die Datei weg.
	/// </summary>
	public async Task<GameStateFile?> LoadAsync(Settings.Settings settings)
	{
		var path = FilePath;

		// Reste eines abgebrochenen Schreibvorgangs entfernen.
		var orphanedTemp = path + ".tmp";
		if (File.Exists(orphanedTemp))
		{
			try { File.Delete(orphanedTemp); }
			catch (IOException) { /* naechster Start versucht es erneut */ }
		}

		if (!File.Exists(path))
			return null;

		GameStateFile? state;
		try
		{
			var json = await File.ReadAllTextAsync(path);
			state = JsonSerializer.Deserialize<GameStateFile>(json);
		}
		catch (Exception ex)
		{
			// Anders als bei der Konfiguration wird hier nichts als .corrupt aufgehoben: eine
			// Anzeige, die wegen einer defekten Punktedatei nicht hochkommt, waere der schlechteste
			// Tausch. Verwerfen und leer starten.
			logger.LogWarning("Spielstand unlesbar ({Msg}) - wird verworfen.", ex.Message);
			Discard();
			return null;
		}

		if (state is null)
		{
			Discard();
			return null;
		}

		if (!IsApplicable(state, settings, out var reason))
		{
			logger.LogInformation("Gespeicherter Spielstand wird nicht geladen: {Reason}", reason);
			Discard();
			return null;
		}

		logger.LogInformation("Spielstand von {SavedAt} wird wiederhergestellt.", state.SavedAtUtc);
		return state;
	}

	/// <summary>
	/// Fuehrt die Wiederherstellung aus, ohne dass die dabei ausgeloesten Aenderungsereignisse
	/// gleich wieder einen Schreibvorgang nach sich ziehen.
	/// </summary>
	public void RestoreWithoutSaving(Action restore)
	{
		_suppressSave = true;
		try
		{
			restore();
		}
		finally
		{
			_suppressSave = false;
		}
	}

	/// <summary>
	/// Prueft, ob ein gespeicherter Stand zum aktuellen Betrieb gehoert. Die Bahn- und
	/// Modus-Pruefung faengt nebenbei den Fall ab, dass jemand einen <c>_config</c>-Ordner von
	/// einer anderen Bahn kopiert hat.
	/// </summary>
	private static bool IsApplicable(GameStateFile state, Settings.Settings settings, out string reason)
	{
		var age = DateTimeOffset.UtcNow - state.SavedAtUtc;

		if (age > MaxAge)
		{
			reason = $"zu alt ({age.TotalHours:F1} h, erlaubt sind {MaxAge.TotalHours:F0} h)";
			return false;
		}

		// Ein Stand aus der Zukunft heisst: die Uhr des Geraets ist gesprungen (ein Pi ohne
		// Echtzeituhr stellt sie erst nach dem Start per NTP). Dann ist das Alter wertlos.
		if (age < -TimeSpan.FromMinutes(5))
		{
			reason = "in der Zukunft gespeichert (Uhrzeit des Geraets unplausibel)";
			return false;
		}

		if (state.BahnNummer != settings.General.BahnNummer)
		{
			reason = $"gehoert zu Bahn {state.BahnNummer}, dieses Geraet ist Bahn {settings.General.BahnNummer}";
			return false;
		}

		if (state.Modus != settings.Game.CurrentModus)
		{
			reason = $"wurde im Modus {state.Modus} gespeichert, aktiv ist {settings.Game.CurrentModus}";
			return false;
		}

		reason = string.Empty;
		return true;
	}

	/// <summary>
	/// Schreibt den Spielstand atomar. Ist nichts eingegeben, wird die Datei geloescht statt eine
	/// leere zu hinterlassen - sonst laege nach jedem Reset eine Datei herum, die beim naechsten
	/// Start nur wieder verworfen wird.
	/// </summary>
	public async Task SaveAsync(GameStateFile state)
	{
		if (state.Match.IsEmpty && state.Ziel.IsEmpty)
		{
			Discard();
			return;
		}

		await _saveGate.WaitAsync();

		try
		{
			var path = FilePath;
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			var tempPath = path + ".tmp";
			var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state, _jsonOptions));

			await using (var stream = new FileStream(
				tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
				bufferSize: 4096, FileOptions.WriteThrough | FileOptions.Asynchronous))
			{
				await stream.WriteAsync(bytes);
				await stream.FlushAsync();
			}

			File.Move(tempPath, path, overwrite: true);
		}
		finally
		{
			_saveGate.Release();
		}
	}

	/// <summary>Loescht den gespeicherten Spielstand - z.B. nach einem ResetResult.</summary>
	public void Discard()
	{
		try
		{
			if (File.Exists(FilePath))
				File.Delete(FilePath);
		}
		catch (IOException ex)
		{
			logger.LogWarning("Spielstand konnte nicht geloescht werden: {Msg}", ex.Message);
		}
	}

	public override void Dispose()
	{
		_saveQueue.Writer.TryComplete();
		base.Dispose();
	}
}
