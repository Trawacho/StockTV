using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.Models;

public class Match
{
	private readonly List<Game> _games = [];

	public event Action? OnMatchChanged;

	private readonly SettingsService _settingsService;

	public Match(SettingsService settingsService)
	{
		System.Diagnostics.Debug.WriteLine("new Match created");
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

	/// <summary>
	/// Es wird die übergeben Kehre zum aktuellen Spiel hinzugefügt, sofern die maximale Anzahl an Kehren pro Spiel nicht überschritten wird. Ansonsten wird ein neues Spiel begonnen und die Kehre diesem Spiel hinzugefügt.
	/// Es sollte anschließend SaveTurnsToLocalSettingsAsync aufgerufen werden, um die Änderungen zu speichern.
	/// </summary>
	/// <param name="turn"></param>
	public void AddTurn(ITurn turn)
	{
		turn.TurnNumber = CurrentGame.Turns.Count + 1;

		if (_settingsService.CurrentSettings.MaxKehrenProSpiel > CurrentGame.Turns.Count)
		{
			CurrentGame.Turns.Add(turn);
			OnMatchChanged?.Invoke();
		}
	}

	/// <summary>
	/// Es wird die letzte Kehre gelöscht. Wenn es keine Kehren mehr im aktuellen Spiel gibt, wird das aktuelle Spiel gelöscht, sofern es mehr als ein Spiel gibt.
	/// Es sollte anschließend SaveTurnsToLocalSettingsAsync aufgerufen werden, um die Änderungen zu speichern.
	/// </summary>
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

		OnMatchChanged?.Invoke();
	}

	/// <summary>
	/// Es werden alle Kehren gelöscht, wenn force = true ist. Ansonsten wird nur die aktuelle Kehre gelöscht oder ein neues Spiel begonnen, wenn die maximale Anzahl an Kehren pro Spiel erreicht ist.
	/// Es sollte anschließend SaveTurnsToLocalSettingsAsync aufgerufen werden, um die Änderungen zu speichern.
	/// </summary>
	/// <param name="force"></param>
	public void Reset(bool force = false)
	{
		if (force)
		{
			this.Begegnungen.Clear();
			this._games.Clear();
			this._games.Add(new Game(_settingsService.CurrentSettings, 1));
			OnMatchChanged?.Invoke();
			return;
		}


		if (_settingsService.CurrentSettings.Modus == Settings.MODUS.TURNIER ||
			_settingsService.CurrentSettings.Modus == Settings.MODUS.BESTOF)
		{
			if (CurrentGame.Turns.Count == _settingsService.CurrentSettings.MaxKehrenProSpiel)
			{
				_games.Add(new Game(_settingsService.CurrentSettings, Convert.ToByte(_games.Count + 1)));
			}
		}
		else
		{
			CurrentGame.Turns.Clear();
		}
		
		OnMatchChanged?.Invoke();
	}

	public async Task SaveTurnsToLocalSettingsAsync()
	{
		var allTurns = Games.SelectMany(g => g.Turns).OfType<Turn>().ToList();
		await _settingsService.SaveTurnsAsync(allTurns);
	}

	private void LoadTurnsFromLocalSettings()
	{
		var allTurns = _settingsService.CurrentSettings.Kehren;
		foreach (var turn in allTurns)
		{
			AddTurn(turn);
			Reset();
		}
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

