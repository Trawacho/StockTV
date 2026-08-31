using System.Text.Json.Serialization;
using StockTvBlazor.Models;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Services;

/// <summary>
/// Der Dateiinhalt von <c>_config/stocktv.state.json</c> - der laufende Spielstand, damit ein
/// Neustart mitten im Bewerb nicht die eingegebenen Punkte kostet.
/// </summary>
/// <remarks>
/// Die Kopffelder tragen die Gueltigkeitspruefung beim Laden: <see cref="SavedAtUtc"/> gegen das
/// Alter, <see cref="BahnNummer"/> und <see cref="Modus"/> dagegen, dass ein fremder oder ein zu
/// einem anderen Spielmodus gehoerender Stand geladen wird.
///
/// Bewusst eigene Klassen statt der Laufzeitmodelle: <see cref="Match"/> und
/// <see cref="ZielBewerb"/> haengen am SettingsService und haben Ereignisse und abgeleitete
/// Werte, die in einer Datei nichts verloren haben.
/// </remarks>
public sealed class GameStateFile
{
	public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public int BahnNummer { get; set; }

	public GameSettings.Modus Modus { get; set; }

	public MatchState Match { get; set; } = new();

	public ZielState Ziel { get; set; } = new();
}

/// <summary>Spielstand der Modi Training, BestOf und Turnier.</summary>
public sealed class MatchState
{
	public List<GameState> Games { get; set; } = [];

	public List<BegegnungState> Begegnungen { get; set; } = [];

	/// <summary>Leer heisst: nichts wiederherzustellen.</summary>
	[JsonIgnore]
	public bool IsEmpty => Games.All(g => g.Turns.Count == 0) && Begegnungen.Count == 0;
}

public sealed class GameState
{
	public int GameNumber { get; set; }

	public List<Turn> Turns { get; set; } = [];
}

public sealed class BegegnungState
{
	public int Spielnummer { get; set; }

	public string MannschaftA { get; set; } = string.Empty;

	public string MannschaftB { get; set; } = string.Empty;
}

/// <summary>Spielstand der Modi Ziel und Ziel2.</summary>
public sealed class ZielState
{
	public List<int> MassenVorne { get; set; } = [];

	public List<int> Schiessen { get; set; } = [];

	public List<int> MassenSeite { get; set; } = [];

	public List<int> Kombinieren { get; set; } = [];

	public string Spielername { get; set; } = string.Empty;

	/// <summary>Nur im Modus Ziel2 belegt: die Summe der abgeschlossenen ersten Runde.</summary>
	public int Runde1Summe { get; set; }

	public int Durchgang { get; set; } = 1;

	[JsonIgnore]
	public bool IsEmpty =>
		MassenVorne.Count == 0 && Schiessen.Count == 0 && MassenSeite.Count == 0
		&& Kombinieren.Count == 0 && Spielername.Length == 0 && Runde1Summe == 0;
}
