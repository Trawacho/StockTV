using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>
/// Das Werbebild auf der Anzeige - REST-Ablösung der NetMQ-Kommandos <c>SetImage</c>,
/// <c>GoToImage</c> und <c>ClearImage</c>.
/// </summary>
[ApiController]
[Route("api/v1/image")]
[Produces("application/json")]
public sealed class ImageController(
	MarketingImageService images,
	SettingsService settingsService,
	GameCommandQueue commands,
	ILogger<ImageController> logger) : ControllerBase
{
	/// <summary>Ob gerade ein Bild hinterlegt ist und unter welchem Namen.</summary>
	[HttpGet]
	[ProducesResponseType<ImageStatusResponse>(StatusCodes.Status200OK)]
	public ActionResult<ImageStatusResponse> Status()
		=> new ImageStatusResponse(images.HasImage, images.FileName);

	/// <summary>
	/// Laedt ein Werbebild hoch - Gegenstueck zu <c>SetImage</c>. Das Format wird an der Signatur
	/// geprueft (PNG, JPEG, GIF, BMP, WebP), nicht an der Dateiendung.
	/// </summary>
	/// <param name="file">Die Bilddatei.</param>
	[HttpPut]
	[Consumes("multipart/form-data")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	[RequestSizeLimit(MarketingImageService.MaxSizeBytes + 4096)]
	public async Task<ActionResult<ActionResponse>> Upload(IFormFile? file)
	{
		if (file is null || file.Length == 0)
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurde keine Datei uebertragen.", null));

		if (file.Length > MarketingImageService.MaxSizeBytes)
		{
			return BadRequest(new ActionResponse("InvalidRequest",
				$"Die Datei ist mit {file.Length} Byte zu gross (erlaubt sind {MarketingImageService.MaxSizeBytes} Byte).",
				null));
		}

		using var buffer = new MemoryStream();
		await file.CopyToAsync(buffer);
		var data = buffer.ToArray();

		var error = images.Validate(data, out var extension);
		if (error is not null)
			return BadRequest(new ActionResponse("InvalidRequest", $"Bild abgelehnt: {error}", null));

		logger.LogInformation("Werbebild ueber REST empfangen: {File} ({Bytes} Byte)", file.FileName, data.Length);

		commands.Enqueue(() => images.SaveAsync(data, file.FileName, extension!));

		return Accepted(new ActionResponse("Ok", "Bild wurde angenommen.", null));
	}

	/// <summary>Zeigt das hinterlegte Bild auf der Anzeige - Gegenstueck zu <c>GoToImage</c>.</summary>
	[HttpPost("show")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status409Conflict)]
	public ActionResult<ActionResponse> Show()
	{
		if (!images.HasImage)
		{
			return Conflict(new ActionResponse("NoImage",
				"Es ist kein Bild hinterlegt - erst hochladen (PUT /api/v1/image).", null));
		}

		logger.LogInformation("GoToImage ueber REST angefordert");
		commands.Enqueue(() => settingsService.RequestNavigation("/marketing"));

		return Accepted(new ActionResponse("Ok", "Anzeige wechselt auf das Werbebild.", null));
	}

	/// <summary>
	/// Entfernt das Bild und schaltet die Anzeige zurueck auf den Spielmodus - Gegenstueck zu
	/// <c>ClearImage</c>.
	/// </summary>
	[HttpDelete]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	public ActionResult<ActionResponse> Clear()
	{
		logger.LogInformation("ClearImage ueber REST angefordert");

		commands.Enqueue(() =>
		{
			images.Clear();
			settingsService.RequestNavigation(
				SettingsService.GetModusUrl(settingsService.CurrentSettings.Game.CurrentModus));
		});

		return Accepted(new ActionResponse("Ok", "Bild wird entfernt, Anzeige kehrt zum Spiel zurueck.", null));
	}
}

/// <summary>Ob ein Werbebild hinterlegt ist.</summary>
public sealed record ImageStatusResponse(bool HasImage, string? FileName);
