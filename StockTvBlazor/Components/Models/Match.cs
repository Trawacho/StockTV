namespace StockTvBlazor.Components.Models
{
	public class Match
	{
		public event EventHandler? TurnsChanged;
		protected void RaiseTurnsChanged()
		{
			TurnsChanged?.Invoke(this, EventArgs.Empty);
			//todo: implement Load Settings
			//LoadTurnsFromLocalSettings();
		}

		private readonly Settings _settings;

		public Match(Settings settings)
		{
			_settings = settings;
			_games.Add(new Game(settings, 1));

		}

		private readonly List<Game> _games = [];
		public IEnumerable<Game> Games
		{
			get
			{
				return _games;
			}
		}

		public Game CurrentGame
		{
			get
			{
				if (_games.Count == 0)
				{
					_games.Add(new Game(_settings, 1));
				}
				return _games.Last();
			}
		}

		public int MatchPointsLeft
		{
			get
			{
				return _games.Sum(g => g.GamePointsLeft);
			}
		}

		public int MatchPointsRight
		{
			get
			{
				return _games.Sum(g => g.GamePointsRight);
			}
		}

		public List<Begegnung> Begegnungen { get; set; } = [];


		public void AddTurn(Turn turn)
		{
			turn.TurnNumber = Convert.ToByte(CurrentGame.Turns.Count + 1);

			if (_settings.GameSettings.TurnsPerGame > CurrentGame.Turns.Count)
			{
				CurrentGame.Turns.Add(turn);
				//todo: implement Save to local Settings
				//SaveTurnsToLocalSettings();
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

			//todo: implement Save to local Settings
			//SaveTurnsToLocalSettings();
			RaiseTurnsChanged();
		}

		public void Reset(bool force = false)
		{
			if (force)
			{
				this.Begegnungen.Clear();
				this._games.Clear();
				this._games.Add(new Game(_settings, 1));
				//todo: implement Save to local Settings
				//SaveTurnsToLocalSettings();
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

			//todo: implement Save to local Settings
			//SaveTurnsToLocalSettings();
			RaiseTurnsChanged();
		}

		private void SaveTurnsToLocalSettings()
		{
			throw new NotImplementedException();
		}

		private void LoadTurnsFromLocalSettings()
		{
			throw new NotImplementedException();
		}

		internal object Serialize()
		{
			throw new NotImplementedException();
		}

		internal object SerializeJson()
		{
			throw new NotImplementedException();
		}
	}
}

