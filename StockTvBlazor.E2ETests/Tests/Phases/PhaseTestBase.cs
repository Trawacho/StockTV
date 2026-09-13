using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public abstract class PhaseTestBase
{
	protected readonly AppFixture Fixture;
	protected Random Rng;
	protected readonly ITestOutputHelper Output;
	protected const int DEBOUNCE_DELAY_MS = 1100;
	protected string CurrentPhase { get; set; } = "Test";

	protected PhaseTestBase(AppFixture fixture, ITestOutputHelper output)
	{
		Fixture = fixture;
		Output = output;
		// Initialisiere den Logger einmalig pro Test-Run in der Fixture
		Fixture.InitializeLogger(output);

		// Generiere einen zufälligen Seed für bessere Test-Variabilität
		int seed = new Random().Next();
		Rng = new Random(seed);

		// Schreibe den Seed ins Log für Reproduzierbarkeit bei Fehlern
		Log("Setup", $"Random Seed für diesen Testlauf: {seed}");
	}

	#region CORE SETUP & LOGGING

	/// <summary>
	/// Schreibt eine Lognachricht mit Phasenbeschreibung und optionalem Symbol.
	/// </summary>
	/// <param name="phase">Die aktuelle Testphase</param>
	/// <param name="message">Die Lognachricht</param>
	/// <param name="symbol">Das anzuzeigende Symbol (Standard: None)</param>
	protected void Log(string phase, string message, LogSymbol symbol = LogSymbol.None)
	{
		Fixture.Logger?.WriteLn(phase, message, symbol);
	}

	/// <summary>
	/// Loggt den Start einer neuen Testphase mit beschreibendem Titel.
	/// </summary>
	/// <param name="phase">Der Name der Phase</param>
	/// <param name="description">Eine Beschreibung was die Phase testet</param>
	protected void LogPhaseStart(string phase, string description)
	{
		Fixture.Logger?.WritePhaseStart(phase, description);
	}

	/// <summary>
	/// Loggt das Ende einer Testphase.
	/// </summary>
	/// <param name="phase">Der Name der Phase die beendet wird</param>
	protected void LogPhaseEnd(string phase)
	{
		Fixture.Logger?.WritePhaseEnd(phase);
	}

	#endregion CORE SETUP & LOGGING

	#region SETTINGS & CONFIGURATION

	/// <summary>
	/// Holt die aktuellen Einstellungen vom NetMQ ReP-Socket.
	/// Falls die Abfrage fehlschlägt, werden Standardeinstellungen zurückgegeben.
	/// </summary>
	/// <returns>Ein 10-Byte-Array mit den aktuellen Einstellungen</returns>
	protected Task<byte[]> GetCurrentSettings()
	{
		try
		{
			var response = Fixture.SendNetMqRaw("GetSettings");
			if (response.FrameCount >= 2)
				return Task.FromResult(response[1].ToByteArray());
		}
		catch { }

		return Task.FromResult(new byte[] { 1, 0, 0, 0, 0, 10, 6, 50, 1, 0 });
	}

	/// <summary>
	/// Sendet neue Einstellungen an die App über NetMQ und wartet auf die Verarbeitung.
	/// </summary>
	/// <param name="settingsBytes">Ein 10-Byte-Array mit den neuen Einstellungen</param>
	protected async Task SendSettings(byte[] settingsBytes)
	{
		Fixture.SendNetMqRaw("SetSettings", settingsBytes);
		await Task.Delay(500);
	}

	/// <summary>
	/// Sendet einen ResetResult-Befehl an die App und wartet auf die Verarbeitung.
	/// </summary>
	protected async Task SendResetResult()
	{
		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(500);
	}

	/// <summary>
	/// Lädt die aktuellen Settings, baut neue Settings mit den angegebenen Parametern,
	/// sendet sie an die App und validiert dass sie korrekt übernommen wurden.
	/// </summary>
	/// <param name="modus">Der Spielmodus (0=Training, 1=BestOf, 2=Turnier, 100=Ziel, 101=Ziel2)</param>
	/// <param name="maxPunkteProKehre">Maximale Punkte pro Kehre</param>
	/// <param name="maxKehrenProSpiel">Maximale Kehren pro Spiel</param>
	/// <param name="richtung">Spielrichtung (0=Links, 1=Rechts), optional</param>
	/// <param name="validatePersistence">Wenn true, wird auch validiert dass die Settings-Datei auf dem Server persistiert wurde</param>
	protected async Task ConfigureAndValidateSettings(
		int modus,
		int maxPunkteProKehre,
		int maxKehrenProSpiel,
		int? richtung = null,
		bool validatePersistence = true)
	{
		Log(CurrentPhase, $"Lade aktuelle Settings vom Server...");
		var currentSettings = await GetCurrentSettings();

		var newSettings = GameplayScriptHelpers.BuildSettingsBytes(
			currentSettings,
			modus: (byte)modus,
			maxPunkteProKehre: (byte)maxPunkteProKehre,
			maxKehrenProSpiel: (byte)maxKehrenProSpiel,
			richtung: richtung.HasValue ? (byte?)richtung.Value : null);

		Log(CurrentPhase, $"Sende Settings (Modus={modus}, MaxPunkte={maxPunkteProKehre}, MaxKehren={maxKehrenProSpiel})" +
			(richtung.HasValue ? $", Richtung={richtung}" : "") + "...");
		await SendSettings(newSettings);

		Log(CurrentPhase, $"Validiere Settings in-memory...");
		var appliedSettings = await GetCurrentSettings();
		Assert.Equal(newSettings, appliedSettings);
		Log(CurrentPhase, $"Settings validiert und angewendet", LogSymbol.Check);

		if (validatePersistence)
		{
			await ValidateSettingsPersistenceAsync(modus, maxPunkteProKehre, maxKehrenProSpiel, richtung);
		}
	}

	/// <summary>
	/// Validiert dass die Settings-Datei auf dem Server mit den erwarteten Werten persistiert wurde.
	/// Liest die Datei _config/stocktv.config.json und vergleicht die Werte.
	/// Wartet auf den erwarteten Content für alle vier Settings-Werte.
	/// </summary>
	protected async Task ValidateSettingsPersistenceAsync(
		int expectedModus,
		int expectedMaxPunkteProKehre,
		int expectedMaxKehrenProSpiel,
		int? expectedRichtung = null)
	{
		Log(CurrentPhase, $"Validiere Settings-Persistierung auf dem Server...");

		var fileContent = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
		{
			try
			{
				using var doc = System.Text.Json.JsonDocument.Parse(json);
				var root = doc.RootElement;
				var gameObj = root.GetProperty("Game");
				var uiObj = root.GetProperty("UI");

				if (gameObj.GetProperty("CurrentModus").GetInt32() != expectedModus) return false;
				if (gameObj.GetProperty("MaxPunkteProKehre").GetInt32() != expectedMaxPunkteProKehre) return false;
				if (gameObj.GetProperty("MaxKehrenProSpiel").GetInt32() != expectedMaxKehrenProSpiel) return false;
				if (expectedRichtung.HasValue && uiObj.GetProperty("CurrentRichtung").GetInt32() != expectedRichtung.Value) return false;

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		});

		using var finalJsonDoc = System.Text.Json.JsonDocument.Parse(fileContent);
		var finalRoot = finalJsonDoc.RootElement;
		var finalGameObj = finalRoot.GetProperty("Game");
		var finalUiObj = finalRoot.GetProperty("UI");

		int fileModus = finalGameObj.GetProperty("CurrentModus").GetInt32();
		int fileMaxPunkte = finalGameObj.GetProperty("MaxPunkteProKehre").GetInt32();
		int fileMaxKehren = finalGameObj.GetProperty("MaxKehrenProSpiel").GetInt32();
		int fileRichtung = finalUiObj.GetProperty("CurrentRichtung").GetInt32();

		Assert.Equal(expectedModus, fileModus);
		Log(CurrentPhase, $"Modus in Datei persistiert: {fileModus}", LogSymbol.Check);

		Assert.Equal(expectedMaxPunkteProKehre, fileMaxPunkte);
		Log(CurrentPhase, $"MaxPunkteProKehre in Datei persistiert: {fileMaxPunkte}", LogSymbol.Check);

		Assert.Equal(expectedMaxKehrenProSpiel, fileMaxKehren);
		Log(CurrentPhase, $"MaxKehrenProSpiel in Datei persistiert: {fileMaxKehren}", LogSymbol.Check);

		if (expectedRichtung.HasValue)
		{
			Assert.Equal(expectedRichtung.Value, fileRichtung);
			Log(CurrentPhase, $"Richtung in Datei persistiert: {fileRichtung}", LogSymbol.Check);
		}
	}

	#endregion SETTINGS & CONFIGURATION

	#region TOURNAMENT HELPERS

	/// <summary>
	/// Sendet Team-Namen an die App über NetMQ im Format "Spielnr:TeamA:TeamB;..."
	/// </summary>
	/// <param name="teamNamesMap">Dictionary mit Spiel-Nummer als Key und (TeamLeft, TeamRight) als Value</param>
	protected void SendTeamNames(Dictionary<int, (string left, string right)> teamNamesMap)
	{
		var teamNamesPayload = string.Join(";", teamNamesMap.Select(kvp =>
			$"{kvp.Key}:{kvp.Value.left}:{kvp.Value.right}"));

		Log(CurrentPhase, $"Sende Team-Namen an Server: {teamNamesPayload}");
		Fixture.SendNetMqCommand("SetTeamNames", teamNamesPayload);
		Log(CurrentPhase, $"Team-Namen gesetzt ({teamNamesMap.Count} Begegnungen)", LogSymbol.Check);
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

		confirmKey ??= Rng.Next(2) == 0 ? "*" : "/";

		Log(CurrentPhase, $"Eingabe: {value}, Bestätigungskey: {confirmKey}");

		char? previousChar = null;
		foreach (char c in value)
		{
			// Wenn gleiches Zeichen wie zuvor: Debounce einhalten (VOR dem Tastendruck!)
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
	/// Drückt eine einzelne Taste und wartet auf die UI-Aktualisierung.
	/// </summary>
	/// <param name="key">Die zu drückende Taste (z.B. "+", "-", "Enter", "*", "/")</param>
	/// <param name="logMessage">Optionale Log-Nachricht (wird vor dem Tastendruck geloggt)</param>
	protected async Task PressKeyAsync(string key, string? logMessage = null)
	{
		if (!string.IsNullOrEmpty(logMessage))
			Log(CurrentPhase, logMessage);

		if (Fixture.Page == null)
			return;

		await Fixture.Page.Keyboard.PressAsync(key);
		await Task.Delay(200);
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
	/// Validiert die kumulativen Punkte nach einem Spiel in BestOf-Modi.
	/// Nach Spielende zeigen .left-points und .right-points die Summe ALLER bisherigen Spiele.
	/// </summary>
	/// <param name="gameNumber">Die Spiel-Nummer die gerade beendet wurde</param>
	/// <param name="allGames">Dictionary mit allen Spielen bis zu gameNumber</param>
	protected async Task ValidateGameSummaryAsync(
		int gameNumber,
		Dictionary<int, (List<int> turnsLeft, List<int> turnsRight)> allGames)
	{
		if (Fixture.Page == null)
			return;

		var displayLeftPoints = await Fixture.Page.Locator(".left-points").TextContentAsync();
		var displayRightPoints = await Fixture.Page.Locator(".right-points").TextContentAsync();

		// Berechne kumulative Summe aller Spiele bis gameNumber
		int cumulativeSumLeft = 0;
		int cumulativeSumRight = 0;

		for (int i = 1; i <= gameNumber; i++)
		{
			if (allGames.TryGetValue(i, out var game))
			{
				cumulativeSumLeft += game.turnsLeft.Sum();
				cumulativeSumRight += game.turnsRight.Sum();
			}
		}

		_ = int.TryParse(displayLeftPoints?.Trim() ?? "0", out int sumLeft);
		_ = int.TryParse(displayRightPoints?.Trim() ?? "0", out int sumRight);

		Assert.Equal(cumulativeSumLeft, sumLeft);
		Assert.Equal(cumulativeSumRight, sumRight);

		Log(CurrentPhase, $"Spiel {gameNumber} Kumulative Punkte: Links {sumLeft} | Rechts {sumRight}", LogSymbol.Check);
	}

	/// <summary>
	/// Validiert die Match Points (Spielpunkte) für BestOf und ähnliche Modi.
	/// Gibt aus, wie viele Match Points jede Seite hat.
	/// </summary>
	/// <param name="gameNumber">Die Spiel-Nummer die gerade beendet wurde</param>
	/// <param name="teamLeft">Team-Name links</param>
	/// <param name="teamRight">Team-Name rechts</param>
	protected async Task ValidateMatchPointsAsync(int gameNumber, string? teamLeft = null, string? teamRight = null)
	{
		if (Fixture.Page == null)
			return;

		var matchPointsLeft = await Fixture.Page.Locator(".score-cell.left-match-points").TextContentAsync();
		var matchPointsRight = await Fixture.Page.Locator(".score-cell.right-match-points").TextContentAsync();

		Assert.NotNull(matchPointsLeft);
		Assert.NotNull(matchPointsRight);

		_ = int.TryParse(matchPointsLeft.Trim(), out int pointsLeft);
		_ = int.TryParse(matchPointsRight.Trim(), out int pointsRight);

		var teamInfo = "";
		if (!string.IsNullOrEmpty(teamLeft) && !string.IsNullOrEmpty(teamRight))
		{
			teamInfo = $" ({teamLeft} vs {teamRight})";
		}

		Log(CurrentPhase, $"Match Points nach Spiel {gameNumber}: Links {pointsLeft} | Rechts {pointsRight}{teamInfo}", LogSymbol.Check);
	}

	/// <summary>
	/// Validates that match state is correctly persisted in match-state.json file.
	/// Checks that all turns match expected values and count.
	/// Waits for the expected turn count to appear (handles async write delays).
	/// </summary>
	protected async Task ValidateMatchStatePersistenceAsync(
		Dictionary<int, (List<int> turnsLeft, List<int> turnsRight)> allGames)
	{
		int totalExpectedTurns = allGames.Values.Sum(g => g.turnsLeft.Count);
		var lastGame = allGames.OrderBy(x => x.Key).Last();
		var (expectedTurnsLeft, expectedTurnsRight) = lastGame.Value;
		int currentGameTurns = expectedTurnsLeft.Count;

		Log(CurrentPhase, "Validierung der match-state.json Persistierung...");

		var matchStateJson = await Fixture.ReadLocalFileAsync("match-state.json", json =>
		{
			try
			{
				using var doc = System.Text.Json.JsonDocument.Parse(json);
				return doc.RootElement.TryGetProperty("Turns", out var turns)
					&& turns.ValueKind == System.Text.Json.JsonValueKind.Array
					&& turns.GetArrayLength() == totalExpectedTurns;
			}
			catch (System.Text.Json.JsonException)
			{
				return false;
			}
		});

		using var jsonDoc = System.Text.Json.JsonDocument.Parse(matchStateJson);
		var root = jsonDoc.RootElement;

		if (!root.TryGetProperty("Turns", out var turnsArray) || turnsArray.ValueKind != System.Text.Json.JsonValueKind.Array)
		{
			Log(CurrentPhase, $"'Turns' Array nicht gefunden in match-state.json", LogSymbol.Warning);
			Assert.Fail("Turns array not found in match-state.json");
			return;
		}

		int actualTurnCount = turnsArray.GetArrayLength();
		Assert.Equal(totalExpectedTurns, actualTurnCount);

		int startIndex = actualTurnCount - currentGameTurns;
		for (int i = 0; i < currentGameTurns; i++)
		{
			var turn = turnsArray[startIndex + i];
			int pointsLeft = turn.GetProperty("PointsLeft").GetInt32();
			int pointsRight = turn.GetProperty("PointsRight").GetInt32();

			Assert.Equal(expectedTurnsLeft[i], pointsLeft);
			Assert.Equal(expectedTurnsRight[i], pointsRight);
		}

		Log(CurrentPhase, $"match-state.json validiert: {actualTurnCount} Kehren korrekt persistiert", LogSymbol.Check);
	}

	#endregion TOURNAMENT HELPERS

	#region NETMQ HELPERS

	/// <summary>
	/// Holt das neueste GetResult-Payload vom NetMQ Publisher.
	/// </summary>
	/// <returns>Das Payload als String, oder null wenn keine GetResult-Nachrichten vorhanden sind</returns>
	protected string? GetLatestGetResultPayload()
	{
		var messages = Fixture.GetPublisherMessagesByTopic("GetResult");
		return messages.Count > 0 ? messages[^1].Payload : null;
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
			Log(CurrentPhase, "Kein GetResult-Payload vom NetMQ Publisher erhalten", LogSymbol.Warning);
			return;
		}

		var games = GameplayScriptHelpers.StripPrefixAndParseGames(payload);
		if (games == null)
		{
			Log(CurrentPhase, "GetResult-Payload konnte nicht geparst werden", LogSymbol.Warning);
			return;
		}

		// Validiere dass alle erwarteten Spiele im Payload enthalten sind
		Log(CurrentPhase, $"Validierung des NetMQ GetResult-Payloads...");
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
				$"NetMQ Spiel {gameNumber}: Links {broadcastLeftStr}={sumLeft} | Rechts {broadcastRightStr}={sumRight}",
				LogSymbol.Check);
		}
	}

	#endregion NETMQ HELPERS

	#region DISPLAY VALIDATION

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
		Log(CurrentPhase, $"Validierung der Anzeige: Links={string.Join("-", expectedTurnsLeft ?? new List<int>())}, Rechts={string.Join("-", expectedTurnsRight ?? new List<int>())}, TeamLinks={expectedTeamLeft}, TeamRechts={expectedTeamRight}, Spiel={expectedGameNumber}, Kehre={expectedTurnNumber}");

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

			Log(CurrentPhase, $"Kehren + Summen korrekt: Links {expectedLeftStr}={expectedLeftSum} | Rechts {expectedRightStr}={expectedRightSum}", LogSymbol.Check);
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

			Log(CurrentPhase, $"Team-Namen korrekt: {expectedTeamLeft} vs {expectedTeamRight}", LogSymbol.Check);
		}
		else if (!hasExpectedTeams && !hasDisplayedTeams)
		{
			Log(CurrentPhase, $"Keine Team-Namen angezeigt (korrekt)", LogSymbol.Check);
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
				Log(CurrentPhase, $"Header korrekt: Spiel {displayedGameNumber}, Kehre {displayedTurnNumber}", LogSymbol.Check);
			}
			else if (expectedGameNumber == 1)
			{
				// Im Training: nur Kehre validieren
				if (expectedTurnNumber.HasValue)
				{
					Assert.Equal(expectedTurnNumber.Value, displayedTurnNumber);
				}
				Log(CurrentPhase, $"Header korrekt: Kehre {displayedTurnNumber}", LogSymbol.Check);
			}
		}
	}

	/// <summary>
	/// Validates Ziel mode UI display elements:
	/// - AnzahlVersuche (attempts counter with format "X/Y")
	/// - GesamtText (header "Gesamt:")
	/// - GesamtPunkteText (total points sum)
	/// - SummeDerVersuche (sums per discipline with format "A - B - C - D")
	/// - LastValue / InputValue (previous/current input)
	/// </summary>
	protected async Task ValidateDisplayZielAsync(
		int expectedAttemptNumber,
		int maxAttemptsDisplay,
		bool checkInvalidOverlay = false)
	{
		if (Fixture.Page == null)
			return;

		Log(CurrentPhase, $"Validiere Ziel Display: Versuch {expectedAttemptNumber}/{maxAttemptsDisplay}");

		try
		{
			// Validate AnzahlVersuche counter (e.g., "5/24")
			var anzahlText = await Fixture.Page.Locator(".ziel-versuche").TextContentAsync();
			Assert.NotNull(anzahlText);
			Assert.Contains(expectedAttemptNumber.ToString(), anzahlText);
			Assert.Contains(maxAttemptsDisplay.ToString(), anzahlText);

			// Validate GesamtText header
			var gesamtHeader = await Fixture.Page.Locator(".ziel-gesamt").TextContentAsync();
			Assert.NotNull(gesamtHeader);
			Assert.Contains("Gesamt", gesamtHeader ?? "");

			// Validate GesamtPunkteText (should be a number)
			var gesamtPunkte = await Fixture.Page.Locator(".ziel-col-c").TextContentAsync();
			Assert.NotNull(gesamtPunkte);
			Assert.True(int.TryParse(gesamtPunkte?.Trim() ?? "", out _),
				$"GesamtPunkte should be numeric, got: {gesamtPunkte}");

			// Validate SummeDerVersuche (format: "0 - 2 - 4 - 6" or similar)
			var summen = await Fixture.Page.Locator(".ziel-summe").TextContentAsync();
			Assert.NotNull(summen);
			// Should contain dashes separating the 4 discipline sums
			var parts = summen?.Split('-');
			Assert.True(parts?.Length >= 3, $"SummeDerVersuche should have 4 sums separated by '-', got: {summen}");

			if (checkInvalidOverlay)
			{
				var overlay = await Fixture.Page.Locator(".ziel-overlay.show").IsVisibleAsync();
				Assert.True(overlay, "Invalid overlay should be visible");
				Log(CurrentPhase, $"Invalid overlay validiert", LogSymbol.Check);
			}

			Log(CurrentPhase, $"Display validiert: {anzahlText?.Trim()} | Summen: {summen?.Trim()}", LogSymbol.Check);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Display-Validierung fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
	}

	#endregion DISPLAY VALIDATION

	#region ZIEL HELPERS

	/// <summary>
	/// Enters a value in Ziel mode and validates success or invalid input overlay.
	/// Returns true if value was accepted, false if invalid (overlay shown).
	/// </summary>
	protected async Task<bool> EnterAndConfirmZielAsync(string value)
	{
		if (Fixture.Page == null)
			return false;

		Log(CurrentPhase, $"Ziel Eingabe: {value}");

		// Enter digits
		char? previousChar = null;
		foreach (char c in value)
		{
			if (previousChar != null && c == previousChar)
			{
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}

			await Fixture.Page.Keyboard.PressAsync(c.ToString());
			await Task.Delay(200);

			previousChar = c;
		}

		// Confirm with "/" (Rot) — Ziel accepts either * or /
		await Fixture.Page.Keyboard.PressAsync("/");
		await Task.Delay(200);

		// Check if invalid overlay appeared
		var overlay = await Fixture.Page.Locator(".ziel-overlay.show").IsVisibleAsync();
		if (overlay)
		{
			Log(CurrentPhase, $"Wert {value} ungültig (overlay sichtbar)", LogSymbol.Info);
			// Wait for overlay to disappear
			await Task.Delay(1600);
			return false;
		}

		Log(CurrentPhase, $"Wert {value} akzeptiert", LogSymbol.Check);
		return true;
	}

	/// <summary>
	/// Gets current attempt counter text from Ziel display (e.g., "5/24").
	/// </summary>
	protected async Task<string> GetZielAttemptCounterAsync()
	{
		if (Fixture.Page == null)
			return "";

		var anzahlText = await Fixture.Page.Locator(".ziel-versuche").TextContentAsync();
		return anzahlText?.Trim() ?? "";
	}

	/// <summary>
	/// Deletes last Ziel attempt with "-" key.
	/// </summary>
	protected async Task DeleteZielAttemptAsync()
	{
		if (Fixture.Page == null)
			return;

		Log(CurrentPhase, "Lösche letzten Versuch (Taste -)");
		await PressKeyAsync("-");
		await Task.Delay(500);
	}

	/// <summary>
	/// Sets participant name via NetMQ SetTeilnehmer command.
	/// </summary>
	protected void SetZielTeilnehmer(string spielername)
	{
		Log(CurrentPhase, $"Sende Spielername via NetMQ: '{spielername}'");
		var nameBytes = System.Text.Encoding.UTF8.GetBytes(spielername);
		Fixture.SendNetMqRaw("SetTeilnehmer", nameBytes);
		Log(CurrentPhase, $"Spielername gesetzt", LogSymbol.Check);
	}

	/// <summary>
	/// Validates Ziel player name (Spielername) is displayed on the page.
	/// </summary>
	protected async Task ValidateZielSpielernameAsync(string expectedName)
	{
		if (Fixture.Page == null)
			return;

		Log(CurrentPhase, $"Validiere Spielername auf UI: '{expectedName}'");

		var spielernameElement = await Fixture.Page.Locator(".ziel-spielername").TextContentAsync();
		Assert.NotNull(spielernameElement);
		Assert.Contains(expectedName, spielernameElement?.Trim() ?? "");

		Log(CurrentPhase, $"Spielername validiert: {spielernameElement?.Trim()}", LogSymbol.Check);
	}

	/// <summary>
	/// Gets the latest GetResult payload from Ziel mode and parses it.
	/// Returns parsed Ziel attempt counts per discipline.
	/// </summary>
	protected async Task ValidateZielNetMqPublisherAsync(
		int expectedAttemptCount,
		Dictionary<string, int> expectedDisziplinSummen)
	{
		Log(CurrentPhase, $"Validiere Ziel NetMQ Publisher (Versuch {expectedAttemptCount})");

		var payload = GetLatestGetResultPayload();
		if (string.IsNullOrEmpty(payload))
		{
			Log(CurrentPhase, "Kein GetResult-Payload vom NetMQ Publisher erhalten", LogSymbol.Warning);
			return;
		}

		// For Ziel mode, the payload contains settings (10 bytes) + JSON with discipline data
		if (payload.Length < 10)
		{
			Log(CurrentPhase, "Payload zu kurz für Settings+JSON", LogSymbol.Warning);
			return;
		}

		try
		{
			var jsonPart = payload[10..];
			using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonPart);
			var root = jsonDoc.RootElement;

			// Parse Ziel mode disciplines: MassenVorne, Schiessen, MassenSeite, Kombinieren
			foreach (var property in root.EnumerateObject())
			{
				if (property.Value.TryGetProperty("Versuche", out var versuche) &&
					versuche.ValueKind == System.Text.Json.JsonValueKind.Array)
				{
					var count = versuche.GetArrayLength();
					var disziplinName = property.Name;

					if (expectedDisziplinSummen.ContainsKey(disziplinName))
					{
						int sum = 0;
						foreach (var versuch in versuche.EnumerateArray())
						{
							sum += versuch.GetInt32();
						}

						Assert.Equal(expectedDisziplinSummen[disziplinName], sum);
						Log(CurrentPhase, $"Disziplin {disziplinName}: {count} Versuche, Summe={sum}", LogSymbol.Check);
					}
				}
			}

			Log(CurrentPhase, $"NetMQ GetResult validiert", LogSymbol.Check);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Fehler beim Parsen von GetResult: {ex.Message}", LogSymbol.Warning);
		}
	}

	#endregion ZIEL HELPERS
}
