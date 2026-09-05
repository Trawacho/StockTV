using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StockTvBlazor.E2ETests.Helpers;

/// <summary>
/// Helper utilities for comprehensive E2E test scenario scripting.
/// </summary>
public static class GameplayScriptHelpers
{
	/// <summary>
	/// Build a 10-byte settings array, selectively overwriting fields.
	/// </summary>
	public static byte[] BuildSettingsBytes(
		byte[] currentSettings,
		byte? modus = null,
		byte? maxPunkteProKehre = null,
		byte? maxKehrenProSpiel = null)
	{
		if (currentSettings.Length < 10)
			throw new ArgumentException("currentSettings must be at least 10 bytes");

		var result = new byte[10];
		Array.Copy(currentSettings, result, 10);

		// Field positions: [0]=BahnNummer, [1]=Spielgruppe, [2]=Modus, [3]=Richtung, [4]=Theme,
		//                  [5]=MaxPunkteProKehre, [6]=MaxKehrenProSpiel, [7]=MidColumnWidth, [8]=MessageVersion, [9]=reserved
		if (modus.HasValue) result[2] = modus.Value;
		if (maxPunkteProKehre.HasValue) result[5] = maxPunkteProKehre.Value;
		if (maxKehrenProSpiel.HasValue) result[6] = maxKehrenProSpiel.Value;

		return result;
	}

	/// <summary>
	/// Strip the 10-byte settings prefix from a GetResult payload and parse the JSON Games array.
	/// </summary>
	public static List<GameSnapshot>? StripPrefixAndParseGames(string payload)
	{
		if (payload.Length < 10)
			return null;

		var jsonPart = payload.Substring(10);
		try
		{
			return JsonSerializer.Deserialize<List<GameSnapshot>>(jsonPart);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Parse "Spiel: X    Kehre: Y" style header text and extract Spiel/Kehre numbers.
	/// </summary>
	public static (int Spiel, int Kehre) ParseHeaderSpielUndKehre(string headerText)
	{
		var spielMatch = Regex.Match(headerText, @"Spiel:\s*(\d+)");
		var kehreMatch = Regex.Match(headerText, @"Kehre:\s*(\d+)");

		int spiel = spielMatch.Success ? int.Parse(spielMatch.Groups[1].Value) : 0;
		int kehre = kehreMatch.Success ? int.Parse(kehreMatch.Groups[1].Value) : 0;

		return (spiel, kehre);
	}

	/// <summary>
	/// Poll until a condition is true or timeout expires.
	/// </summary>
	public static async Task<bool> PollUntilAsync(
		Func<Task<bool>> condition,
		TimeSpan timeout,
		TimeSpan? pollInterval = null)
	{
		pollInterval ??= TimeSpan.FromMilliseconds(100);
		var deadline = DateTime.UtcNow.Add(timeout);

		while (DateTime.UtcNow < deadline)
		{
			if (await condition())
				return true;

			await Task.Delay(pollInterval.Value);
		}

		return false;
	}

	/// <summary>
	/// Generate a random turn value avoiding repeated digits (for debounce safety in multi-digit input).
	/// </summary>
	public static string RandomTurnValue(Random rng, int maxValue)
	{
		if (maxValue < 0) return "0";
		if (maxValue < 10) return rng.Next(0, maxValue + 1).ToString();

		// For multi-digit: ensure digits differ to avoid debounce overlap
		int tens = rng.Next(1, (maxValue / 10) + 1);
		int ones = rng.Next(0, 10);

		// Avoid repeated digit
		if (ones == tens % 10)
			ones = (ones + 1) % 10;

		return $"{tens}{ones}";
	}

	/// <summary>
	/// DTO for Game JSON deserialization from GetResult broadcasts.
	/// </summary>
	public class GameSnapshot
	{
		public int GameNumber { get; set; }
		public List<TurnSnapshot> Turns { get; set; } = new();
	}

	/// <summary>
	/// DTO for Turn JSON deserialization.
	/// </summary>
	public class TurnSnapshot
	{
		public int TurnNumber { get; set; }
		public int PointsLeft { get; set; }
		public int PointsRight { get; set; }
	}

	/// <summary>
	/// Calculate total points from a list of turns.
	/// </summary>
	public static (int Left, int Right) SumTurns(List<TurnSnapshot> turns)
	{
		int left = 0, right = 0;
		foreach (var turn in turns)
		{
			left += turn.PointsLeft;
			right += turn.PointsRight;
		}
		return (left, right);
	}
}
