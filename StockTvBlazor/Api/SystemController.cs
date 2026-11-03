using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using StockTvBlazor.Services;

namespace StockTvBlazor.Api;

/// <summary>
/// Geraeteverwaltung fuer Raspberry Pis - REST-Ablösung von <c>GetNetworkConfig</c>,
/// <c>SetNetworkConfig</c>, <c>GetHostname</c>, <c>SetHostname</c> und <c>RebootPi</c>.
/// </summary>
/// <remarks>
/// Dieselben zwei Sperren wie im NetMQ-Pfad, nur mit HTTP-Statuscodes statt <c>NACK:</c>-Zeichenketten:
/// nur auf echten Raspberry Pis (sonst 412) und nur, solange keine Spieldaten hinterlegt sind
/// (sonst 409, <see cref="GameStateGuard"/>).
///
/// Anders als bei NetMQ blockiert das Auslesen hier keinen fremden Thread: der dortige
/// <c>GetNetworkConfig</c> ruft <c>sudo nmcli</c> synchron im Poller-Callback auf (bis zu 10s+10s
/// Timeout) und legt damit die gesamte Kommandoannahme still. Hier wird schlicht awaitet.
/// </remarks>
[ApiController]
[Route("api/v1/system")]
[Produces("application/json")]
public sealed class SystemController(
	PlatformInfoService platformInfo,
	NetworkConfigService networkConfig,
	MatchService matchService,
	ZielService zielService,
	GameCommandQueue commands,
	ILogger<SystemController> logger) : ControllerBase
{
	/// <summary>Der Hostname des Geraets.</summary>
	[HttpGet("hostname")]
	[ProducesResponseType<HostnameResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status412PreconditionFailed)]
	public ActionResult<HostnameResponse> GetHostname()
		=> NotAPi() ?? (ActionResult<HostnameResponse>)new HostnameResponse(networkConfig.GetHostname());

	/// <summary>Setzt den Hostnamen. Wirkt erst nach einem Neustart.</summary>
	[HttpPut("hostname")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status412PreconditionFailed)]
	public ActionResult<ActionResponse> SetHostname([FromBody] HostnameResponse? request)
	{
		if (Blocked() is { } blocked) return blocked;

		var hostname = (request?.Hostname ?? string.Empty).Trim();
		if (!NetworkConfigService.HostnameRegex.IsMatch(hostname))
			return BadRequest(new ActionResponse("InvalidRequest", "Ungueltiger Hostname.", null));

		logger.LogInformation("SetHostname ueber REST angefordert");

		commands.Enqueue(async () =>
		{
			var result = await networkConfig.SetHostnameAsync(hostname, CancellationToken.None);
			if (!result.Success)
				logger.LogWarning("SetHostname fehlgeschlagen: {Error}", result.ErrorMessage);
		});

		return Accepted(new ActionResponse("Ok", "Hostname wurde angenommen, wirkt nach dem Neustart.", null));
	}

	/// <summary>Die Netzwerk-Konfiguration aller verbundenen Schnittstellen.</summary>
	[HttpGet("network")]
	[ProducesResponseType<IReadOnlyList<NetworkInterfaceDto>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status412PreconditionFailed)]
	public async Task<ActionResult<IReadOnlyList<NetworkInterfaceDto>>> GetNetwork(CancellationToken ct)
	{
		if (NotAPi() is { } notAPi) return notAPi;

		var interfaces = await networkConfig.GetInterfacesAsync(ct);
		var result = new List<NetworkInterfaceDto>();

		foreach (var iface in interfaces.Where(i => i.ConnectionName is not null))
		{
			var details = await networkConfig.GetConnectionDetailsAsync(iface, ct);
			if (details is null)
				continue;

			result.Add(new NetworkInterfaceDto(
				iface.Device,
				details.IsDhcp ? "dhcp" : "static",
				details.IpAddress?.ToString(),
				details.Prefix,
				details.Gateway?.ToString(),
				[.. details.DnsServers.Select(d => d.ToString())]));
		}

		return result;
	}

	/// <summary>Setzt die Netzwerk-Konfiguration einer Schnittstelle.</summary>
	[HttpPut("network")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status412PreconditionFailed)]
	public ActionResult<ActionResponse> SetNetwork([FromBody] NetworkInterfaceDto? request)
	{
		if (Blocked() is { } blocked) return blocked;

		if (request is null || string.IsNullOrWhiteSpace(request.Device))
			return BadRequest(new ActionResponse("InvalidRequest", "Es wurde kein Geraet uebergeben.", null));

		var isDhcp = string.Equals(request.Mode, "dhcp", StringComparison.OrdinalIgnoreCase);
		if (!isDhcp && !string.Equals(request.Mode, "static", StringComparison.OrdinalIgnoreCase))
			return BadRequest(new ActionResponse("InvalidRequest", "mode muss 'dhcp' oder 'static' sein.", null));

		IPAddress? ip = null, gateway = null;
		List<IPAddress> dns = [];

		if (!isDhcp)
		{
			if (!TryParseIPv4(request.IpAddress, out ip) || request.Prefix is null or < 0 or > 32)
				return BadRequest(new ActionResponse("InvalidRequest", "ipAddress oder prefix ist ungueltig.", null));

			if (!TryParseIPv4(request.Gateway, out gateway))
				return BadRequest(new ActionResponse("InvalidRequest", "gateway ist ungueltig.", null));

			foreach (var entry in request.DnsServers ?? [])
			{
				if (!TryParseIPv4(entry, out var dnsIp))
					return BadRequest(new ActionResponse("InvalidRequest", $"dnsServers enthaelt '{entry}'.", null));

				dns.Add(dnsIp!);
			}

			if (dns.Count == 0)
				return BadRequest(new ActionResponse("InvalidRequest", "Mindestens ein DNS-Server ist noetig.", null));
		}

		logger.LogInformation("SetNetworkConfig ueber REST angefordert: {Device}", request.Device);

		commands.Enqueue(async () =>
		{
			var interfaces = await networkConfig.GetInterfacesAsync(CancellationToken.None);
			var iface = interfaces.FirstOrDefault(i => i.Device == request.Device);

			if (iface?.ConnectionName is null)
			{
				logger.LogWarning("SetNetworkConfig: Geraet '{Device}' nicht gefunden oder nicht verbunden", request.Device);
				return;
			}

			var result = isDhcp
				? await networkConfig.SetDhcpAsync(iface.ConnectionName, CancellationToken.None)
				: await networkConfig.SetStaticAsync(iface.ConnectionName, ip!, request.Prefix!.Value, gateway!, dns, CancellationToken.None);

			if (!result.Success)
				logger.LogWarning("SetNetworkConfig fehlgeschlagen: {Error}", result.ErrorMessage);
		});

		return Accepted(new ActionResponse("Ok", "Netzwerk-Konfiguration wurde angenommen.", null));
	}

	/// <summary>Startet das Geraet neu.</summary>
	[HttpPost("reboot")]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status202Accepted)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status409Conflict)]
	[ProducesResponseType<ActionResponse>(StatusCodes.Status412PreconditionFailed)]
	public ActionResult<ActionResponse> Reboot()
	{
		if (Blocked() is { } blocked) return blocked;

		logger.LogWarning("Neustart ueber REST angefordert");

		commands.Enqueue(async () =>
		{
			var result = await networkConfig.RebootAsync(CancellationToken.None);
			if (!result.Success)
				logger.LogWarning("Neustart fehlgeschlagen: {Error}", result.ErrorMessage);
		});

		return Accepted(new ActionResponse("Ok", "Neustart wurde angenommen.", null));
	}

	/// <summary>412, wenn das kein Raspberry Pi ist - entspricht <c>NACK:not-a-pi</c>.</summary>
	private ObjectResult? NotAPi() => platformInfo.IsRaspberryPi
		? null
		: StatusCode(StatusCodes.Status412PreconditionFailed, new ActionResponse(
			"NotAPi", "Diese Funktion gibt es nur auf einem Raspberry Pi.", null));

	/// <summary>
	/// Zusaetzlich 409, solange Spieldaten hinterlegt sind - entspricht <c>NACK:values-present</c>.
	/// </summary>
	private ObjectResult? Blocked()
	{
		if (NotAPi() is { } notAPi)
			return notAPi;

		if (GameStateGuard.HasRecordedValues(matchService, zielService))
		{
			return Conflict(new ActionResponse("ValuesPresent",
				"Es sind bereits Spielwerte hinterlegt. Erst zuruecksetzen (POST /api/v1/result/reset).", null));
		}

		return null;
	}

	private static bool TryParseIPv4(string? value, out IPAddress? address)
	{
		address = null;

		return !string.IsNullOrWhiteSpace(value)
			&& IPAddress.TryParse(value, out address)
			&& address.AddressFamily == AddressFamily.InterNetwork;
	}
}

public sealed record HostnameResponse(string Hostname);

/// <summary>Eine Netzwerk-Schnittstelle. <c>mode</c> ist <c>dhcp</c> oder <c>static</c>.</summary>
public sealed record NetworkInterfaceDto(
	string Device,
	string Mode,
	string? IpAddress,
	int? Prefix,
	string? Gateway,
	IReadOnlyList<string>? DnsServers);
