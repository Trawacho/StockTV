using NetMQ;
using NetMQ.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;

namespace StockTvBlazor.Components.Networking;

public class NetMqPublisherService : BackgroundService, IDisposable
{
	private readonly XPublisherSocket _pubSocket; 
	private readonly NetMQPoller _poller;
	private readonly NetMQTimer _aliveTimer;
	private readonly Channel<(string Topic, object Payload)> _messageChannel;
	private readonly string _serializedAliveInfo;
	private readonly ILogger<NetMqPublisherService> _logger;

	public NetMqPublisherService(ILogger<NetMqPublisherService> logger)
	{
		_logger = logger;
		try
		{
			_pubSocket = new XPublisherSocket();
			_pubSocket.Bind("tcp://*:4748"); // Dein alter Port
			_pubSocket.Options.SendHighWatermark = 100;
			_logger.LogInformation("NetMqPublisherService initialized and bound to port 4748");
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to initialize NetMqPublisherService");
			throw;
		}

		_serializedAliveInfo = JsonSerializer.Serialize(AliveInfo.Create());
		_logger.LogDebug("AliveInfo: {AliveInfo}", _serializedAliveInfo);

		_aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
		_aliveTimer.Elapsed += (s, e) => Publish("Alive", _serializedAliveInfo);


		// Poller für Timer und Subscription-Erkennung
		_poller = [_pubSocket, _aliveTimer];
		_messageChannel = Channel.CreateUnbounded<(string, object)>();

		_pubSocket.ReceiveReady += OnReceiveReady;
	}

	private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		var msg = e.Socket.ReceiveFrameBytes();
		
		if (msg.Length > 0)
		{
			bool isSubscribe = msg[0] == 1;
			var topic = System.Text.Encoding.UTF8.GetString(msg, 1, msg.Length - 1);
			_logger.LogDebug("Client {Action} Topic: '{Topic}'",
				isSubscribe ? "abonniert" : "kündigt", topic);
		}

		//_pubSocket.SendMoreFrame(msg).SendFrame(msg);  <== StockApp kann mit der Message nicht umgehen
	}

	public void Publish(string topic, string payload)
	{
		bool written = _messageChannel.Writer.TryWrite((topic, payload));
		if (!written)
			_logger.LogWarning("Publish fehlgeschalgen. Topic: {Topic}", topic);
	}

	public void Publish(string topic, byte[] data)
	{
		bool written = _messageChannel.Writer.TryWrite((topic, data));
		if(!written)
			_logger.LogWarning("Publish fehlgeschlagen.Topic: {Topic}", topic);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("ExecuteAsync gestartet");
		_poller.RunAsync();

		try
		{
			await foreach (var (topic, payload) in _messageChannel.Reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					_pubSocket.SendMoreFrame(topic);
					if (payload is string text)
						_pubSocket.SendFrame(text);
					else if (payload is byte[] bytes)
						_pubSocket.SendFrame(bytes);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Fehler beim Senden. Topic: {Topic}", topic);
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Publisher wurde durch CancellationToken gestoppt");
		}

		_poller.Stop();
		_logger.LogInformation("ExecuteAsync beendet");
	}

	public override void Dispose()
	{
		_logger.LogInformation("dispose");
		Channel_complete();
		_pubSocket.ReceiveReady -= OnReceiveReady;
		_pubSocket.Close();
		_pubSocket.Dispose();
		NetMQConfig.Cleanup();
		base.Dispose();
	}

	private void Channel_complete()
	{
		try { _messageChannel.Writer.Complete(); } catch { }
	}
}


public class AliveInfo
{
	public string? IpAddress { get; private set; } 
	public string? HostName { get; private set; } 
	public string? AppVersion { get; private set; } 

	public static AliveInfo Create()
	{
		return new AliveInfo
		{
			AppVersion = Assembly.GetExecutingAssembly()
										 .GetName()
										 .Version?
										 .ToString() ?? "0.0.0.0",
			HostName =  Environment.MachineName,
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
