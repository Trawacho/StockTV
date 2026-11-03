using StockTvBlazor.Models;
using StockTvBlazor.Networking;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Services;

public class MatchService(
	SettingsService settingsService,
	ILogger<MatchService> logger,
	NetMqPublisherService publisherService,
	GameEventBroadcaster broadcaster) : IGameInputService
{
	private readonly SettingsService _settingsService = settingsService;
	private readonly ILogger<MatchService> _logger = logger;
	private readonly NetMqPublisherService _publisherService = publisherService;
	private readonly GameEventBroadcaster _broadcaster = broadcaster;
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
			if (string.IsNullOrWhiteSpace(part)) continue;
			var begegnung = part.Split(':');
			if (begegnung.Length >= 3 && int.TryParse(begegnung[0], out int spielnummer))
			{
				CurrentMatch.AddBegegnung(new Models.Begegnung(spielnummer, begegnung[1], begegnung[2]));
			}
		}
	}

	/// <summary>
	/// Setzt die Teamnamen für den BestOf-Modus direkt über die Input-Seite (statt extern per
	/// NetMQ). Trägt dieselben zwei Namen für die Spiele 1-7 ein, damit sie über die gesamte Serie
	/// nicht erneut gesetzt werden müssen. leftFieldValue/rightFieldValue beziehen sich auf die
	/// aktuell sichtbare linke/rechte Position - die Zuordnung zu TeamA/TeamB wird anhand der
	/// aktuellen Richtung umgerechnet, damit Begegnung.TeamNameLeft/-Right sofort wieder denselben
	/// Namen an der eingegebenen Position liefert.
	/// </summary>
	public void SetBestOfTeamNames(string leftFieldValue, string rightFieldValue)
	{
		var isLinks = _settingsService.CurrentSettings.UI.CurrentRichtung == UiSettings.Richtung.Links;
		var teamA = isLinks ? rightFieldValue : leftFieldValue;
		var teamB = isLinks ? leftFieldValue : rightFieldValue;

		CurrentMatch.ClearBegegnungen();
		for (int spielNummer = 1; spielNummer <= 7; spielNummer++)
			CurrentMatch.AddBegegnung(new Begegnung(spielNummer, teamA, teamB));

		// Konsistent mit ProcessKeyAsync: MatchService feuert OnGlobalRefresh nach jeder Mutation.
		RequestGlobalRefresh();
	}

	private int _inputValue;

	private int _specialCounter;

	private readonly Debounce _debounce = new();

	public event Action? OnGlobalRefresh;

	public event Action<string>? OnNavigationRequested;

	public void RequestGlobalRefresh() => OnGlobalRefresh?.Invoke();

	public int Inputvalue => _inputValue;

	public async Task ProcessKeyAsync(string value)
	{
		var s = _settingsService.CurrentSettings;

		// Special Counter
		if ((_inputValue == 0 || _inputValue == 10)
			&& value == "Enter"
			&& !s.General.BlockLocalChanges)
		{
			_specialCounter++;
		}
		else
		{
			_specialCounter = 0;
		}

		// Debounce
		if (!(value == "-" && _inputValue == 0 && !s.General.BlockLocalChanges))
		{
			if (!_debounce.IsDebounceOk(value))
				return;
		}

		switch (value)
		{
			case "Enter": ShowSpecialPage(); break;
			case "*": AddToGreen(); break;
			case "-": DeleteLastTurn(); break;
			case "/" or "Backspace": AddToRed(); break;
			case "+": Reset(); break;

			default:
				int? input = value switch
				{
					"1" or "End" => 1,
					"2" or "ArrowDown" => 2,
					"3" or "PageDown" => 3,
					"4" or "ArrowLeft" => 4,
					"5" or "Clear" => 5,
					"6" or "ArrowRight" => 6,
					"7" or "Home" => 7,
					"8" or "ArrowUp" => 8,
					"9" or "PageUp" => 9,
					"0" or "Insert" => 0,
					_ => null
				};

				if (input.HasValue)
					AddInput(input.Value);

				break;
		}

		OnGlobalRefresh?.Invoke();

		// Training ist freies Spiel ohne Spielzaehlung (siehe CurrentMatch.Reset()) - entsprechend
		// soll auch nichts an das zentrale Verwaltungsprogramm gesendet werden.
		if (s.Game.CurrentModus != GameSettings.Modus.Training)
		{
			// Beide Wege bedienen, solange StockAppV2 noch auf NetMQ hoert.
			_publisherService.Publish("GetResult", CurrentMatch.SerializeJson());
			_broadcaster.ResultChanged(new Api.ResultDto(
				Api.GameDtoFactory.Settings(s),
				Api.GameDtoFactory.Match(CurrentMatch),
				Ziel: null));
		}
	}

	private void AddInput(int value)
	{
		var s = _settingsService.CurrentSettings;

		int newValue = (_inputValue < 0) ? value : (_inputValue * 10) + value;
		int maxPoints = s.Game.MaxPunkteProKehre;

		if (newValue <= maxPoints)
			_inputValue = newValue;
		else
			_inputValue =  -1;
	}

	private void AddToGreen()
	{
		if (_inputValue == -1)
			return;

		var s = _settingsService.CurrentSettings;

		var turn = Turn.Create(_inputValue, s.UI.CurrentRichtung, true);

		CurrentMatch.AddTurn(turn);

		_inputValue = -1;
	}

	private void AddToRed()
	{
		if (_inputValue == -1)
			return;

		var s = _settingsService.CurrentSettings;

		var turn = Turn.Create(_inputValue, s.UI.CurrentRichtung, false);

		CurrentMatch.AddTurn(turn);

		_inputValue = -1;
	}

	private void Reset(bool force = false)
	{
		CurrentMatch.Reset(force);
		_inputValue = -1;
	}

	private void DeleteLastTurn()
	{
		if (_inputValue > 0)
		{
			_inputValue = -1;
			return;
		}

		CurrentMatch.DeleteLastTurn();
	}

	private protected void ShowSpecialPage()
	{
		if (_specialCounter < 5) return;

		_specialCounter = 0;

		if (_inputValue == 0)
		{
			_settingsService.SettingsPageActive = true;
			OnNavigationRequested?.Invoke("/settings");
		}
		else if (_inputValue == 10)
		{
			// TODO: Marketing
		}
	}
}