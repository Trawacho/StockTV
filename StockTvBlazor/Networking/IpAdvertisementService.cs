using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace StockTvBlazor.Networking;

public record AdvertisedIpInfo(IPAddress Address, string AddressString);

public class IpAdvertisementService
{
	private static AdvertisedIpInfo? _cachedInfo;

	public static AdvertisedIpInfo GetAdvertisedIp()
	{
		if (_cachedInfo != null)
			return _cachedInfo;

		var publicHost = Environment.GetEnvironmentVariable("PUBLIC_HOST");

		string ipString;
		if (!string.IsNullOrWhiteSpace(publicHost))
		{
			ipString = publicHost;
		}
		else
		{
			ipString = GetFirstLocalIPv4() ?? "127.0.0.1";
		}

		var address = IPAddress.Parse(ipString);
		_cachedInfo = new AdvertisedIpInfo(address, ipString);
		return _cachedInfo;
	}

	private static string? GetFirstLocalIPv4()
	{
		try
		{
			// Direkte Abfrage der Netzwerk-Interfaces statt DNS-Hostnamen-Aufloesung:
			// Dns.GetHostEntry(Dns.GetHostName()) haengt auf Debian/Raspberry Pi OS von /etc/hosts ab
			// (dort steht oft nur "127.0.1.1 <hostname>"), was je nach Netzwerk-Konfiguration
			// (DHCP vs. statische IP, dhcpcd vs. NetworkManager) zur Loopback-Adresse fuehren kann.
			var candidates = NetworkInterface.GetAllNetworkInterfaces()
				.Where(nic => nic.OperationalStatus == OperationalStatus.Up
					&& nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				.Select(nic => new
				{
					HasGateway = nic.GetIPProperties().GatewayAddresses
						.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork),
					Address = nic.GetIPProperties().UnicastAddresses
						.Select(ua => ua.Address)
						.FirstOrDefault(IsUsableIPv4)
				})
				.Where(x => x.Address != null)
				// Interfaces mit Standard-Gateway (echtes LAN/WLAN) vor Interfaces ohne
				// Gateway (z.B. Docker-Bridges, virtuelle Adapter) bevorzugen.
				.OrderByDescending(x => x.HasGateway)
				.ToList();

			return candidates.FirstOrDefault()?.Address?.ToString();
		}
		catch
		{
			return null;
		}
	}

	private static bool IsUsableIPv4(IPAddress address)
	{
		if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
			return false;

		// APIPA/Link-Local (169.254.x.x) ausschliessen, z.B. wenn DHCP fehlgeschlagen ist.
		var bytes = address.GetAddressBytes();
		return !(bytes[0] == 169 && bytes[1] == 254);
	}
}
