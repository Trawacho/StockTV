using StockTvBlazor.Components.Services;
using System.Text;
using System.Text.Json;

namespace StockTvBlazor.Components.Models;

public class Match
{
	private readonly List<Game> _games = [];

	public event Action? OnMatchChanged;

	private readonly SettingsService _settingsService;
	private readonly ILogger<MatchService> _logger;

	public Match(SettingsService settingsService, ILogger<MatchService> logger)
	{
		System.Diagnostics.Debug.WriteLine("new Match created");
		_settingsService = settingsService;
		_logger = logger;
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
	public void AddTurn(Turn turn)
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

	internal byte[] Serialize()
	{
		/* 
			*  the byte array starts with ten bytes, containing the settings, starting with courtnumber, groupnumber, modus, direction,.....
			*  starting with the 11th byte the values from the games are following, always two bytes per game.
			*  the first byte is the sum of the left team, the second is the sum of the right team followed by the next game with also 2 bytes length
			*  
			*  e.g.
			*  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16
			*  01 02 09 03 15 05 03 00 00 00 09 03 03 15 05 03 
			*  Court 1
			*     Group 2
			*        Modus 09
			*           Direction 03
			*  ...
			*                                 Game1: 9:3
			*                                      Game2: 3:15
			*                                           Game3: 5:3
			*  
			*/


		var values = new List<byte>();

		values.AddRange(_settingsService.GetSettings());

		//Add for each Game the sum of the turn-value for left and right
		foreach (Game g in Games)
		{
			values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsLeft)));
			values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsRight)));
		}

		//Convert the list of values to an array
		return [.. values];
	}

	internal byte[] SerializeJson()
	{
		var values = new List<byte>();
		try
		{
			values.AddRange(_settingsService.GetSettings());
			string json = JsonSerializer.Serialize(Games);
			values.AddRange(Encoding.UTF8.GetBytes(json));
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, $"Fehler bei der JSON-Serialisierung");
			// Hier könntest du auch eine benutzerfreundliche Fehlermeldung zurückgeben oder eine alternative Serialisierung versuchen
		}

		return [.. values];
	}
}

