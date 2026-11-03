namespace StockTvBlazor.Services;

/// <summary>
/// Verwaltet das Werbebild, das die zentrale Verwaltung ueber NetMQ auf die Anzeige schiebt
/// (Topics <c>SetImage</c>, <c>GoToImage</c>, <c>ClearImage</c>; drueben heissen die Aufrufe
/// <c>SetMarketingImage</c>/<c>ShowMarketing</c>/<c>ClearMarketingImage</c>).
/// </summary>
/// <remarks>
/// Das Bild liegt auf der Platte und nicht im Arbeitsspeicher, damit es einen Neustart der
/// Anzeige uebersteht - sonst muesste es die Zentrale nach jedem Stromausfall erneut schicken.
/// Ablage in <c>_config/marketing/</c>, weil das neben <c>_logs</c> der einzige Ordner ist, der
/// auf allen Plattformen gleich eingebunden ist.
/// </remarks>
public sealed class MarketingImageService(ILogger<MarketingImageService> logger)
{
	/// <summary>
	/// Obergrenze fuer ein Werbebild. Der Wert ist grosszuegig fuer ein Foto und klein genug,
	/// dass ein versehentlich geschicktes Video die SD-Karte eines Pi nicht vollschreibt.
	/// </summary>
	public const int MaxSizeBytes = 8 * 1024 * 1024;

	private static string Directory => Path.Combine(AppContext.BaseDirectory, "_config", "marketing");

	private readonly object _gate = new();

	/// <summary>Feuert, wenn ein Bild gesetzt oder entfernt wurde.</summary>
	public event Action? OnChanged;

	/// <summary>Der Dateiname, unter dem die Zentrale das Bild geschickt hat.</summary>
	public string? FileName
	{
		get
		{
			var file = CurrentFile;
			return file is null ? null : Path.GetFileName(file);
		}
	}

	public bool HasImage => CurrentFile is not null;

	/// <summary>
	/// Der Pfad des aktuellen Bildes oder <c>null</c>. Es liegt immer hoechstens eine Datei im
	/// Ordner - <see cref="SaveAsync"/> raeumt vorher auf.
	/// </summary>
	public string? CurrentFile
	{
		get
		{
			try
			{
				if (!System.IO.Directory.Exists(Directory))
					return null;

				return System.IO.Directory.EnumerateFiles(Directory)
					.FirstOrDefault(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
			}
			catch (IOException)
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Prueft ein eingehendes Bild, ohne es zu schreiben. Gibt einen Fehlerschluessel zurueck,
	/// wenn es abgelehnt wird - die Zeichenketten wandern unveraendert als <c>NACK:...</c>
	/// zurueck an die Zentrale.
	/// </summary>
	/// <remarks>
	/// Bewusst getrennt vom Schreiben: die Pruefung ist billig und laeuft direkt im
	/// NetMQ-Poller-Thread, damit die Antwort die Ablehnung enthaelt. Das Schreiben selbst
	/// gehoert - wie bei allen anderen Schreibkommandos - in den _actionChannel; ein
	/// blockierender Dateizugriff im Poller-Callback legt die gesamte Kommandoannahme still.
	/// </remarks>
	public string? Validate(byte[] data, out string? extension)
	{
		extension = null;

		if (data.Length == 0)
			return "empty-image";

		if (data.Length > MaxSizeBytes)
			return "image-too-large";

		// Auf die Signatur pruefen und nicht auf die Dateiendung: der Dateiname kommt von aussen,
		// die ersten Bytes beschreiben, was wirklich geliefert wurde. Ohne das wuerde die Anzeige
		// beliebige Daten als Bild ausliefern.
		extension = DetectExtension(data);
		return extension is null ? "unsupported-format" : null;
	}

	/// <summary>
	/// Schreibt ein zuvor mit <see cref="Validate"/> angenommenes Bild auf die Platte.
	/// </summary>
	public async Task SaveAsync(byte[] data, string fileName, string extension, CancellationToken ct = default)
	{
		var invalid = Path.GetInvalidFileNameChars();
		var safeName = new string([.. Path.GetFileNameWithoutExtension(fileName).Where(c => !invalid.Contains(c))]);
		if (string.IsNullOrWhiteSpace(safeName))
			safeName = "marketing";

		try
		{
			System.IO.Directory.CreateDirectory(Directory);

			// Erst das Alte weg, dann das Neue: es soll immer genau ein Bild im Ordner liegen.
			ClearFiles();

			var target = Path.Combine(Directory, safeName + extension);
			var tempPath = target + ".tmp";

			await File.WriteAllBytesAsync(tempPath, data, ct).ConfigureAwait(false);
			File.Move(tempPath, target, overwrite: true);

			logger.LogInformation("Werbebild empfangen: {File} ({Bytes} Byte)", Path.GetFileName(target), data.Length);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Werbebild konnte nicht gespeichert werden.");
			return;
		}

		OnChanged?.Invoke();
	}

	/// <summary>Entfernt das Bild.</summary>
	public void Clear()
	{
		lock (_gate)
			ClearFiles();

		logger.LogInformation("Werbebild entfernt.");
		OnChanged?.Invoke();
	}

	private void ClearFiles()
	{
		try
		{
			if (!System.IO.Directory.Exists(Directory))
				return;

			foreach (var file in System.IO.Directory.EnumerateFiles(Directory))
			{
				try { File.Delete(file); }
				catch (IOException ex) { logger.LogWarning("Datei {File} nicht loeschbar: {Msg}", file, ex.Message); }
			}
		}
		catch (IOException ex)
		{
			logger.LogWarning("Werbebild-Ordner nicht aufraeumbar: {Msg}", ex.Message);
		}
	}

	/// <summary>Der MIME-Typ zum aktuellen Bild, fuer die Auslieferung an den Browser.</summary>
	public static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".png" => "image/png",
		".jpg" => "image/jpeg",
		".gif" => "image/gif",
		".webp" => "image/webp",
		".bmp" => "image/bmp",
		_ => "application/octet-stream"
	};

	/// <summary>
	/// Erkennt das Format an den ersten Bytes. Rueckgabe ist die Dateiendung, <c>null</c> heisst
	/// "kein unterstuetztes Bild".
	/// </summary>
	private static string? DetectExtension(byte[] data)
	{
		static bool StartsWith(byte[] d, params byte[] signature)
			=> d.Length >= signature.Length && signature.Select((b, i) => d[i] == b).All(x => x);

		if (StartsWith(data, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return ".png";
		if (StartsWith(data, 0xFF, 0xD8, 0xFF)) return ".jpg";
		if (StartsWith(data, 0x47, 0x49, 0x46, 0x38)) return ".gif";
		if (StartsWith(data, 0x42, 0x4D)) return ".bmp";

		// WEBP: "RIFF" .... "WEBP"
		if (data.Length >= 12
			&& StartsWith(data, 0x52, 0x49, 0x46, 0x46)
			&& data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
		{
			return ".webp";
		}

		return null;
	}
}
