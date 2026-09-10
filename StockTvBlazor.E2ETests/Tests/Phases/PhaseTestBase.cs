using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public abstract class PhaseTestBase
{
	protected readonly AppFixture Fixture;
	protected readonly Random Rng = new Random(1337);
	protected readonly ITestOutputHelper Output;
	protected TestLogWriter? Logger;
	protected const int DEBOUNCE_DELAY_MS = 1100;
	protected string CurrentPhase { get; set; } = "Test";

	protected PhaseTestBase(AppFixture fixture, ITestOutputHelper output)
	{
		Fixture = fixture;
		Output = output;
	}

	/// <summary>
	/// Schreibt eine Lognachricht mit Phasenbeschreibung und optionalem Symbol.
	/// </summary>
	/// <param name="phase">Die aktuelle Testphase</param>
	/// <param name="message">Die Lognachricht</param>
	/// <param name="symbol">Das anzuzeigende Symbol (Standard: "✓")</param>
	protected void Log(string phase, string message, string symbol = "✓")
	{
		Logger ??= new TestLogWriter(Output);
		Logger.WriteLn(phase, message, symbol);
	}

	/// <summary>
	/// Loggt den Start einer neuen Testphase mit beschreibendem Titel.
	/// </summary>
	/// <param name="phase">Der Name der Phase</param>
	/// <param name="description">Eine Beschreibung was die Phase testet</param>
	protected void LogPhaseStart(string phase, string description)
	{
		Logger ??= new TestLogWriter(Output);
		Logger.WritePhaseStart(phase, description);
	}

	/// <summary>
	/// Loggt das Ende einer Testphase.
	/// </summary>
	/// <param name="phase">Der Name der Phase die beendet wird</param>
	protected void LogPhaseEnd(string phase)
	{
		Logger ??= new TestLogWriter(Output);
		Logger.WritePhaseEnd(phase);
	}

	/// <summary>
	/// Holt die aktuellen Einstellungen vom NetMQ ReP-Socket.
	/// Falls die Abfrage fehlschlägt, werden Standardeinstellungen zurückgegeben.
	/// </summary>
	/// <returns>Ein 10-Byte-Array mit den aktuellen Einstellungen</returns>
	protected async Task<byte[]> GetCurrentSettings()
	{
		try
		{
			var response = Fixture.SendNetMqRaw("GetSettings");
			if (response.FrameCount >= 2)
				return response[1].ToByteArray();
		}
		catch { }

		return new byte[] { 1, 0, 0, 0, 0, 10, 6, 50, 1, 0 };
	}

	/// <summary>
	/// Sendet neue Einstellungen an die App über NetMQ und wartet auf die Verarbeitung.
	/// </summary>
	/// <param name="settingsBytes">Ein 10-Byte-Array mit den neuen Einstellungen</param>
	protected async Task SendSettings(byte[] settingsBytes)
	{
		Fixture.SendNetMqRaw("SetSettings", settingsBytes);
		await Task.Delay(DEBOUNCE_DELAY_MS);
	}

	/// <summary>
	/// Sendet einen ResetResult-Befehl an die App und wartet auf die Verarbeitung.
	/// </summary>
	protected async Task SendResetResult()
	{
		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(DEBOUNCE_DELAY_MS);
	}

	protected class EntryResult
	{
		public string Value { get; set; } = "";
		public string ConfirmKey { get; set; } = "";
		public bool IsLeftSide => ConfirmKey == "*";
		public bool IsRightSide => ConfirmKey == "/";
	}

	/// <summary>
	/// Sendet den Wert an die Seite und bestätigt ihn mit dem angegebenen ConfirmKey (oder einem zufälligen Key, wenn keiner angegeben ist).
	/// Validiert dass der Wert korrekt in der Anzeige erscheint, bevor der ConfirmKey gesendet wird.
	/// Optional kann ein expectedDisplay angegeben werden, um zu validieren dass der angezeigte Wert korrekt ist.
	/// </summary>
	/// <param name="value">Der zu sendende Wert</param>
	/// <param name="confirmKey">Der zu verwendende Bestätigungskey</param>
	/// <param name="expectedDisplay">Der erwartete Wert in der Anzeige</param>
	/// <returns>Das Ergebnis der Eingabe und Bestätigung</returns>
	protected async Task<EntryResult> EnterAndConfirm(string value, string? confirmKey = null, string? expectedDisplay = null)
	{
		if (Fixture.Page == null)
			return new EntryResult { Value = value, ConfirmKey = confirmKey ?? "" };

		confirmKey ??= GetConfirmKey();

		Log(CurrentPhase, $"Eingabe: {value}, Bestätigungskey: {confirmKey}");

		char? previousChar = null;
		foreach (char c in value)
		{
			// Wenn gleiches Zeichen wie zuvor: längere Verzögerung für Debounce (VOR dem Tastendruck!)
			if (previousChar != null && c == previousChar)
			{
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}

			await Fixture.Page.Keyboard.PressAsync(c.ToString());

			// Nach jedem Tastendruck warten, damit die UI aktualisiert wird
			await Task.Delay(200);

			previousChar = c;
		}

		// Validate that the input value is visible in the display field before sending confirm key
		var inputDisplay = await Fixture.Page.Locator(".score-cell.input-value").TextContentAsync();
		var expected = expectedDisplay ?? value;
		Assert.True(expected == inputDisplay?.Trim(), $"Input display should show '{expected}' but shows '{inputDisplay?.Trim()}'");

		await Fixture.Page.Keyboard.PressAsync(confirmKey);
		await Task.Delay(200);

		return new EntryResult { Value = value, ConfirmKey = confirmKey };
	}

	/// <summary>
	/// Holt das neueste GetResult-Payload vom NetMQ Publisher.
	/// </summary>
	/// <returns>Das Payload als String, oder null wenn keine GetResult-Nachrichten vorhanden sind</returns>
	protected string? GetLatestGetResultPayload()
	{
		var messages = Fixture.GetPublisherMessagesByTopic("GetResult");
		return messages.Count > 0 ? messages.Last().Payload : null;
	}

	/// <summary>
	/// Gibt einen zufälligen Bestätigungskey zurück: entweder "*" (Links/Grün) oder "/" (Rechts/Rot).
	/// </summary>
	/// <returns>Entweder "*" oder "/"</returns>
	protected string GetConfirmKey()
	{
		return Rng.Next(2) == 0 ? "*" : "/";
	}

	/// <summary>
	/// Trägt einen Kehren-Wert in die Listen turnsLeft und turnsRight ein, basierend auf der bestätigten Seite.
	/// Wenn Links bestätigt wurde, wird der Wert zu turnsLeft und 0 zu turnsRight hinzugefügt.
	/// Wenn Rechts bestätigt wurde, wird 0 zu turnsLeft und der Wert zu turnsRight hinzugefügt.
	/// </summary>
	/// <param name="result">Das Ergebnis der Eingabe (welche Seite bestätigt wurde)</param>
	/// <param name="value">Der eingegebene Wert</param>
	/// <param name="turnsLeft">Liste der Werte auf der linken Seite</param>
	/// <param name="turnsRight">Liste der Werte auf der rechten Seite</param>
	protected void TrackTurn(EntryResult result, int value, List<int> turnsLeft, List<int> turnsRight)
	{
		if (result.IsLeftSide)
		{
			turnsLeft.Add(value);
			turnsRight.Add(0);
		}
		else
		{
			turnsLeft.Add(0);
			turnsRight.Add(value);
		}
	}

	/// <summary>
	/// Validiert dass der komplette NetMQ GetResult-Payload alle erwarteten Spiele mit ihren Turns und Summen enthält.
	/// Prüft jeden Spieldatensatz einzeln und loggt die Ergebnisse.
	/// </summary>
	/// <param name="expectedGames">Dictionary mit GameNumber als Key und Tuple(turnsLeft, turnsRight) als Value. Enthält alle bis dahin abgeschlossenen Spiele.</param>
	protected async Task ValidateNetMqPublisherCompleteStateAsync(
		Dictionary<int, (List<int> turnsLeft, List<int> turnsRight)> expectedGames)
	{
		var payload = GetLatestGetResultPayload();
		if (string.IsNullOrEmpty(payload))
		{
			Log(CurrentPhase, "⚠ Kein GetResult-Payload vom NetMQ Publisher erhalten", "!");
			return;
		}

		var games = GameplayScriptHelpers.StripPrefixAndParseGames(payload);
		if (games == null)
		{
			Log(CurrentPhase, "⚠ GetResult-Payload konnte nicht geparst werden", "!");
			return;
		}

		// Validiere dass alle erwarteten Spiele im Payload enthalten sind
		Assert.Equal(expectedGames.Count, games.Count);

		foreach (var expectedGame in expectedGames)
		{
			int gameNumber = expectedGame.Key;
			var (expectedTurnsLeft, expectedTurnsRight) = expectedGame.Value;

			var broadcastGame = games.FirstOrDefault(g => g.GameNumber == gameNumber);
			Assert.NotNull(broadcastGame);

			// Vergleiche Turns
			var expectedLeftStr = expectedTurnsLeft.Count > 0 ? string.Join("-", expectedTurnsLeft) : "";
			var expectedRightStr = expectedTurnsRight.Count > 0 ? string.Join("-", expectedTurnsRight) : "";

			var broadcastLeftStr = broadcastGame.Turns.Count > 0
				? string.Join("-", broadcastGame.Turns.Select(t => t.PointsLeft))
				: "";
			var broadcastRightStr = broadcastGame.Turns.Count > 0
				? string.Join("-", broadcastGame.Turns.Select(t => t.PointsRight))
				: "";

			Assert.Equal(expectedLeftStr, broadcastLeftStr);
			Assert.Equal(expectedRightStr, broadcastRightStr);

			// Validiere Summen
			var (sumLeft, sumRight) = GameplayScriptHelpers.SumTurns(broadcastGame.Turns);
			int expectedSumLeft = expectedTurnsLeft.Sum();
			int expectedSumRight = expectedTurnsRight.Sum();

			Assert.Equal(expectedSumLeft, sumLeft);
			Assert.Equal(expectedSumRight, sumRight);

			Log(CurrentPhase,
				$"✓ NetMQ Spiel {gameNumber}: Links {broadcastLeftStr}={sumLeft} | Rechts {broadcastRightStr}={sumRight}",
				"✓");
		}
	}

	/// <summary>
	/// Validiert alle UI-Elemente der Spielanzeige gegen die erwarteten Werte:
	/// - Kehren und deren Summen (Links und Rechts)
	/// - Teamnamen (müssen beide vorhanden oder beide abwesend sein)
	/// - Spiel- und Kehren-Header
	///
	/// WICHTIG: Wenn expectedTeamLeft/expectedTeamRight übergeben werden, muss IMMER BEIDES übergeben werden!
	/// - null, null = validiere dass KEINE Teamnamen angezeigt werden
	/// - "Team1", "Team2" = validiere dass BEIDE Teamnamen angezeigt werden
	/// - Gemischte Werte führen zu Fehler!
	/// </summary>
	/// <param name="expectedTurnsLeft">Liste der erwarteten Punkte pro Kehre auf der linken Seite</param>
	/// <param name="expectedTurnsRight">Liste der erwarteten Punkte pro Kehre auf der rechten Seite</param>
	/// <param name="expectedTeamLeft">Erwarteter Team-Name links (muss zusammen mit expectedTeamRight angegeben werden)</param>
	/// <param name="expectedTeamRight">Erwarteter Team-Name rechts (muss zusammen mit expectedTeamLeft angegeben werden)</param>
	/// <param name="expectedGameNumber">Erwartete Spiel-Nummer im Header</param>
	/// <param name="expectedTurnNumber">Erwartete Kehren-Nummer im Header</param>
	protected async Task ValidateDisplayAsync(
		List<int>? expectedTurnsLeft = null,
		List<int>? expectedTurnsRight = null,
		string? expectedTeamLeft = null,
		string? expectedTeamRight = null,
		int? expectedGameNumber = null,
		int? expectedTurnNumber = null)
	{
		if (Fixture.Page == null)
			return;

		// Validate Turns and Sums
		if (expectedTurnsLeft != null || expectedTurnsRight != null)
		{
			var displayLeftSum = await Fixture.Page.Locator(".left-point-sum").TextContentAsync();
			var displayRightSum = await Fixture.Page.Locator(".right-point-sum").TextContentAsync();
			var displayLeftPoints = await Fixture.Page.Locator(".left-points").TextContentAsync();
			var displayRightPoints = await Fixture.Page.Locator(".right-points").TextContentAsync();

			// Expected values
			var expectedLeftStr = expectedTurnsLeft?.Count > 0 ? string.Join("-", expectedTurnsLeft) : "";
			var expectedRightStr = expectedTurnsRight?.Count > 0 ? string.Join("-", expectedTurnsRight) : "";
			int expectedLeftSum = expectedTurnsLeft?.Sum() ?? 0;
			int expectedRightSum = expectedTurnsRight?.Sum() ?? 0;

			// Validate left turns
			Assert.Equal(expectedLeftStr, displayLeftPoints?.Trim() ?? "");

			// Validate right turns
			Assert.Equal(expectedRightStr, displayRightPoints?.Trim() ?? "");

			// Validate sums
			Assert.NotNull(displayLeftSum);
			Assert.NotNull(displayRightSum);

			if (!int.TryParse(displayLeftSum.Trim(), out int displayedLeftSum))
				displayedLeftSum = 0;
			if (!int.TryParse(displayRightSum.Trim(), out int displayedRightSum))
				displayedRightSum = 0;

			Assert.Equal(expectedLeftSum, displayedLeftSum);
			Assert.Equal(expectedRightSum, displayedRightSum);

			Log(CurrentPhase, $"✓ Kehren + Summen korrekt: Links {expectedLeftStr}={expectedLeftSum} | Rechts {expectedRightStr}={expectedRightSum}", "✓");
		}

		// Validate Team Names
		// ENTWEDER beide Teamnamen sind vorhanden ODER keine
		var displayedLeftElements = await Fixture.Page.Locator(".teamname-left").AllAsync();
		var displayedRightElements = await Fixture.Page.Locator(".teamname-right").AllAsync();

		bool hasExpectedTeams = !string.IsNullOrEmpty(expectedTeamLeft) && !string.IsNullOrEmpty(expectedTeamRight);
		bool hasDisplayedTeams = displayedLeftElements.Count > 0 && displayedRightElements.Count > 0;

		// Überprüfe dass beide oder keine vorhanden sind (Konsistenz)
		Assert.Equal(hasExpectedTeams, hasDisplayedTeams);

		if (hasExpectedTeams && hasDisplayedTeams)
		{
			var displayedLeft = await Fixture.Page.Locator(".teamname-left").TextContentAsync();
			var displayedRight = await Fixture.Page.Locator(".teamname-right").TextContentAsync();

			Assert.Equal(expectedTeamLeft, displayedLeft?.Trim() ?? "");
			Assert.Equal(expectedTeamRight, displayedRight?.Trim() ?? "");

			Log(CurrentPhase, $"✓ Team-Namen korrekt: {expectedTeamLeft} vs {expectedTeamRight}", "✓");
		}
		else if (!hasExpectedTeams && !hasDisplayedTeams)
		{
			Log(CurrentPhase, $"✓ Keine Team-Namen angezeigt (korrekt)", "✓");
		}

		// Validate Header (Game Number and Turn Number)
		// Hinweis: Header wird angezeigt, sobald im ersten Spiel eine Kehre eingegeben wird, und bleibt dann sichtbar
		if (expectedGameNumber.HasValue && (expectedTurnNumber == null || expectedTurnNumber > 0))
		{
			var headerText = await Fixture.Page.Locator(".score-row.header-text").TextContentAsync();
			Assert.NotNull(headerText);
			Assert.NotEmpty(headerText);

			// Im Training wird nur Bahn + Kehre angezeigt, im Turnier Spiel + Kehre
			// GameplayScriptHelpers.ParseHeaderSpielUndKehre() gibt (0, kehre) zurück wenn "Spiel" nicht vorhanden ist
			var (displayedGameNumber, displayedTurnNumber) = GameplayScriptHelpers.ParseHeaderSpielUndKehre(headerText);

			if (expectedGameNumber.HasValue && expectedGameNumber > 1)
			{
				Assert.Equal(expectedGameNumber.Value, displayedGameNumber);
				Log(CurrentPhase, $"✓ Header korrekt: Spiel {displayedGameNumber}, Kehre {displayedTurnNumber}", "✓");
			}
			else if (expectedGameNumber == 1)
			{
				// Im Training: nur Kehre validieren
				if (expectedTurnNumber.HasValue)
				{
					Assert.Equal(expectedTurnNumber.Value, displayedTurnNumber);
				}
				Log(CurrentPhase, $"✓ Header korrekt: Kehre {displayedTurnNumber}", "✓");
			}
		}
	}
}
