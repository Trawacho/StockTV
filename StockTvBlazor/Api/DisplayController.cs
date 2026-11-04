using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>
/// Darstellung der Anzeige, die nicht Teil des NetMQ-Protokolls ist.
/// </summary>
/// <remarks>
/// Nur ueber REST erreichbar - das 10-Byte-Paket von <c>SetSettings</c> bleibt unveraendert.
/// </remarks>
[ApiController]
[Route("api/v1/display")]
[Produces("application/json")]
public sealed class DisplayController(
	SettingsService settingsService,
	GameCommandQueue commands,
	ILogger<DisplayController> logger) : ControllerBase
{
	/// <summary>
	/// Der Anspiel-Rahmen: Rahmen statt » bzw. « um die Mannschaft mit Anspiel. Gilt fuer alle
	/// Themes.
	/// </summary>
	[HttpGet("anspiel-frame")]
	[ProducesResponseType<AnspielFrameDto>(StatusCodes.Status200OK)]
	public ActionResult<AnspielFrameDto> GetAnspielFrame()
	{
		var frame = settingsService.CurrentSettings.UI.AnspielFrame;
		return new AnspielFrameDto(frame.ShowFrame, frame.FrameWidthPx);
	}

	/// <summary>
	/// Setzt den Anspiel-Rahmen. Ein weggelassenes Feld bleibt unveraendert; die Anzeige
	/// uebernimmt die Aenderung sofort.
	/// </summary>
	[HttpPut("anspiel-frame")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	public ActionResult<ActionResponse> SetAnspielFrame([FromBody] AnspielFrameUpdateDto? request)
	{
		if (request is null)
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurden keine Werte uebergeben.", null));

		// Vorab pruefen, damit der Aufrufer den Fehler sieht - ausgefuehrt wird spaeter in der
		// Warteschlange, wie alle Aenderungen von aussen.
		if (request.ShowFrame is null && request.FrameWidthPx is null)
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurde weder showFrame noch frameWidthPx uebergeben.", null));

		if (request.FrameWidthPx is < AnspielFrameSettings.MinWidthPx or > AnspielFrameSettings.MaxWidthPx)
			return BadRequest(new ActionResponse("InvalidRequest",
				$"frameWidthPx muss zwischen {AnspielFrameSettings.MinWidthPx} und {AnspielFrameSettings.MaxWidthPx} liegen.", null));

		logger.LogInformation("Anspiel-Rahmen ueber REST: ShowFrame={Show}, FrameWidthPx={Width}",
			request.ShowFrame?.ToString() ?? "unveraendert", request.FrameWidthPx?.ToString() ?? "unveraendert");

		commands.Enqueue(() => { settingsService.SetAnspielFrame(request.ShowFrame, request.FrameWidthPx); });

		return Accepted(new ActionResponse("Ok", "Anspiel-Rahmen wurde angenommen.", null));
	}
}

/// <summary>Der Anspiel-Rahmen, wie er gerade eingestellt ist.</summary>
/// <param name="ShowFrame">Rahmen statt Sonderzeichen anzeigen.</param>
/// <param name="FrameWidthPx">Rahmenstaerke in Pixeln (1-10).</param>
public sealed record AnspielFrameDto(bool ShowFrame, int FrameWidthPx);

/// <summary>Aenderung am Anspiel-Rahmen. Mindestens ein Feld muss gesetzt sein.</summary>
public sealed class AnspielFrameUpdateDto
{
	/// <summary>Rahmen statt Sonderzeichen anzeigen. Weglassen = unveraendert.</summary>
	public bool? ShowFrame { get; set; }

	/// <summary>Rahmenstaerke in Pixeln, 1-10. Weglassen = unveraendert.</summary>
	public int? FrameWidthPx { get; set; }
}
