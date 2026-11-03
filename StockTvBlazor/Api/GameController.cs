using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Networking;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>
/// Spielstand und Einstellungen - die REST-Ablösung der NetMQ-Kommandos auf Port 4747.
/// </summary>
/// <remarks>
/// Alle schreibenden Aufrufe gehen ueber <see cref="GameCommandQueue"/> und nicht direkt in die
/// Dienste: NetMQ und REST laufen parallel, und weder <see cref="MatchService"/> noch
/// <see cref="ZielService"/> sind thread-sicher. Die Antwort bedeutet deshalb "angenommen", nicht
/// "bereits ausgefuehrt" - genau wie das <c>ACK</c> im NetMQ-Protokoll.
/// </remarks>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class GameController(
	SettingsService settingsService,
	MatchService matchService,
	ZielService zielService,
	GameCommandQueue commands,
	ILogger<GameController> logger) : ControllerBase
{
	/// <summary>Einfacher Erreichbarkeitstest - Gegenstueck zum NetMQ-Topic <c>Hello</c>.</summary>
	[HttpGet("hello")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status200OK)]
	public ActionResult<ActionResponse> Hello()
		=> new ActionResponse("Welcome", "StockTV ist erreichbar.", null);

	/// <summary>Auskunft ueber das Geraet - Gegenstueck zum NetMQ-Topic <c>Alive</c>.</summary>
	[HttpGet("info")]
	[ProducesResponseType<AliveDto>(StatusCodes.Status200OK)]
	public ActionResult<AliveDto> Info()
	{
		var alive = AliveInfo.Create(HttpContext.RequestServices.GetRequiredService<PlatformInfoService>().OsVersion);

		return new AliveDto(
			alive.HostName ?? "",
			alive.IpAddress ?? "",
			alive.AppVersion ?? "",
			alive.OsVersion ?? "",
			settingsService.CurrentSettings.General.BahnNummer);
	}

	/// <summary>
	/// Der vollstaendige Spielstand - Gegenstueck zu <c>GetResult</c>. Je nach Modus ist
	/// <c>match</c> oder <c>ziel</c> belegt.
	/// </summary>
	[HttpGet("result")]
	[ProducesResponseType<ResultDto>(StatusCodes.Status200OK)]
	public ActionResult<ResultDto> GetResult()
		=> GameDtoFactory.Result(
			settingsService.CurrentSettings, matchService.CurrentMatch, zielService.CurrentZielBewerb);

	/// <summary>Setzt den Spielstand zurueck - Gegenstueck zu <c>ResetResult</c>.</summary>
	[HttpPost("result/reset")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	public ActionResult<ActionResponse> ResetResult()
	{
		logger.LogInformation("ResetResult ueber REST angefordert");

		commands.Enqueue(() =>
		{
			var modus = settingsService.CurrentSettings.Game.CurrentModus;

			if (modus is GameSettings.Modus.Ziel or GameSettings.Modus.Ziel2)
				zielService.CurrentZielBewerb.Reset();
			else
				matchService.CurrentMatch.Reset(true);
		});

		return Accepted(new ActionResponse("Ok", "Zuruecksetzen wurde angenommen.", null));
	}

	/// <summary>Die aktuellen Einstellungen - Gegenstueck zu <c>GetSettings</c>.</summary>
	[HttpGet("settings")]
	[ProducesResponseType<SettingsDto>(StatusCodes.Status200OK)]
	public ActionResult<SettingsDto> GetSettings()
		=> GameDtoFactory.Settings(settingsService.CurrentSettings);

	/// <summary>
	/// Setzt die Einstellungen - Gegenstueck zu <c>SetSettings</c>. Ein Moduswechsel laesst die
	/// Anzeige auf die passende Seite wechseln.
	/// </summary>
	[HttpPut("settings")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetSettings([FromBody] SettingsUpdateDto? request)
	{
		if (request is null)
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurden keine Einstellungen uebergeben.", null));

		if (!Enum.IsDefined(request.Modus) || !Enum.IsDefined(request.Richtung) || !Enum.IsDefined(request.Theme))
			return BadRequest(new ActionResponse("InvalidRequest", "Modus, Richtung oder Theme ist unbekannt.", null));

		// Byte-Werte: alles darueber wuerde beim Umwandeln still ueberlaufen.
		if (request.BahnNummer is < 0 or > 255 || request.Spielgruppe is < 0 or > 255
			|| request.MaxPunkteProKehre is < 0 or > 255 || request.MaxKehrenProSpiel is < 0 or > 255
			|| request.MidColumnWidth is < 0 or > 255)
		{
			return BadRequest(new ActionResponse("InvalidRequest", "Ein Zahlenwert liegt ausserhalb von 0-255.", null));
		}

		logger.LogInformation("SetSettings ueber REST angefordert");

		// Bewusst ueber das Byte-Paket: so nehmen REST und NetMQ denselben Pfad samt dessen
		// Theme-Logik und der Navigation bei einem Moduswechsel.
		var payload = request.ToLegacyBytes();
		commands.Enqueue(() => settingsService.SetSettings(payload));

		return Accepted(new ActionResponse("Ok", "Einstellungen wurden angenommen.", null));
	}

	/// <summary>
	/// Setzt die Begegnungen - Gegenstueck zu <c>SetTeamNames</c>. Ersetzt immer die komplette
	/// Liste.
	/// </summary>
	[HttpPut("match/teamnames")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetTeamNames([FromBody] IReadOnlyList<BegegnungDto>? begegnungen)
	{
		if (begegnungen is null)
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurden keine Begegnungen uebergeben.", null));

		// Die Namen landen in einem ':'/';'-getrennten String - beide Zeichen wuerden ihn zerreissen.
		if (begegnungen.Any(b => b.MannschaftA.Contains(':') || b.MannschaftA.Contains(';')
							  || b.MannschaftB.Contains(':') || b.MannschaftB.Contains(';')))
		{
			return BadRequest(new ActionResponse("InvalidRequest",
				"Mannschaftsnamen duerfen weder ':' noch ';' enthalten.", null));
		}

		logger.LogInformation("SetTeamNames ueber REST angefordert: {Count} Begegnung(en)", begegnungen.Count);

		var payload = string.Join(';',
			begegnungen.Select(b => $"{b.Spielnummer}:{b.MannschaftA}:{b.MannschaftB}"));

		commands.Enqueue(() => matchService.SetTeamNames(System.Text.Encoding.UTF8.GetBytes(payload)));

		return Accepted(new ActionResponse("Ok", "Begegnungen wurden angenommen.", null));
	}

	/// <summary>Setzt den Namen des Teilnehmers - Gegenstueck zu <c>SetTeilnehmer</c>.</summary>
	[HttpPut("ziel/teilnehmer")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetTeilnehmer([FromBody] TeilnehmerRequest? request)
	{
		var name = request?.Name?.Trim();

		if (string.IsNullOrEmpty(name))
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurde kein Name uebergeben (Feld name).", null));

		logger.LogInformation("SetTeilnehmer ueber REST angefordert");

		commands.Enqueue(() => zielService.SetTeilnehmer(System.Text.Encoding.UTF8.GetBytes(name)));

		return Accepted(new ActionResponse("Ok", "Teilnehmer wurde angenommen.", null));
	}
}

/// <summary>Anforderung, den Teilnehmer des Zielbewerbs zu setzen.</summary>
public sealed class TeilnehmerRequest
{
	public string? Name { get; set; }
}
