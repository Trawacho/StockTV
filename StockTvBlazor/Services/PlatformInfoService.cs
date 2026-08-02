using System.Runtime.InteropServices;

namespace StockTvBlazor.Services;

public enum SystemKind { Windows, RaspberryPi, Docker, Linux, Unknown }

public class PlatformInfoService
{
	private const string DeviceTreeModelPath = "/proc/device-tree/model";
	private const string DockerEnvPath = "/.dockerenv";
	private const string OsReleasePath = "/etc/os-release";

	public bool IsRaspberryPi { get; }

	// Exaktes Modell aus /proc/device-tree/model (z.B. "Raspberry Pi 4 Model B Rev 1.4"),
	// nur gesetzt wenn IsRaspberryPi true ist.
	public string? RaspberryPiModel { get; }

	public SystemKind Kind { get; }

	// Menschenlesbare OS-Beschreibung fuer Diagnosezwecke (z.B. im mDNS-TXT-Record "osVer" und im
	// NetMQ-Alive-Broadcast): Systemart + Distribution/Modell + Architektur. Auf Linux wird dafuer
	// bevorzugt /etc/os-release (PRETTY_NAME) gelesen statt RuntimeInformation.OSDescription, da
	// Letzteres nur den rohen Kernel-Build-String liefert (z.B. "Linux 6.6.51+rpt-rpi-v8 #1 SMP
	// PREEMPT Debian 1:6.6.51-1+rpt3 (2024-10-08) aarch64") statt eines sauberen Distri-Namens.
	public string OsVersion { get; }

	public PlatformInfoService(ILogger<PlatformInfoService> logger)
	{
		(IsRaspberryPi, RaspberryPiModel) = DetectRaspberryPi(logger);
		Kind = DetectSystemKind(IsRaspberryPi, logger);
		OsVersion = BuildOsVersion(Kind, RaspberryPiModel, logger);
	}

	private static (bool IsRaspberryPi, string? Model) DetectRaspberryPi(ILogger logger)
	{
		try
		{
			if (!OperatingSystem.IsLinux())
				return (false, null);

			if (!File.Exists(DeviceTreeModelPath))
				return (false, null);

			// Device-Tree-String-Properties sind NUL-terminiert - ohne TrimEnd('\0') haengt ein
			// Steuerzeichen am Modellstring, das z.B. in JSON-Payloads unschoen auffallen wuerde.
			var model = File.ReadAllText(DeviceTreeModelPath).TrimEnd('\0').Trim();
			var isPi = model.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
			return (isPi, isPi ? model : null);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Raspberry-Pi-Erkennung fehlgeschlagen, gehe von 'kein Raspberry Pi' aus");
			return (false, null);
		}
	}

	private static SystemKind DetectSystemKind(bool isRaspberryPi, ILogger logger)
	{
		// isRaspberryPi kommt bereits fertig ermittelt von DetectRaspberryPi() herein, die intern
		// schon OperatingSystem.IsLinux() als Voraussetzung prueft (siehe dort) - ist der Wert hier
		// true, steht Linux also zweifelsfrei bereits fest, ein IsWindows()-Check waere in diesem
		// Fall reine Verschwendung. Deshalb zuerst pruefen: zugleich die spezifischste Erkennung.
		if (isRaspberryPi)
			return SystemKind.RaspberryPi;

		if (OperatingSystem.IsWindows())
			return SystemKind.Windows;

		// Ab hier NICHT mehr per Ausschlussverfahren "also Linux" annehmen - .NET laeuft auch auf
		// macOS/FreeBSD/etc., die zwar kein Deployment-Ziel dieses Projekts sind, aber sonst
		// faelschlich als "Linux" gemeldet wuerden. Docker-Erkennung ergibt ausserhalb von Linux
		// ohnehin keinen Sinn (dieses Projekt baut nur Linux-Container, siehe build/Dockerfile).
		if (!OperatingSystem.IsLinux())
		{
			logger.LogWarning("Unbekanntes Betriebssystem erkannt: {OsDescription}", RuntimeInformation.OSDescription);
			return SystemKind.Unknown;
		}

		try
		{
			// DOTNET_RUNNING_IN_CONTAINER(S) wird von den offiziellen Microsoft-.NET-Images
			// automatisch gesetzt (siehe build/Dockerfile: mcr.microsoft.com/dotnet/aspnet-Basis);
			// /.dockerenv als Fallback fuer andere Container-Runtimes.
			if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINERS") == "true"
				|| Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
				|| File.Exists(DockerEnvPath))
				return SystemKind.Docker;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Docker-Erkennung fehlgeschlagen, gehe von 'kein Docker' aus");
		}

		return SystemKind.Linux;
	}

	private static string BuildOsVersion(SystemKind kind, string? raspberryPiModel, ILogger logger)
	{
		var architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

		if (kind == SystemKind.Windows)
			return $"{kind}: {RuntimeInformation.OSDescription} ({architecture})";

		// Linux-Zweige (RaspberryPi/Docker/Linux): saubere Distri-Bezeichnung aus /etc/os-release
		// bevorzugen, Fallback auf den rohen Kernel-Build-String falls die Datei fehlt/nicht lesbar
		// ist. Bei Unknown (z.B. macOS/FreeBSD) existiert /etc/os-release ohnehin nicht, der
		// Fallback liefert dort korrekt die tatsaechliche OS-Beschreibung.
		var distro = ReadOsReleasePrettyName(logger) ?? RuntimeInformation.OSDescription;

		var details = kind == SystemKind.RaspberryPi && raspberryPiModel is not null
			? $"{raspberryPiModel}, {distro}"
			: distro;

		return $"{kind}: {details} ({architecture})";
	}

	private static string? ReadOsReleasePrettyName(ILogger logger)
	{
		try
		{
			if (!File.Exists(OsReleasePath))
				return null;

			foreach (var line in File.ReadLines(OsReleasePath))
			{
				if (!line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
					continue;

				return line["PRETTY_NAME=".Length..].Trim().Trim('"');
			}

			return null;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "/etc/os-release konnte nicht gelesen werden");
			return null;
		}
	}
}
