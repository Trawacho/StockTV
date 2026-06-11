using NetMQ;
using NetMQ.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Networking;

public class NetMqPublisherService : BackgroundService, IDisposable
{
	private readonly XPublisherSocket _pubSocket;

	private readonly NetMQPoller _poller;

	private readonly NetMQTimer _aliveTimer;

	private readonly Channel<(string Topic, object Payload)> _messageChannel;
	private readonly Channel<bool> _blockLocalChangesChannel;

	private string _serializedAliveInfo;
	private bool _aliveInfoResolved;

	private readonly ILogger<NetMqPublisherService> _logger;
	private readonly SettingsService settingsService;

	public NetMqPublisherService(ILogger<NetMqPublisherService> logger, SettingsService settingsService)
	{
		_logger = logger;
		this.settingsService = settingsService;
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
		_aliveInfoResolved = aliveInfo.IpAddress != "127.0.0.1";

		_logger.LogInformation(
			"Server AliveInfo: HostName={Host}, IP={Ip}, AppVersion={Version}",
			aliveInfo.HostName,
			aliveInfo.IpAddress,
			aliveInfo.AppVersion
		);

		_aliveTimer = new NetMQTimer(TimeSpan.FromSeconds(5));
		_aliveTimer.Elapsed += (s, e) =>
		{
			if (!_aliveInfoResolved)
			{
				var updated = AliveInfo.Create();
				if (updated.IpAddress != "127.0.0.1")
				{
					_aliveInfoResolved = true;
					_serializedAliveInfo = JsonSerializer.Serialize(updated);
					_logger.LogInformation(
						"AliveInfo aktualisiert: HostName={Host}, IP={Ip}",
						updated.HostName, updated.IpAddress);
				}
			}
			Publish("Alive", _serializedAliveInfo);
		};

		_poller = [_pubSocket, _aliveTimer];
		_messageChannel = Channel.CreateUnbounded<(string, object)>();
		_blockLocalChangesChannel = Channel.CreateUnbounded<bool>();

		_pubSocket.ReceiveReady += OnReceiveReady;
	}

	private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		var msg = e.Socket.ReceiveFrameBytes();

		if (msg.Length > 0)
		{
			bool isSubscribe = msg[0] == 1;
			_blockLocalChangesChannel.Writer.TryWrite(isSubscribe);
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

		var messageTask = HandleMessagesAsync(stoppingToken);
		var blockChangesTask = HandleBlockChangesAsync(stoppingToken);

		try
		{
			await Task.WhenAll(messageTask, blockChangesTask);
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("NetMqPublisherService durch CancellationToken gestoppt");
		}

		_poller.StopAsync();
		_logger.LogInformation("ExecuteAsync beendet");
	}

	private async Task HandleMessagesAsync(CancellationToken stoppingToken)
	{
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
		catch (OperationCanceledException) { }
	}

	private async Task HandleBlockChangesAsync(CancellationToken stoppingToken)
	{
		try
		{
			await foreach (var block in _blockLocalChangesChannel.Reader.ReadAllAsync(stoppingToken))
			{
				settingsService.ChangeBlockLocalChanges(block);
			}
		}
		catch (OperationCanceledException) { }
	}

	private bool _disposed = false;
	public override void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_logger.LogInformation("NetMqPublisherService Dispose gestartet");
		_messageChannel.Writer.TryComplete();
		_blockLocalChangesChannel.Writer.TryComplete();
		_pubSocket.ReceiveReady -= OnReceiveReady;
		if (_poller.IsRunning)
			_poller.StopAsync();
		_poller.Dispose();
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
		var advertisedIp = IpAdvertisementService.GetAdvertisedIp();
		string host = Dns.GetHostName();

		var alive = new AliveInfo
		{
			AppVersion = Assembly.GetExecutingAssembly()
								 .GetName()
								 .Version?.ToString() ?? "0.0.0.0",
			HostName = host,
			IpAddress = advertisedIp.AddressString
		};

		return alive;
	}
}
