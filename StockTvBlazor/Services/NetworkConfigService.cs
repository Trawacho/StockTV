using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Services;

public record NetworkInterfaceInfo(string Device, string Type, string State, string? ConnectionName);

public record NetworkConnectionDetails(
	string ConnectionName,
	bool IsDhcp,
	string? IpAddress,
	int? Prefix,
	string? Gateway,
	IReadOnlyList<string> DnsServers);

public record NetworkOperationResult(bool Success, string? ErrorMessage);

/// <summary>
/// Liest/schreibt die Netzwerkkonfiguration und den Hostnamen auf einem Raspberry Pi. Netzwerk-
/// Aufrufe laufen über "sudo -n /usr/bin/nmcli ..." (Cmnd_Alias STOCKTV_NM_READ/STOCKTV_NM_WRITE),
/// der Hostname über das feste Skript /usr/local/sbin/stocktv-set-hostname.sh (Cmnd_Alias
/// STOCKTV_HOSTNAME), beide freigegeben in der sudoers-Regel aus build/rpi/install.sh. Wird die
/// Argument-Reihenfolge/der Skriptpfad hier geändert, muss install.sh entsprechend angepasst werden,
/// sonst schlagen die Aufrufe mit "sudo: no command matches" fehl.
/// </summary>
public class NetworkConfigService
{
	// RFC-1123-Hostname-Label: Buchstaben/Ziffern, Bindestrich erlaubt, nicht am Anfang/Ende, 1-63 Zeichen.
	// Gemeinsam von Web-UI (SysUpdatePage) und NetMQ-Pfad (NetMqResponseService) verwendet.
	public static readonly Regex HostnameRegex = new(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$", RegexOptions.Compiled);

	private static readonly string[] AllowedInterfaceTypes = ["ethernet", "wifi"];

	private readonly ILogger<NetworkConfigService> _logger;

	public NetworkConfigService(ILogger<NetworkConfigService> logger)
	{
		_logger = logger;
	}

	public Task<bool> IsNmcliAvailableAsync(CancellationToken ct = default)
		=> Task.FromResult(File.Exists("/usr/bin/nmcli"));

	// Environment.MachineName liest den aktuellen Kernel-Hostnamen (gethostname()) ohne Root-Rechte -
	// dieselbe Quelle, die auch MdnsDiscoveryService fuer den mDNS-Instanznamen verwendet.
	public string GetHostname() => Environment.MachineName;

	// Setzt den Hostnamen NICHT direkt per nmcli, sondern ueber das von install.sh ausgelieferte
	// Skript /usr/local/sbin/stocktv-set-hostname.sh: "nmcli general hostname" alleine aktualisiert
	// zwar den Kernel-/persistenten Hostnamen, laesst aber die "127.0.1.1 <hostname>"-Zeile in
	// /etc/hosts unangetastet - das fuehrt zu "sudo: unable to resolve host ..."-Meldungen bei jedem
	// sudo-Aufruf, sobald beide auseinanderlaufen. Das Skript aktualisiert beides atomar.
	private const string SetHostnameScriptPath = "/usr/local/sbin/stocktv-set-hostname.sh";

	public async Task<NetworkOperationResult> SetHostnameAsync(string hostname, CancellationToken ct = default)
	{
		var (exitCode, _, stdErr) = await RunSudoAsync(
			SetHostnameScriptPath, [hostname], TimeSpan.FromSeconds(10), ct);

		return exitCode == 0
			? new NetworkOperationResult(true, null)
			: new NetworkOperationResult(false, stdErr);
	}

	// Anders als beim Update (StartUpdateAsync) braucht der Reboot keine systemd-run-Entkopplung:
	// "systemctl reboot" liefert seine D-Bus-Antwort zurueck, bevor der eigentliche Shutdown beginnt -
	// der Aufruf selbst wird nicht durch das eigene Herunterfahren abgebrochen.
	public async Task<NetworkOperationResult> RebootAsync(CancellationToken ct = default)
	{
		var (exitCode, _, stdErr) = await RunSudoAsync(
			"/usr/bin/systemctl", ["reboot"], TimeSpan.FromSeconds(10), ct);

		return exitCode == 0
			? new NetworkOperationResult(true, null)
			: new NetworkOperationResult(false, stdErr);
	}

	public async Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken ct = default)
	{
		var (exitCode, stdOut, stdErr) = await RunSudoNmcliAsync(
			["-t", "-f", "DEVICE,TYPE,STATE,CONNECTION", "device", "status"],
			TimeSpan.FromSeconds(10), ct);

		if (exitCode != 0)
		{
			_logger.LogWarning("nmcli device status fehlgeschlagen: {StdErr}", stdErr);
			return [];
		}

		var result = new List<NetworkInterfaceInfo>();
		foreach (var line in stdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = SplitTerse(line);
			if (parts.Count < 4)
				continue;

			var device = parts[0];
			var type = parts[1];
			var state = parts[2];
			var connection = parts[3];

			// Allowlist statt Blockliste: nur ethernet/wifi werden aufgenommen. Das schließt lo
			// (TYPE=loopback), Docker-/Bridge-/tun-/p2p-wifi-Interfaces automatisch aus.
			// "lo" zusätzlich hart ausschliessen, falls ein zukuenftiges nmcli den TYPE abweichend befuellt.
			if (device == "lo" || !AllowedInterfaceTypes.Contains(type))
				continue;

			result.Add(new NetworkInterfaceInfo(device, type, state, connection == "--" ? null : connection));
		}

		return result;
	}

	public async Task<NetworkConnectionDetails?> GetConnectionDetailsAsync(NetworkInterfaceInfo iface, CancellationToken ct = default)
	{
		if (string.IsNullOrEmpty(iface.ConnectionName))
			return null;

		var (methodExit, methodOut, methodErr) = await RunSudoNmcliAsync(
			["-t", "-f", "ipv4.method", "con", "show", iface.ConnectionName],
			TimeSpan.FromSeconds(10), ct);

		if (methodExit != 0)
		{
			_logger.LogWarning("nmcli con show fehlgeschlagen: {StdErr}", methodErr);
			return null;
		}

		var methodValues = ParseKeyValueDump(methodOut.Split('\n', StringSplitOptions.RemoveEmptyEntries));
		var method = methodValues.TryGetValue("ipv4.method", out var methodList) ? methodList.FirstOrDefault() : null;
		var isDhcp = method != "manual";

		var (deviceExit, deviceOut, deviceErr) = await RunSudoNmcliAsync(
			["-t", "-f", "IP4.ADDRESS,IP4.GATEWAY,IP4.DNS", "device", "show", iface.Device],
			TimeSpan.FromSeconds(10), ct);

		if (deviceExit != 0)
		{
			_logger.LogWarning("nmcli device show fehlgeschlagen: {StdErr}", deviceErr);
			return new NetworkConnectionDetails(iface.ConnectionName, isDhcp, null, null, null, []);
		}

		var deviceValues = ParseKeyValueDump(deviceOut.Split('\n', StringSplitOptions.RemoveEmptyEntries));

		string? ip = null;
		int? prefix = null;
		if (deviceValues.TryGetValue("IP4.ADDRESS", out var addressList) && addressList.Count > 0)
		{
			var addressParts = addressList[0].Split('/', 2);
			ip = addressParts[0];
			if (addressParts.Length == 2 && int.TryParse(addressParts[1], out var parsedPrefix))
				prefix = parsedPrefix;
		}

		var gateway = deviceValues.TryGetValue("IP4.GATEWAY", out var gatewayList) ? gatewayList.FirstOrDefault() : null;
		var dnsServers = deviceValues.TryGetValue("IP4.DNS", out var dnsList) ? dnsList : [];

		return new NetworkConnectionDetails(iface.ConnectionName, isDhcp, ip, prefix, gateway, dnsServers);
	}

	public async Task<NetworkOperationResult> SetStaticAsync(
		string connectionName, IPAddress ip, int prefix, IPAddress gateway, IReadOnlyList<IPAddress> dnsServers, CancellationToken ct = default)
	{
		var dnsArg = string.Join(",", dnsServers.Select(d => d.ToString()));

		var (modExit, _, modErr) = await RunSudoNmcliAsync(
			["con", "mod", connectionName,
				"ipv4.method", "manual",
				"ipv4.addresses", $"{ip}/{prefix}",
				"ipv4.gateway", gateway.ToString(),
				"ipv4.dns", dnsArg],
			TimeSpan.FromSeconds(10), ct);

		if (modExit != 0)
			return new NetworkOperationResult(false, modErr);

		return await ConnectionUpAsync(connectionName, ct);
	}

	public async Task<NetworkOperationResult> SetDhcpAsync(string connectionName, CancellationToken ct = default)
	{
		// ipv4.addresses/gateway/dns bewusst NICHT explizit leeren: NetworkManager wertet diese
		// Werte ohnehin nur bei ipv4.method=manual aus. Ein leeres Argument ("") liesse sich in der
		// sudoers-Regel nicht zuverlaessig matchen (sudo baut den Vergleichs-String aus dem argv
		// zusammen, ein leeres Element ergibt dort ein doppeltes Leerzeichen statt der literalen
		// Zeichen "" aus dem sudoers-Muster) - das fuehrte zu "sudo: a password is required".
		var (modExit, _, modErr) = await RunSudoNmcliAsync(
			["con", "mod", connectionName, "ipv4.method", "auto"],
			TimeSpan.FromSeconds(10), ct);

		if (modExit != 0)
			return new NetworkOperationResult(false, modErr);

		return await ConnectionUpAsync(connectionName, ct);
	}

	private async Task<NetworkOperationResult> ConnectionUpAsync(string connectionName, CancellationToken ct)
	{
		var (upExit, _, upErr) = await RunSudoNmcliAsync(
			["con", "up", connectionName],
			TimeSpan.FromSeconds(30), ct);

		return upExit == 0
			? new NetworkOperationResult(true, null)
			: new NetworkOperationResult(false, upErr);
	}

	private Task<(int ExitCode, string StdOut, string StdErr)> RunSudoNmcliAsync(
		IReadOnlyList<string> nmcliArgs, TimeSpan timeout, CancellationToken ct)
		=> RunSudoAsync("/usr/bin/nmcli", nmcliArgs, timeout, ct);

	private async Task<(int ExitCode, string StdOut, string StdErr)> RunSudoAsync(
		string executablePath, IReadOnlyList<string> args, TimeSpan timeout, CancellationToken ct)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "sudo",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		psi.ArgumentList.Add("-n");
		psi.ArgumentList.Add(executablePath);
		foreach (var arg in args)
			psi.ArgumentList.Add(arg);

		using var process = new Process { StartInfo = psi };

		try
		{
			process.Start();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "sudo {ExecutablePath} konnte nicht gestartet werden", executablePath);
			return (-1, "", ex.Message);
		}

		var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
		var stdErrTask = process.StandardError.ReadToEndAsync(ct);

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(timeout);

		try
		{
			await process.WaitForExitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			try { process.Kill(entireProcessTree: true); } catch { /* Prozess evtl. bereits beendet */ }
			_logger.LogWarning("sudo {ExecutablePath} abgebrochen (Zeitueberschreitung nach {Timeout})", executablePath, timeout);
			return (-1, "", "Zeitüberschreitung beim Ausführen des Befehls.");
		}

		var stdOut = await stdOutTask;
		var stdErr = await stdErrTask;
		return (process.ExitCode, stdOut, stdErr);
	}

	/// <summary>
	/// Trennt eine nmcli -t (terse) Zeile an unescaped ':'. nmcli escaped ':' und '\' innerhalb von
	/// Werten als '\:' bzw. '\\' - ein naives Split(':') wuerde solche Werte falsch aufteilen.
	/// </summary>
	private static List<string> SplitTerse(string line)
	{
		var result = new List<string>();
		var sb = new StringBuilder();

		for (var i = 0; i < line.Length; i++)
		{
			if (line[i] == '\\' && i + 1 < line.Length && (line[i + 1] == ':' || line[i + 1] == '\\'))
			{
				sb.Append(line[i + 1]);
				i++;
			}
			else if (line[i] == ':')
			{
				result.Add(sb.ToString());
				sb.Clear();
			}
			else
			{
				sb.Append(line[i]);
			}
		}

		result.Add(sb.ToString());
		return result;
	}

	/// <summary>
	/// Parst "KEY:VALUE"-Zeilen (z.B. aus "nmcli con show"/"device show"). Mehrwertige Keys wie
	/// "IP4.DNS[1]"/"IP4.DNS[2]" werden auf ihren Basis-Key ("IP4.DNS") zusammengefuehrt.
	/// </summary>
	private static Dictionary<string, List<string>> ParseKeyValueDump(IEnumerable<string> lines)
	{
		var result = new Dictionary<string, List<string>>();

		foreach (var line in lines)
		{
			if (string.IsNullOrEmpty(line))
				continue;

			var parts = SplitTerse(line);
			if (parts.Count < 1)
				continue;

			var baseKey = StripIndexSuffix(parts[0]);
			var value = parts.Count > 1 ? string.Join(":", parts.Skip(1)) : "";

			if (!result.TryGetValue(baseKey, out var list))
			{
				list = [];
				result[baseKey] = list;
			}

			list.Add(value);
		}

		return result;
	}

	private static string StripIndexSuffix(string key)
	{
		var bracketIndex = key.IndexOf('[');
		return bracketIndex >= 0 ? key[..bracketIndex] : key;
	}
}
