using StockTvBlazor.Api;

namespace StockTvBlazor.Services;

/// <summary>
/// Verteilt Ereignisse der Anzeige an alle angeschlossenen Protokolle.
/// </summary>
/// <remarks>
/// Solange NetMQ und die REST-/SignalR-Schnittstelle nebeneinander laufen, muss jedes Ergebnis an
/// beide gehen - sonst sieht eine noch nicht umgestellte StockAppV2 den Spielstand nicht mehr.
/// <see cref="MatchService"/>/<see cref="ZielService"/> rufen deshalb weiterhin den
/// NetMQ-Publisher direkt und zusaetzlich diesen Verteiler.
///
/// Der SignalR-Hub lebt im eigenen Web-Host der REST-Schnittstelle und damit in einem anderen
/// DI-Container (siehe <c>Api/StockTvApiHost.cs</c>). Er kann hier deshalb nicht injiziert werden;
/// stattdessen haengt sich der Host beim Start an <see cref="OnBroadcast"/>. Ist die Schnittstelle
/// abgeschaltet oder nicht gestartet, bleibt das Ereignis unabonniert und der Aufruf verpufft
/// folgenlos.
/// </remarks>
public sealed class GameEventBroadcaster(ILogger<GameEventBroadcaster> logger)
{
	/// <summary>Methodenname beim Client und die Nutzlast.</summary>
	public event Func<string, object, Task>? OnBroadcast;

	public void ResultChanged(ResultDto result) => Send("ResultChanged", result);

	public void SettingsChanged(SettingsDto settings) => Send("SettingsChanged", settings);

	public void Alive(AliveDto alive) => Send("Alive", alive);

	private void Send(string method, object payload)
	{
		var handler = OnBroadcast;
		if (handler is null)
			return;

		// Bewusst nicht abgewartet: der Aufruf kommt aus der Punkteingabe bzw. einem Zeitgeber,
		// und ein haengender SignalR-Client darf die Anzeige nicht ausbremsen.
		_ = Task.Run(async () =>
		{
			try
			{
				await handler(method, payload);
			}
			catch (Exception ex)
			{
				logger.LogWarning("Senden von {Method} an die SignalR-Clients fehlgeschlagen: {Msg}",
					method, ex.Message);
			}
		});
	}
}
