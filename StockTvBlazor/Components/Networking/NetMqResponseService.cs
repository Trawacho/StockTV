using NetMQ;
using NetMQ.Sockets;
using StockTvBlazor.Components.Services;
using System.Text;
using System.Threading.Channels;

namespace StockTvBlazor.Components.Networking;

public class NetMqResponseService : BackgroundService, IDisposable
{
	private readonly ResponseSocket _repSocket;
	private readonly NetMQPoller _poller;
	private readonly ILogger<NetMqResponseService> _logger;
	private readonly SettingsService _settingsService;
	private readonly MatchService _matchService;
	private readonly ZielService _zielService;
	private readonly Channel<Action> _actionChannel = Channel.CreateUnbounded<Action>();
	public NetMqResponseService(ILogger<NetMqResponseService> logger, SettingsService settingsService, MatchService matchService, ZielService zielService)
	{
		_logger = logger;
		_settingsService = settingsService;
		_matchService = matchService;
		_zielService = zielService;

		// 1. Socket initialisieren
		_repSocket = new ResponseSocket();

		// 2. Port auf 4747 festlegen (entspricht dem ctrSvc im mDNS)
		try
		{
			_repSocket.Bind("tcp://*:4747");
			_logger.LogInformation("NetMqResponseService successfully bound to port 4747");
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to bind NetMqResponseService to port 4747. Is the port already in use?");
			throw;
		}

		_repSocket.Options.Identity = Encoding.UTF8.GetBytes(Environment.MachineName + "-" + Guid.NewGuid().ToString());

		// 3. Event-Handler für eingehende Nachrichten registrieren
		_repSocket.ReceiveReady += OnReceiveReady;

		// 4. Poller mit dem Socket verknüpfen
		_poller = [_repSocket];

		_logger.LogInformation("NetMqResponseService initialized and listening on port 4747");
	}

	private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		try
		{
			// 1. Deklaration der Variable für die eingehende Multipart-Message
			NetMQMessage? requestMsg = null;

			// 2. Versuche die Nachricht zu Empfangen und requestMsg zu füllen
			if (e.Socket.TryReceiveMultipartMessage(ref requestMsg))
			{
				// _logger.LogInformation($"Multipart Request mit {requestMsg.FrameCount} Frames empfangen.");

				// 3. Logik verarbeiten
				NetMQMessage responseMsg = ProcessMultipartRequest(requestMsg);

				// 4. Die Antwort als Multipart-Message zurücksenden
				// Im REP-Muster muss IMMER eine Antwort (auch mehrteilig) folgen
				if (responseMsg.IsEmpty)
					responseMsg.Append("ACK");
				e.Socket.SendMultipartMessage(responseMsg);
			}
			else
			{
				_logger.LogWarning("Received a message, but failed to parse it as a multipart message.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler bei der NetMQ-Multipart-Verarbeitung auf Port 4747");
			try { e.Socket.SendFrame("ERROR"); } catch { }
		}
	}

	private NetMQMessage ProcessMultipartRequest(NetMQMessage request)
	{
		var response = new NetMQMessage();

		var topic = request[0].ConvertToString();

		switch (topic)
		{
			case "Hello":
				_logger.LogDebug("Hello topic received, sending welcome message.");
				response.Append("Welcome");
				break;

			case "GetResult":
				_logger.LogDebug("GetResult topic received, sending current match or ziel result.");
				response.Append("GetResult");
				if (_settingsService.CurrentSettings.Modus == Models.Settings.MODUS.ZIEL)
					response.Append(_zielService.CurrentZielBewerb.SerializeJson());
				else
					response.Append(_matchService.CurrentMatch.SerializeJson());

				break;

			case "ResetResult":
				_logger.LogDebug("ResetResult topic received, resetting current match or ziel.");
				if (!_actionChannel.Writer.TryWrite(() =>
				{
					if (_settingsService.CurrentSettings.Modus == Models.Settings.MODUS.ZIEL)
						_zielService.CurrentZielBewerb.Reset();
					else
						_matchService.CurrentMatch.Reset(true);
				}
				))
					_logger.LogWarning("Failed to write ResetResult action to channel.");
				response.Append("ACK");
				break;

			case "GetSettings":
				_logger.LogDebug("GetSettings topic received, sending current settings.");
				response.Append("GetSettings");
				response.Append(_settingsService.GetSettings());
				break;

			case "SetSettings":
				_logger.LogDebug("SetSettings topic received, updating settings.");
				if (!_actionChannel.Writer.TryWrite(() => _settingsService.SetSettings(request[1].ToByteArray())))
					_logger.LogWarning("Failed to write SetSettings action to channel.");
				response.Append("ACK");
				break;

			case "SetTeamNames":
				_logger.LogDebug("SetTeamNames topic received, updating team names.");
				if (!_actionChannel.Writer.TryWrite(() => _matchService.SetTeamNames(request[1].ToByteArray())))
					_logger.LogWarning("Failed to write SetTeamNames action to channel.");
				response.Append("ACK");
				break;

			case "SetTeilnehmer":
				_logger.LogDebug("SetTeilnehmer topic received, update SpielerName.");
				if (!_actionChannel.Writer.TryWrite(() => _zielService.SetTeilnehmer(request[1].ToByteArray())))
					_logger.LogWarning("Failed to write SetTeilnehmer action to channel.");
				response.Append("ACK");
				break;

			default:
				_logger.LogWarning("Unbekanntes Topic empfangen: {0}", topic);
				response.Append("unknown topic received");
				break;
		}


		return response;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("ExecuteAsync gestartet");
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
					_logger.LogError(ex, "Fehler beim Ausführen einer Channel-Aktion");
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("NetMqResponseService durch CancellationToken gestoppt");
		}

		if (_poller.IsRunning)
			_poller.Stop();

		_logger.LogInformation("ExecuteAsync beendet");
	}

	private bool _disposed = false;
	public override void Dispose()
	{
		if(_disposed) return;
		_disposed = true;

		_logger.LogInformation("NetMqResponseService dispose gestartet");
		if (_poller.IsRunning) _poller.Stop();

		try { _actionChannel.Writer.TryComplete(); } catch { }

		_poller.Dispose();
		_repSocket.Close();
		_repSocket.Dispose();
		_logger.LogInformation("NetMqResponseService dispose beendet");

		base.Dispose();
	}
}
