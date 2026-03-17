using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.Models;

public class Match
{
	private readonly List<Game> _games = [];

	public event Action? OnMatchChanged;


	private Settings _settings => _settingsService.CurrentSettings;
	private readonly SettingsService _settingsService;

	public Match(SettingsService settingsService)
	{
		_settingsService = settingsService;
		_games.Add(new Game(_settingsService.CurrentSettings, 1));
		LoadTurnsFromLocalSettings();
	}

	public IEnumerable<Game> Games => _games;

	public Game CurrentGame
	{
		get
		{
			if (_games.Count == 0)
				_games.Add(new Game(_settingsService.CurrentSettings, 1));

			return _games.Last();
		}
	}

	public int MatchPointsLeft => _games.Sum(g => g.GamePointsLeft);

	public int MatchPointsRight => _games.Sum(g => g.GamePointsRight);

	public List<Begegnung> Begegnungen { get; set; } = [];


	public void AddTurn(ITurn turn)
	{
		turn.TurnNumber = CurrentGame.Turns.Count + 1;

		if (_settings.MaxKehrenProSpiel > CurrentGame.Turns.Count)
		{
			CurrentGame.Turns.Add(turn);
			SaveTurnsToLocalSettings();
			OnMatchChanged?.Invoke();
		}
	}

	public void DeleteLastTurn()
	{
		if (_games.Count > 1 && CurrentGame.Turns.Count == 0)
		{
			_games.RemoveAt(_games.Count - 1);
		}
		else
		{
			CurrentGame.DeleteLastTurn();
		}

		SaveTurnsToLocalSettings();
		OnMatchChanged?.Invoke();
	}

	public void Reset(bool force = false)
	{
		if (force)
		{
			this.Begegnungen.Clear();
			this._games.Clear();
			this._games.Add(new Game(_settingsService.CurrentSettings, 1));
			SaveTurnsToLocalSettings();
			OnMatchChanged?.Invoke();
			return;
		}


		if (_settings.Modus == Settings.MODUS.TURNIER ||
			_settings.Modus == Settings.MODUS.BESTOF)
		{
			if (CurrentGame.Turns.Count == _settings.MaxKehrenProSpiel)
			{
				_games.Add(new Game(_settingsService.CurrentSettings, Convert.ToByte(_games.Count + 1)));
			}
		}
		else
		{
			CurrentGame.Turns.Clear();
		}

		SaveTurnsToLocalSettings();
		OnMatchChanged?.Invoke();
	}

	private async void SaveTurnsToLocalSettings()
	{
		var allTurns = Games.SelectMany(g => g.Turns).OfType<Turn>().ToList();
		await _settingsService.SaveTurns(allTurns);
	}

	private void LoadTurnsFromLocalSettings()
	{
		//todo: Aktuell das Problem, dass die Kehren beim hinzufügen die Liste ändern
		var allTurns = _settingsService.CurrentSettings.Kehren;
		//foreach (var turn in allTurns)
		//{
		//	AddTurn(turn);
		//	Reset();
		//}
	}

	internal object? Serialize()
	{
		//todo: implement Serialization
		return null;
	}

	internal object? SerializeJson()
	{
		//todo: implement Serialization
		return null;
	}
}

