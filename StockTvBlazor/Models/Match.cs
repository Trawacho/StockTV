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

	public Match(SettingsService settingsService, ILogger<MatchService> logger)
	{
		_settingsService = settingsService;
		_logger = logger;

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

	public void Reset(bool force = false)
	{
		var s = _settingsService.CurrentSettings;

		if (force)
		{
			ClearBegegnungen();
			_games.Clear();
			_games.Add(new Game(s, 1));

			OnMatchChanged?.Invoke();
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

	#endregion

	#region Persistenz (stocktv.state.json)

	/// <summary>Zieht einen Abzug des Spielstands fuer die Ablage auf der Platte.</summary>
	public MatchState CreateSnapshot() => new()
	{
		Games = [.. _games.Select(g => new GameState
		{
			GameNumber = g.GameNumber,
			Turns = [.. g.Turns]
		})],
		Begegnungen = [.. _begegnungen.Select(b => new BegegnungState
		{
			Spielnummer = b.Spielnummer,
			MannschaftA = b.MannschaftA,
			MannschaftB = b.MannschaftB
		})]
	};

	/// <summary>
	/// Stellt einen gesicherten Spielstand wieder her. Der Aufrufer hat vorher geprueft, dass er
	/// zu Bahn, Modus und Zeitfenster passt (siehe <see cref="Services.GameStateStore"/>).
	/// </summary>
	public void RestoreFrom(MatchState state)
	{
		_games.Clear();
		_begegnungen.Clear();

		foreach (var gameState in state.Games.OrderBy(g => g.GameNumber))
		{
			// Ueber den Konstruktor mit Settings, sonst haette das Spiel eine eigene, leere
			// Settings-Instanz und MaxKehrenProSpiel waere falsch - davon haengen die Spielpunkte ab.
			var game = new Game(_settingsService.CurrentSettings, gameState.GameNumber);
			game.Turns.AddRange(gameState.Turns);
			_games.Add(game);
		}

		if (_games.Count == 0)
			_games.Add(new Game(_settingsService.CurrentSettings, 1));

		foreach (var b in state.Begegnungen)
			_begegnungen.Add(new Begegnung(b.Spielnummer, b.MannschaftA, b.MannschaftB));

		_logger.LogInformation(
			"Spielstand wiederhergestellt: {Games} Spiel(e), {Turns} Kehren, {Begegnungen} Begegnung(en)",
			_games.Count, _games.Sum(g => g.Turns.Count), _begegnungen.Count);

		OnMatchChanged?.Invoke();
	}

	#endregion

	#region Serialization

	internal byte[] Serialize()
	{
		var values = new List<byte>();

		values.AddRange(_settingsService.GetSettings());

		foreach (Game g in Games)
		{
			values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsLeft)));
			values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsRight)));
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