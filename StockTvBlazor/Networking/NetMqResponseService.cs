using NetMQ;
using NetMQ.Sockets;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;
using System.Text;
using System.Threading.Channels;

namespace StockTvBlazor.Networking;

public class NetMqResponseService : BackgroundService, IDisposable
{
	private readonly ResponseSocket _repSocket;

	private readonly NetMQPoller _poller;

	private readonly ILogger<NetMqResponseService> _logger;

	private readonly SettingsService _settingsService;

	private readonly MatchService _matchService;
	
	private readonly ZielService _zielService;

	private readonly Channel<Action> _actionChannel = Channel.CreateUnbounded<Action>();

	public NetMqResponseService(
		ILogger<NetMqResponseService> logger,
		SettingsService settingsService,
		MatchService matchService,
		ZielService zielService)
	{
		_logger = logger;
		_settingsService = settingsService;
		_matchService = matchService;
		_zielService = zielService;

		_repSocket = new ResponseSocket();

		try
		{
			_repSocket.Bind("tcp://*:4747");
			_logger.LogInformation("NetMQ bound on port 4747");
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to bind port 4747");
			throw;
		}

		_repSocket.Options.Identity =
			Encoding.UTF8.GetBytes($"{Environment.MachineName}-{Guid.NewGuid()}");

		_repSocket.ReceiveReady += OnReceiveReady;

		_poller = new NetMQPoller { _repSocket };
	}

	private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		try
		{
			NetMQMessage? request = null;

			if (!e.Socket.TryReceiveMultipartMessage(ref request))
			{
				_logger.LogWarning("Invalid multipart message received");
				return;
			}

			var response = Process(request);

			if (response.IsEmpty)
				response.Append("ACK");

			e.Socket.SendMultipartMessage(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "NetMQ processing error");
			try 
			{
				e.Socket.SendFrame("ERROR");
			} 
			catch (Exception sendEx) 
			{ 
				_logger.LogWarning(sendEx, "Failed to send ERROR frame to client"); 
			}
		}
	}

	private NetMQMessage Process(NetMQMessage request)
	{
		var response = new NetMQMessage();
		var topic = request[0].ConvertToString();

		_logger.LogDebug("NetMQ command received: {Topic}", topic);

		switch (topic)
		{
			case "Hello":
				response.Append("Welcome");
				break;

			case "GetResult":
				response.Append("GetResult");

				if (_settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel)
					response.Append(_zielService.CurrentZielBewerb.SerializeJson());
				else
					response.Append(_matchService.CurrentMatch.SerializeJson());
				break;

			case "ResetResult":
				_logger.LogInformation("ResetResult requested");
				_ = _actionChannel.Writer.TryWrite(() =>
				{
					if (_settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel)
						_zielService.CurrentZielBewerb.Reset();
					else
						_matchService.CurrentMatch.Reset(true);
				});

				response.Append("ACK");
				break;

			case "GetSettings":
				response.Append("GetSettings");
				response.Append(_settingsService.GetSettings());
				break;

			case "SetSettings":
				_logger.LogInformation("SetSettings requested");
				_ = _actionChannel.Writer.TryWrite(() =>
					_settingsService.SetSettings(request[1].ToByteArray()));

				response.Append("ACK");
				break;

			case "SetTeamNames":
				_logger.LogInformation("SetTeamNames requested");
				_ = _actionChannel.Writer.TryWrite(() =>
					_matchService.SetTeamNames(request[1].ToByteArray()));

				response.Append("ACK");
				break;

			case "SetTeilnehmer":
				_logger.LogInformation("SetTeilnehmer requested");
				_ = _actionChannel.Writer.TryWrite(() =>
					_zielService.SetTeilnehmer(request[1].ToByteArray()));

				response.Append("ACK");
				break;

			default:
				_logger.LogWarning("Unknown topic: {Topic}", topic);
				response.Append("unknown topic");
				break;
		}

		return response;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("NetMQ service started");

		_poller.RunAsync();

		try
		{
			await foreach (var action in _actionChannel.Reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Channel action error");
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("NetMQ action channel stopped by cancellation");
		}

		if (_poller.IsRunning)
			_poller.StopAsync();

		_logger.LogInformation("NetMQ service stopped");
	}

	private bool _disposed;

	public override void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_logger.LogInformation("Disposing NetMQ service");

		_actionChannel.Writer.TryComplete();
		if (_poller.IsRunning)
			_poller.StopAsync();
		_poller.Dispose();

		base.Dispose();
	}
}
