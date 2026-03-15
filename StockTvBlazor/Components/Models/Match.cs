namespace StockTvBlazor.Components.Models;

public class Match
{
	private readonly List<Game> _games = [];

	public event EventHandler? TurnsChanged;
	protected void RaiseTurnsChanged()
	{
		TurnsChanged?.Invoke(this, EventArgs.Empty);
		LoadTurnsFromLocalSettings();
	}

	private readonly Settings _settings;

	public Match(Settings settings)
	{
		_settings = settings;
		_games.Add(new Game(settings, 1));

	}
	
	public IEnumerable<Game> Games => _games;

	public Game CurrentGame
	{
		get
		{
			if (_games.Count == 0)
				_games.Add(new Game(_settings, 1));

			return _games.Last();
		}
	}

	public int MatchPointsLeft => _games.Sum(g => g.GamePointsLeft);

	public int MatchPointsRight => _games.Sum(g => g.GamePointsRight);

	public List<Begegnung> Begegnungen { get; set; } = [];


	public void AddTurn(Turn turn)
	{
		turn.TurnNumber = Convert.ToByte(CurrentGame.Turns.Count + 1);

		if (_settings.GameSettings.TurnsPerGame > CurrentGame.Turns.Count)
		{
			CurrentGame.Turns.Add(turn);
			SaveTurnsToLocalSettings();
			RaiseTurnsChanged();
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
		RaiseTurnsChanged();
	}

	public void Reset(bool force = false)
	{
		if (force)
		{
			this.Begegnungen.Clear();
			this._games.Clear();
			this._games.Add(new Game(_settings, 1));
			SaveTurnsToLocalSettings();
			RaiseTurnsChanged();
			return;
		}


		if (_settings.GameSettings.GameModus == GameSettings.GameModis.Turnier ||
			_settings.GameSettings.GameModus == GameSettings.GameModis.BestOf)
		{
			if (CurrentGame.Turns.Count == _settings.GameSettings.TurnsPerGame)
			{
				_games.Add(new Game(_settings, Convert.ToByte(_games.Count + 1)));
			}
		}
		else
		{
			CurrentGame.Turns.Clear();
		}

		SaveTurnsToLocalSettings();
		RaiseTurnsChanged();
	}

	private void SaveTurnsToLocalSettings()
	{
		//todo: Implement Save to localsettings
		return;
	}

	private void LoadTurnsFromLocalSettings()
	{
			//todo: Implement Save to localsettings
		return;
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

