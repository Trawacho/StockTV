using Makaretu.Dns;
using System.Reflection;

namespace StockTvBlazor.Components.Networking;

public class MdnsDiscoveryService : BackgroundService
{
	private readonly ServiceDiscovery _serviceDiscovery;
	private readonly ServiceProfile _profile;

	public MdnsDiscoveryService()
	{
		// Initialisiere den Discovery-Dienst
		_serviceDiscovery = new ServiceDiscovery();

		// Profil erstellen: Rechnername, Service-Typ (TCP), Port 4747
		// Wichtig: Der Typ muss mit einem Punkt enden, damit er RFC-konform ist
		_profile = new ServiceProfile(Environment.MachineName, "_stockTV._tcp.", 4747);

		// Deine spezifischen Metadaten (TXT-Records) hinzufügen
		_profile.AddProperty("pubSvc", "4748");
		_profile.AddProperty("ctrSvc", "4747");
		_profile.AddProperty("pkgVer", GetAppVersion());
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Startet das Advertising im Netzwerk
		_serviceDiscovery.Advertise(_profile);

		// Da Advertise() im Hintergrund läuft, geben wir Task.CompletedTask zurück.
		// Der BackgroundService bleibt aktiv, bis die App gestoppt wird.
		return Task.CompletedTask;
	}

	public override void Dispose()
	{
		// Wichtig: Beim Beenden der App den Dienst im Netzwerk abmelden
		_serviceDiscovery.Unadvertise();
		_serviceDiscovery.Dispose();
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
