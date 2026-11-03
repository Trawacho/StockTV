namespace StockTvBlazor.Services;

/// <summary>
/// Zaehlt, wie viele Zuhoerer gerade an der Anzeige haengen - über NetMQ-SUB und über den
/// SignalR-Hub. Solange mindestens einer da ist, gilt <c>BlockLocalChanges</c>: die zentrale
/// Verwaltung fuehrt, lokale Tastatureingaben und das schnelle Loeschen sind gesperrt.
/// </summary>
/// <remarks>
/// Braucht es, seit beide Protokolle nebeneinander laufen: vorher hat der NetMQ-Publisher den
/// Schalter direkt gesetzt. Wuerde der SignalR-Hub das ebenfalls tun, wuerden sich beide
/// gegenseitig ueberschreiben - der Letzte gewinnt, und ein NetMQ-Abonnent verliert die Sperre,
/// sobald sich ein SignalR-Client abmeldet.
///
/// Nebenbei behoben: die NetMQ-Seite hat den Schalter bisher pro Frame <em>umgeschaltet</em>
/// statt gezaehlt. Bei zwei Abonnenten oder einem Reconnect kippte er dadurch in den falschen
/// Zustand.
/// </remarks>
public sealed class SubscriberRegistry(SettingsService settingsService, ILogger<SubscriberRegistry> logger)
{
	private readonly Lock _gate = new();

	private int _netMqSubscribers;

	private int _signalRClients;

	public int NetMqSubscribers { get { lock (_gate) return _netMqSubscribers; } }

	public int SignalRClients { get { lock (_gate) return _signalRClients; } }

	/// <summary>Ein NetMQ-Client hat ein Topic abonniert bzw. gekuendigt.</summary>
	public void NetMqSubscriptionChanged(bool subscribed)
	{
		lock (_gate)
		{
			_netMqSubscribers = Math.Max(0, _netMqSubscribers + (subscribed ? 1 : -1));
			Apply();
		}
	}

	public void SignalRConnected()
	{
		lock (_gate)
		{
			_signalRClients++;
			Apply();
		}
	}

	public void SignalRDisconnected()
	{
		lock (_gate)
		{
			_signalRClients = Math.Max(0, _signalRClients - 1);
			Apply();
		}
	}

	/// <summary>Aufrufer haelt <see cref="_gate"/>.</summary>
	private void Apply()
	{
		var block = _netMqSubscribers > 0 || _signalRClients > 0;

		logger.LogDebug("Zuhoerer: NetMQ={NetMq}, SignalR={SignalR} -> BlockLocalChanges={Block}",
			_netMqSubscribers, _signalRClients, block);

		settingsService.ChangeBlockLocalChanges(block);
	}
}
