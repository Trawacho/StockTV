using StockTvBlazor.Services;
using StockTvBlazor.Settings;
using System.Text;
using System.Text.Json;

namespace StockTvBlazor.Models;

public class Match
{
	private readonly List<Game> _games = [];

	public event Action? OnMatchChanged;

	private readonly SettingsService _settingsService;

	private readonly ILogger<MatchService> _logger;

	private readonly GameStatePersistenceService _gamePersistence;

	public Match(SettingsService settingsService, ILogger<MatchService> logger, GameStatePersistenceService gamePersistence)
	{
		_settingsService = settingsService;
		_logger = logger;
		_gamePersistence = gamePersistence;

		_games.Add(new Game(_settingsService.CurrentSettings, 1));
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

	#region Match Stats

	public int LeftPointsOverAll => _games.Sum(g => g.LeftPointsSum);

	public int RightPointsOverAll => _games.Sum(g => g.RightPointsSum);

	public int MatchPointsLeft => _games.Sum(g => g.GamePointsLeft);

	public int MatchPointsRight => _games.Sum(g => g.GamePointsRight);

	#endregion

	#region Begegnungen

	private readonly List<Begegnung> _begegnungen = [];

	public IEnumerable<Begegnung> Begegnungen => _begegnungen.AsReadOnly();

	public void ClearBegegnungen()
	{
		_begegnungen.Clear();
		OnMatchChanged?.Invoke();
	}

	public void AddBegegnung(Begegnung begegnung)
	{
		_begegnungen.Add(begegnung);
		OnMatchChanged?.Invoke();
	}

	#endregion

	#region Turns

	public void AddTurn(Turn turn)
	{
		var s = _settingsService.CurrentSettings;

		turn.TurnNumber = CurrentGame.Turns.Count + 1;

		if (s.Game.MaxKehrenProSpiel > CurrentGame.Turns.Count)
		{
			CurrentGame.Turns.Add(turn);
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

		OnMatchChanged?.Invoke();
	}

	private void ClearMatchStateInMemory()
	{
		var s = _settingsService.CurrentSettings;
		ClearBegegnungen();
		_games.Clear();
		_games.Add(new Game(s, 1));
		OnMatchChanged?.Invoke();
	}

	public void Reset(bool force = false)
	{
		var s = _settingsService.CurrentSettings;

		if (force)
		{
			ClearMatchStateInMemory();
			_gamePersistence.DeleteMatchState();
			return;
		}

		if (s.Game.CurrentModus == GameSettings.Modus.Turnier ||
			s.Game.CurrentModus == GameSettings.Modus.BestOf)
		{
			if (CurrentGame.Turns.Count == s.Game.MaxKehrenProSpiel)
			{
				_games.Add(new Game(s, Convert.ToByte(_games.Count + 1)));
			}
		}
		else
		{
			CurrentGame.Turns.Clear();
		}

		OnMatchChanged?.Invoke();
	}

	public async Task SaveMatchStateAsync()
	{
		var s = _settingsService.CurrentSettings;

		if (s.Game.CurrentModus == GameSettings.Modus.Training)
			return;

		var allTurns = Games.SelectMany(g => g.Turns).ToList();
		await _gamePersistence.SaveMatchStateAsync(allTurns);
	}

	public async Task LoadMatchStateAsync()
	{
		var s = _settingsService.CurrentSettings;

		if (s.Game.CurrentModus == GameSettings.Modus.Training)
			return;

		ClearMatchStateInMemory();

		var allTurns = await _gamePersistence.LoadMatchStateAsync();

		if (allTurns == null || allTurns.Count == 0)
			return;

		_logger.LogInformation("Loaded {Count} turns from match-state.json", allTurns.Count);
		foreach (var turn in allTurns)
		{
			AddTurn(turn);
			Reset();
		}
	}

	#endregion

	#region Serialization

	internal byte[] Serialize()
	{
		var values = new List<byte>();

		values.AddRange(_settingsService.GetSettings());

		foreach (Game g in Games)
		{
			values.Add(Convert.ToByte(Math.Min(g.Turns.Sum(t => t.PointsLeft), 255)));
			values.Add(Convert.ToByte(Math.Min(g.Turns.Sum(t => t.PointsRight), 255)));
		}

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
			_logger.LogError(ex, "Fehler bei der JSON-Serialisierung");
		}

		return [.. values];
	}

	#endregion
}