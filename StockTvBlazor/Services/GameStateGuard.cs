namespace StockTvBlazor.Services;

/// <summary>
/// Zentrale Sicherheitssperre: liefert true, sobald irgendetwas hinterlegt ist, das bei einem
/// Reboot/einer Netzwerk-Umkonfiguration verloren ginge oder ein laufendes Match gefährden würde
/// (Kehren/Zielversuche persistieren zwar, Teamnamen und Ziel-Spielername aber nicht). Wird sowohl
/// von der Web-UI (SetupPage) als auch vom NetMQ-Pfad (NetMqResponseService) verwendet,
/// damit die Sperre nicht über einen der beiden Wege umgangen werden kann.
/// </summary>
public static class GameStateGuard
{
	public static bool HasRecordedValues(MatchService matchService, ZielService zielService) =>
		matchService.CurrentMatch.Games.Any(g => g.Turns.Count > 0) ||
		matchService.CurrentMatch.Begegnungen.Any() ||
		zielService.CurrentZielBewerb.AnzahlVersuche() > 0 ||
		zielService.CurrentZielBewerb.GesamtSumme > 0 ||
		!string.IsNullOrEmpty(zielService.CurrentZielBewerb.Spielername);
}
