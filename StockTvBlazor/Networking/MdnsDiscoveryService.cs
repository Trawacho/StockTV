using Makaretu.Dns;
using StockTvBlazor.Services;
using System.Reflection;

namespace StockTvBlazor.Networking;

public class MdnsDiscoveryService : BackgroundService
{
	private readonly PlatformInfoService _platformInfo;

	private readonly ILogger<MdnsDiscoveryService> _logger;

	private ServiceDiscovery? _serviceDiscovery;

	public MdnsDiscoveryService(PlatformInfoService platformInfo, ILogger<MdnsDiscoveryService> logger)
	{
		_platformInfo = platformInfo;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Boot-Race-Vermeidung: War beim Erstellen von ServiceDiscovery/MulticastService das
		// Netzwerk-Interface noch ohne IP (nur "up", aber ohne Adresse - typisch kurz nach dem
		// Boot), tritt es der mDNS-Multicast-Gruppe (224.0.0.251) nie bei. Die eingebaute
		// periodische Neuerkennung (Standard: alle 2 Minuten) behebt das nicht, da das Interface
		// selbst schon als "bekannt" galt - nur der Gruppenbeitritt schlug fehl. Ein nachtraeglicher
		// Stop()/Start() von MulticastService wurde auf einem Pi4 getestet und behebt zwar den
		// Gruppenbeitritt, doppelt aber die Socket-Bindings und beantwortet danach trotzdem keine
		// Anfragen mehr - deshalb hier der robustere Weg: ServiceDiscovery wird erst erstellt und
		// gestartet, wenn eine echte IP feststeht (derselbe Realitaets-Check, den auch
		// IpAdvertisementService fuer den NetMQ-Alive-Broadcast nutzt).
		AdvertisedIpInfo advertisedIp;
		try
		{
			advertisedIp = await WaitForRealIpAsync(stoppingToken);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		_serviceDiscovery = new ServiceDiscovery();

		var profile = new ServiceProfile(
			Environment.MachineName,
			"_stockTV._tcp.",
			4747,
			new[] { advertisedIp.Address }
		);

		profile.AddProperty("pubSvc", "4748");
		profile.AddProperty("ctrSvc", "4747");
		profile.AddProperty("pkgVer", GetAppVersion());
		profile.AddProperty("osVer", _platformInfo.OsVersion);

		try
		{
			_serviceDiscovery.Advertise(profile);
			_logger.LogInformation("mDNS-Advertising gestartet: Instance={Instance}, IP={Ip}",
				Environment.MachineName, advertisedIp.AddressString);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "mDNS-Advertising konnte nicht gestartet werden");
		}
	}

	private static async Task<AdvertisedIpInfo> WaitForRealIpAsync(CancellationToken stoppingToken)
	{
		while (true)
		{
			var info = IpAdvertisementService.GetAdvertisedIp();
			if (info.AddressString != "127.0.0.1")
				return info;

			await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
		}
	}

	public override void Dispose()
	{
		if (_serviceDiscovery is not null)
		{
			// Wichtig: Beim Beenden der App den Dienst im Netzwerk abmelden
			_serviceDiscovery.Unadvertise();
			_serviceDiscovery.Dispose();
			_logger.LogInformation("mDNS-Advertising beendet und abgemeldet");
		}

		base.Dispose();
	}

	private static string GetAppVersion()
	{
		// Liest die Version aus der Projektdatei (csproj -> Version)
		var version = Assembly.GetExecutingAssembly().GetName().Version;
		return version != null
			? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
			: "1.0.0.0";
	}
}
