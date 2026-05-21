using StockTvBlazor.Models;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Services;

public class MatchService(SettingsService settingsService, ILogger<MatchService> logger, NetMqPublisherService publisherService)
{
	private readonly SettingsService _settingsService = settingsService;
	private readonly ILogger<MatchService> _logger = logger;
	private readonly NetMqPublisherService _publisherService = publisherService;
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
			case "*": await AddToGreenAsync(); break;
			case "-": await DeleteLastTurnAsync(); break;
			case "/" or "Backspace": await AddToRedAsync(); break;
			case "+": await ResetAsync(); break;

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

		_publisherService.Publish("GetResult", CurrentMatch.SerializeJson());
	}

	private void AddInput(int value)
	{
		var s = _settingsService.CurrentSettings;

		int newValue = (_inputValue < 0) ? value : (_inputValue * 10) + value;
		int maxPoints = s.Game.MaxPunkteProKehre;

		if (newValue <= maxPoints)
			_inputValue = newValue;
		else
			_inputValue = (value <= maxPoints) ? value : -1;
	}

	private async Task AddToGreenAsync()
	{
		if (_inputValue == -1)
			return;

		var s = _settingsService.CurrentSettings;

		var turn = Turn.Create(_inputValue, s.UI.CurrentRichtung, true);

		CurrentMatch.AddTurn(turn);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;
	}

	private async Task AddToRedAsync()
	{
		if (_inputValue == -1)
			return;

		var s = _settingsService.CurrentSettings;

		var turn = Turn.Create(_inputValue, s.UI.CurrentRichtung, false);

		CurrentMatch.AddTurn(turn);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;
	}

	private async Task ResetAsync(bool force = false)
	{
		CurrentMatch.Reset(force);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();
		_inputValue = -1;
	}

	private async Task DeleteLastTurnAsync()
	{
		if (_inputValue > 0)
		{
			_inputValue = -1;
			return;
		}

		CurrentMatch.DeleteLastTurn();
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();
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