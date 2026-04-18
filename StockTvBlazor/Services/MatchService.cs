using StockTvBlazor.Models;

namespace StockTvBlazor.Services
{
	public class MatchService(SettingsService settingsService, ILogger<MatchService> logger)
	{
		private readonly SettingsService _settingsService = settingsService;
		private readonly ILogger<MatchService> _logger = logger;
		private Match? _currentMatch;

		public Match CurrentMatch => _currentMatch
			?? throw new InvalidOperationException("Match wurde nicht initialisiert. Prüfe Program.cs!");

		public void InitializeMatch()
		{
			_currentMatch ??= new Models.Match(_settingsService, _logger);
		}

		public void SetTeamNames(byte[] teamNamesArray)
		{
			var teamNames = System.Text.Encoding.UTF8.GetString(teamNamesArray);
			CurrentMatch.ClearBegegnungen();
			var parts = teamNames.TrimEnd(';').Split(';');
			foreach (var part in parts)
			{
				var begegnung = part.Split(':');
				if (int.TryParse(begegnung[0], out int spielnummer))
				{
					CurrentMatch.AddBegegnung(new Models.Begegnung(spielnummer, begegnung[1], begegnung[2]));
				}
			}

		}
	}
}
