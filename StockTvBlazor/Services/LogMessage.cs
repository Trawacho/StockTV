namespace StockTvBlazor.Services;

/// <summary>
/// Ein Eintrag der Protokolldatei. Die Ausnahme steht als Text drin: ein <see cref="System.Exception"/>-Objekt laesst
/// sich nicht als JSON schreiben (<c>TargetSite</c>) - das hat frueher den Schreib-Task beendet, danach kam nichts mehr
/// in die Datei.
/// </summary>
public record LogMessage(
	DateTime Timestamp,
	LogLevel Level,
	string Category,
	string Message,
	string? Exception
);
