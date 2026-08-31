using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>
/// Die Einstellungen, die das zentrale Verwaltungsprogramm setzen und lesen darf.
/// </summary>
/// <remarks>
/// Entspricht Feld fuer Feld dem 10-Byte-Paket der NetMQ-Topics <c>GetSettings</c>/<c>SetSettings</c>
/// (siehe <see cref="SettingsService.GetSettings"/>) - nur mit Namen statt Positionen. Byte 9
/// (<c>MessageVersion</c>) wird nur gelesen, Byte 10 ist dort ungenutzt und entfaellt hier.
/// </remarks>
public sealed record SettingsDto(
	int BahnNummer,
	int Spielgruppe,
	GameSettings.Modus Modus,
	UiSettings.Richtung Richtung,
	UiSettings.Theme Theme,
	int MaxPunkteProKehre,
	int MaxKehrenProSpiel,
	int MidColumnWidth,
	int MessageVersion);

/// <summary>Einstellungen, die geschrieben werden duerfen - ohne die nur lesbare MessageVersion.</summary>
public sealed class SettingsUpdateDto
{
	public int BahnNummer { get; set; } = 1;
	public int Spielgruppe { get; set; }
	public GameSettings.Modus Modus { get; set; }
	public UiSettings.Richtung Richtung { get; set; }
	public UiSettings.Theme Theme { get; set; }
	public int MaxPunkteProKehre { get; set; } = 10;
	public int MaxKehrenProSpiel { get; set; } = 6;
	public int MidColumnWidth { get; set; } = 90;

	/// <summary>
	/// Uebersetzt zurueck in das 10-Byte-Paket, damit REST und NetMQ denselben Pfad in
	/// <see cref="SettingsService.SetSettings"/> nehmen - inklusive dessen Pruefungen und der
	/// Navigation bei einem Moduswechsel.
	/// </summary>
	public byte[] ToLegacyBytes() =>
	[
		(byte)BahnNummer,
		(byte)Spielgruppe,
		(byte)Modus,
		(byte)Richtung,
		(byte)Theme,
		(byte)MaxPunkteProKehre,
		(byte)MaxKehrenProSpiel,
		(byte)MidColumnWidth,
		1,
		0
	];
}

public sealed record TurnDto(int TurnNumber, int PointsLeft, int PointsRight);

public sealed record GameDto(
	int GameNumber, int PointsLeft, int PointsRight, IReadOnlyList<TurnDto> Turns);

public sealed record BegegnungDto(int Spielnummer, string MannschaftA, string MannschaftB);

/// <summary>Spielstand der Modi Training, BestOf und Turnier.</summary>
public sealed record MatchResultDto(
	int PointsLeft,
	int PointsRight,
	int MatchPointsLeft,
	int MatchPointsRight,
	IReadOnlyList<GameDto> Games,
	IReadOnlyList<BegegnungDto> Begegnungen);

public sealed record ZielDisziplinDto(string Name, int Nummer, IReadOnlyList<int> Versuche, int Summe);

/// <summary>Spielstand der Modi Ziel und Ziel2.</summary>
public sealed record ZielResultDto(
	string Spielername,
	int Durchgang,
	int GesamtSumme,
	int AnzahlVersuche,
	int MaxVersuche,
	IReadOnlyList<ZielDisziplinDto> Disziplinen);

/// <summary>
/// Der vollstaendige Spielstand. Genau eines von <see cref="Match"/> und <see cref="Ziel"/> ist
/// belegt - welches, entscheidet <see cref="SettingsDto.Modus"/>.
/// </summary>
/// <remarks>
/// Loest die NetMQ-Antwort auf <c>GetResult</c> ab, die 10 Byte Kopf und daran angehaengtes
/// UTF-8-JSON war und beim Empfaenger von Hand zerlegt werden musste.
/// </remarks>
public sealed record ResultDto(SettingsDto Settings, MatchResultDto? Match, ZielResultDto? Ziel);

/// <summary>Auskunft ueber das Geraet - Gegenstueck zum NetMQ-Topic <c>Alive</c>.</summary>
public sealed record AliveDto(
	string HostName, string IpAddress, string AppVersion, string OsVersion, int BahnNummer);

/// <summary>Erzeugt die DTOs aus dem laufenden Zustand.</summary>
public static class GameDtoFactory
{
	public static SettingsDto Settings(Settings.Settings s)
	{
		// Denselben Weg nehmen wie das Byte-Paket, damit REST und NetMQ nie auseinanderlaufen -
		// insbesondere bei der Theme-Ermittlung (Custom Theme -> BaseTheme).
		var theme = s.UI.ActiveTheme switch
		{
			BuiltInTheme builtIn => builtIn.ThemeType,
			CustomTheme { BaseTheme: { } baseTheme } => baseTheme,
			_ => UiSettings.Theme.Hell
		};

		return new SettingsDto(
			s.General.BahnNummer,
			s.General.Spielgruppe,
			s.Game.CurrentModus,
			s.UI.CurrentRichtung,
			theme,
			s.Game.MaxPunkteProKehre,
			s.Game.MaxKehrenProSpiel,
			s.UI.MidColumnWidth,
			s.General.MessageVersion);
	}

	public static MatchResultDto Match(Match match) => new(
		match.LeftPointsOverAll,
		match.RightPointsOverAll,
		match.MatchPointsLeft,
		match.MatchPointsRight,
		[.. match.Games.Select(g => new GameDto(
			g.GameNumber,
			g.Turns.Sum(t => t.PointsLeft),
			g.Turns.Sum(t => t.PointsRight),
			[.. g.Turns.Select(t => new TurnDto(t.TurnNumber, t.PointsLeft, t.PointsRight))]))],
		[.. match.Begegnungen.Select(b => new BegegnungDto(b.Spielnummer, b.MannschaftA, b.MannschaftB))]);

	public static ZielResultDto Ziel(ZielBewerb ziel)
	{
		var snapshot = ziel.CreateSnapshot();

		return new ZielResultDto(
			ziel.Spielername,
			snapshot.Durchgang,
			ziel.GesamtSumme,
			ziel.AnzahlVersucheDisplay,
			ziel.MaxVersucheDisplay,
			[
				// Reihenfolge und Nummern wie in ZielBewerb.SerializeJson - die Zentrale erwartet
				// die vier Disziplinen in genau dieser Abfolge.
				new ZielDisziplinDto("MassenVorne", 1, snapshot.MassenVorne, ziel.MassenVorneSumme),
				new ZielDisziplinDto("Schiessen",   2, snapshot.Schiessen,   ziel.SchiessenSumme),
				new ZielDisziplinDto("MassenSeite", 3, snapshot.MassenSeite, ziel.MassenSeiteSumme),
				new ZielDisziplinDto("Kombinieren", 4, snapshot.Kombinieren, ziel.KombinierenSumme)
			]);
	}

	/// <summary>Baut den Spielstand passend zum eingestellten Modus.</summary>
	public static ResultDto Result(Settings.Settings settings, Match match, ZielBewerb ziel)
	{
		var isZiel = settings.Game.CurrentModus is GameSettings.Modus.Ziel or GameSettings.Modus.Ziel2;

		return new ResultDto(
			Settings(settings),
			isZiel ? null : Match(match),
			isZiel ? Ziel(ziel) : null);
	}
}
