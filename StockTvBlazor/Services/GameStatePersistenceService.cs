using StockTvBlazor.Models;
using System.Text.Json;

namespace StockTvBlazor.Services;

public class GameStatePersistenceService(ILogger<GameStatePersistenceService> logger)
{
	private readonly ILogger<GameStatePersistenceService> _logger = logger;

	private readonly string _matchStateFileName = "match-state.json";
	private readonly string _zielStateFileName = "ziel-state.json";

	private string _configFolderPath
	{
		get
		{
			string appDataPath = AppContext.BaseDirectory;
			return Path.Combine(appDataPath, "_config");
		}
	}

	private string _matchStateFilePath => Path.Combine(_configFolderPath, _matchStateFileName);
	private string _zielStateFilePath => Path.Combine(_configFolderPath, _zielStateFileName);

	#region Match State

	public async Task<List<Turn>?> LoadMatchStateAsync()
	{
		try
		{
			if (!File.Exists(_matchStateFilePath))
				return null;

			var json = await File.ReadAllTextAsync(_matchStateFilePath);
			if (string.IsNullOrWhiteSpace(json))
				return null;

			var data = JsonSerializer.Deserialize<MatchStateData>(json);
			if (data?.Turns == null || data.Turns.Count == 0)
				return null;

			return data.Turns;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Laden von match-state.json");
			return null;
		}
	}

	public async Task SaveMatchStateAsync(List<Turn> turns)
	{
		try
		{
			EnsureConfigFolderExists();

			if (turns.Count == 0)
			{
				DeleteMatchState();
				return;
			}

			var data = new MatchStateData { Turns = turns };
			var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
			_logger.LogDebug("Schreibe match-state.json ({Count} Kehren) nach: {Path}", turns.Count, _matchStateFilePath);
			await File.WriteAllTextAsync(_matchStateFilePath, json);

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Speichern von match-state.json unter: {Path}", _matchStateFilePath);
		}
	}

	public void DeleteMatchState()
	{
		try
		{
			_logger.LogDebug("Versuche match-state.json zu löschen unter: {Path}", _matchStateFilePath);
			if (File.Exists(_matchStateFilePath))
			{
				File.Delete(_matchStateFilePath);
			}
			else
			{
				_logger.LogWarning("match-state.json nicht gefunden unter: {Path}", _matchStateFilePath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Löschen von match-state.json unter: {Path}", _matchStateFilePath);
		}
	}

	#endregion

	#region Ziel State

	public async Task<ZielStateData?> LoadZielStateAsync()
	{
		try
		{
			if (!File.Exists(_zielStateFilePath))
				return null;

			var json = await File.ReadAllTextAsync(_zielStateFilePath);
			if (string.IsNullOrWhiteSpace(json))
				return null;

			var data = JsonSerializer.Deserialize<ZielStateData>(json);
			if (data?.Versuche == null || data.Versuche.Count == 0)
				return null;

			return data;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Laden von ziel-state.json");
			return null;
		}
	}

	public async Task SaveZielStateAsync(ZielStateData state)
	{
		try
		{
			EnsureConfigFolderExists();

			// Prüfen ob leer (alle Versuche leer)
			bool isEmpty = state.Versuche.Values.All(v => v.Count == 0);
			if (isEmpty)
			{
				DeleteZielState();
				return;
			}

			var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(_zielStateFilePath, json);

			_logger.LogDebug("ziel-state.json gespeichert");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Speichern von ziel-state.json");
		}
	}

	public void DeleteZielState()
	{
		try
		{
			if (File.Exists(_zielStateFilePath))
			{
				File.Delete(_zielStateFilePath);
				_logger.LogDebug("ziel-state.json gelöscht");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Fehler beim Löschen von ziel-state.json");
		}
	}

	#endregion

	#region Helper

	private void EnsureConfigFolderExists()
	{
		if (!Directory.Exists(_configFolderPath))
		{
			Directory.CreateDirectory(_configFolderPath);
		}
	}

	#endregion

	#region Data Models

	public class MatchStateData
	{
		public List<Turn> Turns { get; set; } = new();
	}

	public class ZielStateData
	{
		public Dictionary<string, List<int>> Versuche { get; set; } = new();
		public int Runde { get; set; } = 1;
		public int Runde1Summe { get; set; } = 0;
	}

	#endregion
}
