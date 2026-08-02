using NetMQ;
using NetMQ.Sockets;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;
using System.Net;
using System.Net.Sockets;
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

	private readonly PlatformInfoService _platformInfo;

	private readonly NetworkConfigService _networkConfigService;

	private readonly Channel<Func<Task>> _actionChannel = Channel.CreateUnbounded<Func<Task>>();

	public NetMqResponseService(
		ILogger<NetMqResponseService> logger,
		SettingsService settingsService,
		MatchService matchService,
		ZielService zielService,
		PlatformInfoService platformInfo,
		NetworkConfigService networkConfigService)
	{
		_logger = logger;
		_settingsService = settingsService;
		_matchService = matchService;
		_zielService = zielService;
		_platformInfo = platformInfo;
		_networkConfigService = networkConfigService;

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

				if (_settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel
					|| _settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel2)
					response.Append(_zielService.CurrentZielBewerb.SerializeJson());
				else
					response.Append(_matchService.CurrentMatch.SerializeJson());
				break;

			case "ResetResult":
				_logger.LogInformation("ResetResult requested");
				_ = _actionChannel.Writer.TryWrite(() =>
				{
					if (_settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel
						|| _settingsService.CurrentSettings.Game.CurrentModus == GameSettings.Modus.Ziel2)
						_zielService.CurrentZielBewerb.Reset();
					else
						_matchService.CurrentMatch.Reset(true);
					return Task.CompletedTask;
				});

				response.Append("ACK");
				break;

			case "GetSettings":
				response.Append("GetSettings");
				response.Append(_settingsService.GetSettings());
				break;

			case "GetHostname":
				if (!_platformInfo.IsRaspberryPi)
				{
					response.Append("NACK:not-a-pi");
					break;
				}

				response.Append("GetHostname");
				response.Append(_networkConfigService.GetHostname());
				break;

			case "GetNetworkConfig":
			{
				if (!_platformInfo.IsRaspberryPi)
				{
					response.Append("NACK:not-a-pi");
					break;
				}

				var interfaces = _networkConfigService.GetInterfacesAsync(CancellationToken.None).GetAwaiter().GetResult();
				var entries = new List<string>();

				foreach (var iface in interfaces.Where(i => i.ConnectionName is not null))
				{
					var details = _networkConfigService.GetConnectionDetailsAsync(iface, CancellationToken.None).GetAwaiter().GetResult();
					if (details is null)
						continue;

					var cidr = details.IpAddress is not null ? $"{details.IpAddress}/{details.Prefix}" : "";
					var mode = details.IsDhcp ? "dhcp" : "static";
					entries.Add($"{iface.Device}:{mode}:{cidr}:{details.Gateway}:{string.Join(',', details.DnsServers)}");
				}

				response.Append("GetNetworkConfig");
				response.Append(string.Join(';', entries));
				break;
			}

			case "SetSettings":
				_logger.LogInformation("SetSettings requested");
				_ = _actionChannel.Writer.TryWrite(() =>
				{
					_settingsService.SetSettings(request[1].ToByteArray());
					return Task.CompletedTask;
				});

				response.Append("ACK");
				break;

			case "SetTeamNames":
				_logger.LogInformation("SetTeamNames requested");
				_ = _actionChannel.Writer.TryWrite(() =>
				{
					_matchService.SetTeamNames(request[1].ToByteArray());
					return Task.CompletedTask;
				});

				response.Append("ACK");
				break;

			case "SetTeilnehmer":
				_logger.LogInformation("SetTeilnehmer requested");
				_ = _actionChannel.Writer.TryWrite(() =>
				{
					_zielService.SetTeilnehmer(request[1].ToByteArray());
					return Task.CompletedTask;
				});

				response.Append("ACK");
				break;

			case "SetNetworkConfig":
			{
				_logger.LogInformation("SetNetworkConfig requested");

				if (!_platformInfo.IsRaspberryPi) { response.Append("NACK:not-a-pi"); break; }
				if (GameStateGuard.HasRecordedValues(_matchService, _zielService)) { response.Append("NACK:values-present"); break; }
				if (request.FrameCount < 2) { response.Append("NACK:invalid-payload"); break; }

				var payload = Encoding.UTF8.GetString(request[1].ToByteArray());
				if (!TryParseNetworkConfigPayload(payload, out var cmd, out var parseError))
				{
					response.Append($"NACK:{parseError}");
					break;
				}

				_ = _actionChannel.Writer.TryWrite(async () =>
				{
					var netInterfaces = await _networkConfigService.GetInterfacesAsync(CancellationToken.None);
					var iface = netInterfaces.FirstOrDefault(i => i.Device == cmd!.Device);
					if (iface?.ConnectionName is null)
					{
						_logger.LogWarning("SetNetworkConfig: Device '{Device}' nicht gefunden oder nicht verbunden", cmd!.Device);
						return;
					}

					var result = cmd!.IsDhcp
						? await _networkConfigService.SetDhcpAsync(iface.ConnectionName, CancellationToken.None)
						: await _networkConfigService.SetStaticAsync(iface.ConnectionName, cmd.Ip!, cmd.Prefix, cmd.Gateway!, cmd.DnsServers!, CancellationToken.None);

					if (!result.Success)
						_logger.LogWarning("SetNetworkConfig fehlgeschlagen: {Error}", result.ErrorMessage);
				});

				response.Append("ACK");
				break;
			}

			case "SetHostname":
			{
				_logger.LogInformation("SetHostname requested");

				if (!_platformInfo.IsRaspberryPi) { response.Append("NACK:not-a-pi"); break; }
				if (GameStateGuard.HasRecordedValues(_matchService, _zielService)) { response.Append("NACK:values-present"); break; }
				if (request.FrameCount < 2) { response.Append("NACK:invalid-payload"); break; }

				var hostname = Encoding.UTF8.GetString(request[1].ToByteArray()).Trim();
				if (!NetworkConfigService.HostnameRegex.IsMatch(hostname)) { response.Append("NACK:invalid-hostname"); break; }

				_ = _actionChannel.Writer.TryWrite(async () =>
				{
					var result = await _networkConfigService.SetHostnameAsync(hostname, CancellationToken.None);
					if (!result.Success)
						_logger.LogWarning("SetHostname fehlgeschlagen: {Error}", result.ErrorMessage);
				});

				response.Append("ACK");
				break;
			}

			case "RebootPi":
			{
				_logger.LogInformation("RebootPi requested");

				if (!_platformInfo.IsRaspberryPi) { response.Append("NACK:not-a-pi"); break; }
				if (GameStateGuard.HasRecordedValues(_matchService, _zielService)) { response.Append("NACK:values-present"); break; }

				_ = _actionChannel.Writer.TryWrite(async () =>
				{
					var result = await _networkConfigService.RebootAsync(CancellationToken.None);
					if (!result.Success)
						_logger.LogWarning("RebootPi fehlgeschlagen: {Error}", result.ErrorMessage);
				});

				response.Append("ACK");
				break;
			}

			case "SetImage":
				_logger.LogInformation("SetImage requested");
				// TODO: Implement SetImage functionality
				response.Append("ACK");
				break;

			case "GoToImage":
				_logger.LogInformation("GoToImage requested");
				// TODO: Implement GoToImage functionality
				response.Append("ACK");
				break;

			case "ClearImage":
				_logger.LogInformation("ClearImage requested");
				// TODO: Implement ClearImage functionality
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
					await action();
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

	private sealed record ParsedNetworkConfig(
		string Device, bool IsDhcp, IPAddress? Ip, int Prefix, IPAddress? Gateway, IReadOnlyList<IPAddress>? DnsServers);

	// Payload: "<device>:<mode>:<cidr>:<gateway>:<dnsServers>" - siehe CLAUDE.md fuer das genaue
	// Format. Bewusst kein Code-Sharing mit SysUpdatePage.razor.cs's TryValidateStaticInput, da dort
	// auf Formularfeldern statt einem String-Payload validiert wird.
	private static bool TryParseNetworkConfigPayload(string payload, out ParsedNetworkConfig? cmd, out string? error)
	{
		cmd = null;

		var fields = payload.Split(':');
		if (fields.Length != 5 || string.IsNullOrWhiteSpace(fields[0]))
		{
			error = "invalid-payload";
			return false;
		}

		var device = fields[0];
		var modeText = fields[1].Trim().ToLowerInvariant();

		if (modeText != "dhcp" && modeText != "static")
		{
			error = "invalid-mode";
			return false;
		}

		if (modeText == "dhcp")
		{
			error = null;
			cmd = new ParsedNetworkConfig(device, true, null, 0, null, null);
			return true;
		}

		var cidrParts = fields[2].Split('/', 2);
		if (cidrParts.Length != 2
			|| !IPAddress.TryParse(cidrParts[0], out var ip) || ip.AddressFamily != AddressFamily.InterNetwork
			|| !int.TryParse(cidrParts[1], out var prefix) || prefix < 0 || prefix > 32)
		{
			error = "invalid-ip";
			return false;
		}

		if (!IPAddress.TryParse(fields[3], out var gateway) || gateway.AddressFamily != AddressFamily.InterNetwork)
		{
			error = "invalid-gateway";
			return false;
		}

		var dnsEntries = fields[4].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (dnsEntries.Length == 0)
		{
			error = "invalid-dns";
			return false;
		}

		var dnsServers = new List<IPAddress>();
		foreach (var entry in dnsEntries)
		{
			if (!IPAddress.TryParse(entry, out var dnsIp) || dnsIp.AddressFamily != AddressFamily.InterNetwork)
			{
				error = "invalid-dns";
				return false;
			}
			dnsServers.Add(dnsIp);
		}

		error = null;
		cmd = new ParsedNetworkConfig(device, false, ip, prefix, gateway, dnsServers);
		return true;
	}
}
