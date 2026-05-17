using NetMQ;
using NetMQ.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;

namespace StockTvBlazor.Networking;

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
			_pubSocket.Bind("tcp://*:4748"); // Port bleibt unverändert
			_pubSocket.Options.SendHighWatermark = 100;

			_logger.LogInformation("NetMqPublisherService initialized and bound to port 4748");
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to initialize NetMqPublisherService");
			throw;
		}

		// --- AliveInfo erstellen ---
		var aliveInfo = AliveInfo.Create();
		_serializedAliveInfo = JsonSerializer.Serialize(aliveInfo);

		// --- DIREKT LOGGEN ---
		_logger.LogInformation(
			"Server AliveInfo: HostName={Host}, IP={Ip}, AppVersion={Version}",
			aliveInfo.HostName,
			aliveInfo.IpAddress,
			aliveInfo.AppVersion
		);

		_aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
		_aliveTimer.Elapsed += (s, e) => Publish("Alive", _serializedAliveInfo);

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
	}

	public void Publish(string topic, string payload)
	{
		if (!_messageChannel.Writer.TryWrite((topic, payload)))
			_logger.LogWarning("Publish fehlgeschlagen. Topic: {Topic}", topic);
	}

	public void Publish(string topic, byte[] data)
	{
		if (!_messageChannel.Writer.TryWrite((topic, data)))
			_logger.LogWarning("Publish fehlgeschlagen. Topic: {Topic}", topic);
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
			_logger.LogInformation("NetMqPublisherService durch CancellationToken gestoppt");
		}

		_poller.Stop();
		_logger.LogInformation("ExecuteAsync beendet");
	}

	private bool _disposed = false;
	public override void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_logger.LogInformation("NetMqPublisherService Dispose gestartet");
		try { _messageChannel.Writer.TryComplete(); } catch { }
		_pubSocket.ReceiveReady -= OnReceiveReady;
		_pubSocket.Close();
		_pubSocket.Dispose();
		_logger.LogInformation("NetMqPublisherService dispose beendet");

		base.Dispose();
	}
}

public class AliveInfo
{
	public string? IpAddress { get; private set; }
	public string? HostName { get; private set; }
	public string? AppVersion { get; private set; }

	public static AliveInfo Create()
	{
		var publicHost = Environment.GetEnvironmentVariable("PUBLIC_HOST");

		// Priorität ENV > Hostname
		string host = !string.IsNullOrWhiteSpace(publicHost)
			? publicHost
			: Dns.GetHostName();

		// Optional: echte IPv4 nur, wenn ENV nicht gesetzt
		string ip = !string.IsNullOrWhiteSpace(publicHost)
			? publicHost
			: GetFirstLocalIPv4() ?? "127.0.0.1";

		var alive = new AliveInfo
		{
			AppVersion = Assembly.GetExecutingAssembly()
								 .GetName()
								 .Version?.ToString() ?? "0.0.0.0",
			HostName = host,
			IpAddress = ip
		};

		return alive;
	}

	private static string? GetFirstLocalIPv4()
	{
		try
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			var ipAddress = host.AddressList
				.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
			return ipAddress?.ToString();
		}
		catch
		{
			return null;
		}
	}
}
