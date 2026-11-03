using System.Text;
using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>Die Konfiguration des Geraets einsehen und den Schluessel der Fernsteuerung wechseln.</summary>
[ApiController]
[Route("api/v1/config")]
[Produces("application/json")]
public sealed class ConfigController(SettingsService settings, ILogger<ConfigController> logger) : ControllerBase
{
	private const string FileName = "stocktv.device.json";

	/// <summary>
	/// Laedt die Geraete-Konfiguration herunter, so wie sie auf dem Geraet liegt: Protokollierung,
	/// Netzwerk und REST-Schnittstelle. Der API-Schluessel steht darin verschluesselt.
	/// </summary>
	/// <remarks>
	/// Bewusst nur die Geraetedatei und nicht die Betriebs-Konfiguration: Spielstand, Teamnamen
	/// und Einstellungen des laufenden Bewerbs gehen die Fernwartung nichts an. Sie kommen ueber
	/// die Spiel-Endpunkte.
	/// </remarks>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Download()
	{
		var path = SettingsService.GetDeviceFilePath();

		if (!System.IO.File.Exists(path))
		{
			return NotFound(new ActionResponse(
				"NotFound", $"Es gibt keine Konfigurationsdatei unter {path}.", null));
		}

		logger.LogInformation("Konfigurationsdatei wird heruntergeladen.");

		var content = await System.IO.File.ReadAllTextAsync(path);
		return File(Encoding.UTF8.GetBytes(content), "application/json", FileName);
	}

	/// <summary>
	/// Wechselt den Schluessel der Fernsteuerung. Der neue gilt sofort - der bisherige ab
	/// dieser Antwort nicht mehr.
	/// </summary>
	/// <param name="request">Der neue Schluessel.</param>
	[HttpPost("api-key")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> ChangeApiKey([FromBody] ApiKeyRequest? request)
	{
		string newKey = (request?.NewKey ?? string.Empty).Trim();

		if (newKey.Length == 0)
			return Reject("Es wurde kein neuer Schluessel uebergeben (Feld newKey).");

		if (newKey.Length < MinimumApiKeyLength)
		{
			return Reject(
				$"Der Schluessel ist zu kurz - mindestens {MinimumApiKeyLength} Zeichen. " +
				"Er ist das Einzige, was zwischen dem Netz und dieser Schnittstelle steht.");
		}

		var restApi = settings.CurrentSettings.Device.RestApi;

		if (string.Equals(newKey, restApi.ApiKey, StringComparison.Ordinal))
			return Reject("Der neue Schluessel ist der bisherige.");

		string previous = restApi.ApiKey;

		try
		{
			// Erst in den laufenden Betrieb, dann auf die Platte: die Pruefung in
			// ApiKeyMiddleware liest den Wert bei jeder Anfrage aus genau diesem Objekt.
			restApi.ApiKey = newKey;

			// Bewusst SaveSettingsNowAsync und nicht RequestSaveSettings: nur so laesst sich ein
			// gescheitertes Schreiben ueberhaupt bemerken und unten zuruecknehmen.
			await settings.SaveSettingsNowAsync(SettingsScope.Device);
		}
		catch (Exception ex)
		{
			// Zuruecknehmen, sonst gaelte ein Schluessel, der nirgends steht - und nach dem
			// naechsten Start kaeme niemand mehr auf das Geraet.
			restApi.ApiKey = previous;

			logger.LogError(ex, "Der API-Schluessel konnte nicht gespeichert werden.");

			return StatusCode(StatusCodes.Status500InternalServerError, new ActionResponse(
				"Failed",
				"Der Schluessel konnte nicht gespeichert werden und bleibt unveraendert.",
				ex.Message));
		}

		logger.LogWarning("Der API-Schluessel wurde ueber die Schnittstelle gewechselt.");

		return Ok(new ActionResponse(
			"Ok",
			"Der Schluessel gilt ab sofort. Der bisherige ist damit ungueltig.",
			$"Verschluesselt abgelegt in {SettingsService.GetDeviceFilePath()}"));
	}

	/// <summary>
	/// Kurz genug zum Eintippen, lang genug, dass Durchprobieren keine Aussicht hat. Wer es
	/// genauer will, nimmt einen erzeugten Schluessel.
	/// </summary>
	private const int MinimumApiKeyLength = 8;

	private BadRequestObjectResult Reject(string message)
		=> BadRequest(new ActionResponse("InvalidRequest", message, null));
}
