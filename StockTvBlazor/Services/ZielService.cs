using StockTvBlazor.Models;

namespace StockTvBlazor.Services;

public class ZielService(SettingsService settingsService, ILogger<MatchService> logger)
{
	private readonly SettingsService _settingsService = settingsService;
	private readonly ILogger<MatchService> _logger = logger;
	private ZielBewerb? _currentZielBewerb;

	public void InitializeZiel()
	{
		_currentZielBewerb ??= new ZielBewerb(_settingsService);
		_logger.LogInformation("ZielService wurde initialisiert.");
	}

	public ZielBewerb CurrentZielBewerb => _currentZielBewerb
			?? throw new InvalidOperationException("ZielBewerb wurde nicht initialisiert. Prüfe Program.cs!");

	public void SetTeilnehmer(byte[] name)
	{
		var spielerName = System.Text.Encoding.UTF8.GetString(name);
		CurrentZielBewerb.AddSpielerName(spielerName);
	}
}
