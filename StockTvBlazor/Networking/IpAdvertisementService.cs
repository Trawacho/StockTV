using System.Net;
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
