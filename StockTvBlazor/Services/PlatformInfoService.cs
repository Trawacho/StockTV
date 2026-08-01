namespace StockTvBlazor.Services;

public class PlatformInfoService
{
	private const string DeviceTreeModelPath = "/proc/device-tree/model";

	public bool IsRaspberryPi { get; }

	public PlatformInfoService(ILogger<PlatformInfoService> logger)
	{
		IsRaspberryPi = DetectRaspberryPi(logger);
	}

	private static bool DetectRaspberryPi(ILogger logger)
	{
		try
		{
			if (!OperatingSystem.IsLinux())
				return false;

			if (!File.Exists(DeviceTreeModelPath))
				return false;

			var model = File.ReadAllText(DeviceTreeModelPath);
			return model.Contains("Raspberry Pi", StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Raspberry-Pi-Erkennung fehlgeschlagen, gehe von 'kein Raspberry Pi' aus");
			return false;
		}
	}
}
