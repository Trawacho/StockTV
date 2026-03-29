namespace StockTvBlazor.Components.Services
{
	public class MatchService(SettingsService settingsService, ILogger<MatchService> logger)
	{
		private readonly SettingsService _settingsService = settingsService;
		private readonly ILogger<MatchService> _logger = logger;
		private Components.Models.Match? _currentMatch;

		public Models.Match CurrentMatch => _currentMatch
			?? throw new InvalidOperationException("Match wurde nicht initialisiert. Prüfe Program.cs!");

		public void InitializeMatch()
		{
			_currentMatch ??= new Models.Match(_settingsService, _logger);
		}
	}
}
