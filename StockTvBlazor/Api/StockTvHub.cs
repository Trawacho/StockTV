using Microsoft.AspNetCore.SignalR;
using StockTvBlazor.Services;

namespace StockTvBlazor.Api;

/// <summary>
/// Der Live-Kanal zur zentralen Verwaltung - Gegenstueck zum NetMQ-PUB-Socket auf Port 4748.
/// </summary>
/// <remarks>
/// Der Server sendet, der Client hoert nur zu. Abgeloest werden damit die beiden Topics des
/// Publishers:
/// <list type="bullet">
/// <item><c>ResultChanged</c> (<see cref="ResultDto"/>) - bei jeder Eingabe, statt
/// <c>Publish("GetResult", ...)</c></item>
/// <item><c>Alive</c> (<see cref="AliveDto"/>) - alle 5 Sekunden, statt <c>Publish("Alive", ...)</c></item>
/// <item><c>SettingsChanged</c> (<see cref="SettingsDto"/>) - neu, im NetMQ-Protokoll gab es das nicht</item>
/// </list>
///
/// Die reine Anwesenheit eines Clients setzt <c>BlockLocalChanges</c> - genauso wie ein
/// NetMQ-Abonnent. Anders als dort ist es hier keine Vermutung aus Subscribe-Frames, sondern der
/// tatsaechliche Verbindungszustand.
///
/// Der Schluessel wird von <see cref="ApiKeyMiddleware"/> geprueft; der .NET-Client uebergibt ihn
/// als Kopfzeile (<c>HttpConnectionOptions.Headers</c>), die auch beim Aushandeln mitgeht.
/// </remarks>
public sealed class StockTvHub(SubscriberRegistry subscribers, ILogger<StockTvHub> logger) : Hub
{
	public override Task OnConnectedAsync()
	{
		subscribers.SignalRConnected();
		logger.LogInformation("SignalR-Client verbunden: {ConnectionId}", Context.ConnectionId);

		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		subscribers.SignalRDisconnected();
		logger.LogInformation("SignalR-Client getrennt: {ConnectionId}", Context.ConnectionId);

		return base.OnDisconnectedAsync(exception);
	}
}
