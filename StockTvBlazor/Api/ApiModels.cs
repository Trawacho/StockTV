namespace StockTvBlazor.Api;

/// <summary>Antwort auf eine ausgeloeste Aktion.</summary>
/// <param name="Status">Einordnung des Ergebnisses, z. B. "Ok" oder "InvalidRequest".</param>
/// <param name="Message">Was passiert ist bzw. warum nicht.</param>
/// <param name="Detail">Zusatzangaben, etwa der Ablageort der Konfigurationsdatei.</param>
public sealed record ActionResponse(string Status, string Message, string? Detail);

/// <summary>Anforderung, den Schluessel der Fernsteuerung zu wechseln.</summary>
public sealed class ApiKeyRequest
{
	/// <summary>
	/// Der neue Schluessel. Mindestens 8 Zeichen; Leerzeichen am Rand werden entfernt, weil
	/// sie sich beim Kopieren einschleichen und dann niemand den Fehler findet.
	/// </summary>
	public string? NewKey { get; set; }
}
