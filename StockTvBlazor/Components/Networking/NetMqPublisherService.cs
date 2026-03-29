using NetMQ;
using NetMQ.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;

namespace StockTvBlazor.Components.Networking;

public class NetMqPublisherService : BackgroundService
{
	private readonly XPublisherSocket _pubSocket; // XPublisher für Subscription-Check
	private readonly NetMQPoller _poller;
	private readonly NetMQTimer _aliveTimer;
	private readonly Channel<(string Topic, object Payload)> _messageChannel;
	private readonly string _serializedAliveInfo;

	public NetMqPublisherService()
	{
		_pubSocket = new XPublisherSocket();
		_pubSocket.Bind("tcp://*:4748"); // Dein alter Port
		_pubSocket.Options.SendHighWatermark = 100;

		// Alive-Timer (alle 5 Sekunden wie im alten Projekt)
		_serializedAliveInfo = JsonSerializer.Serialize(AliveInfo.Create());
		_aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
		_aliveTimer.Elapsed += (s, e) => {
			Publish("Alive", _serializedAliveInfo);
		};

		// Poller für Timer und Subscription-Erkennung
		_poller = [_pubSocket, _aliveTimer];
		_messageChannel = Channel.CreateUnbounded<(string, object)>();

		// Subscription-Logik (optional aus dem letzten Schritt)
		_pubSocket.ReceiveReady += OnReceiveReady;
	}
	private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		var msg = e.Socket.ReceiveFrameBytes();
		// Hier könntest du auf Abos reagieren
		_pubSocket.SendMoreFrame(msg).SendFrame(msg);
	}

	// DIESE FUNKTION rufst du von anderen Klassen auf
	public void Publish(string topic, string payload)
	{
		_messageChannel.Writer.TryWrite((topic, payload));
	}

	// Version 2: Für Binär-Daten (Byte-Arrays)
	public void Publish(string topic, byte[] data)
	{
		_messageChannel.Writer.TryWrite((topic, data));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Wir lesen kontinuierlich aus dem Channel und senden über NetMQ
		await foreach (var (topic, payload) in _messageChannel.Reader.ReadAllAsync(stoppingToken))
		{
			_pubSocket.SendMoreFrame(topic);
			// Prüfen, ob es Text oder Bytes sind und passend senden
			if (payload is string text)
			{
				_pubSocket.SendFrame(text);
			}
			else if (payload is byte[] bytes)
			{
				_pubSocket.SendFrame(bytes);
			}
		}
	}

	public override void Dispose()
	{
		_pubSocket.ReceiveReady -= OnReceiveReady;
		_pubSocket.Close();
		_pubSocket.Dispose();
		NetMQConfig.Cleanup();
		base.Dispose();
	}
}


public class AliveInfo
{
	public string IpAddress { get; set; } = "127.0.0.1";
	public string HostName { get; set; } = Environment.MachineName;
	public string AppVersion { get; set; } = "1.0.0";

	public static AliveInfo Create() 
	{
		return new AliveInfo
		{
			AppVersion = Assembly.GetExecutingAssembly()
										 .GetName()
										 .Version?
										 .ToString() ?? "0.0.0.0",

			IpAddress = GetLocalServerIp() ?? "127.0.0.1"
		};
	}

	private static string GetLocalServerIp()
	{
		// Holt alle IP-Adressen des Host-Rechners
		var host = Dns.GetHostEntry(Dns.GetHostName());

		// Filtert nach der ersten IPv4-Adresse, die nicht die Loopback-Adresse (127.0.0.1) ist
		var ipAddress = host.AddressList
			.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

		return ipAddress?.ToString() ?? "127.0.0.1";
	}
}
