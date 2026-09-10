using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public class Phase2TournamentE2ETests : PhaseTestBase
{
	public Phase2TournamentE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	public async Task Phase2_Turnier_3Spiele()
	{
		CurrentPhase = "Phase 2";
		LogPhaseStart(CurrentPhase, "Turnier 3 Spiele");
		Assert.NotNull(Fixture.Page);

		// Konstanten für Test-Parameter
		const int maxPunkteProKehre = 6;
		const int maxKehrenProSpiel = 6;
		const int numberOfGames = 3;

		// Team-Namen dynamisch generieren
		var teamNamesMap = new Dictionary<int, (string left, string right)>();
		for (int i = 1; i <= numberOfGames; i++)
		{
			teamNamesMap[i] = ($"TeamA{i}", $"TeamB{i}");
		}

		var currentSettings = await GetCurrentSettings();
		var turnierSettings = GameplayScriptHelpers.BuildSettingsBytes(
			currentSettings, modus: 2, maxPunkteProKehre: maxPunkteProKehre, maxKehrenProSpiel: maxKehrenProSpiel, richtung: 1);
		await SendSettings(turnierSettings);
		Log(CurrentPhase, $"Settings: Turnier, MaxPunkte={maxPunkteProKehre}, MaxKehren={maxKehrenProSpiel}, Richtung=1");
		await Task.Delay(500);

		await Fixture.Page.GotoAsync("http://localhost:5001/turnier");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);

		Fixture.ClearPublisherMessages();
		Fixture.SendNetMqCommand("ResetResult");
		Log(CurrentPhase, "ResetResult gesendet");
		await Task.Delay(500);

		// An NetMQ senden (Format: "Spielnr:TeamA:TeamB;...")
		var teamNamesPayload = string.Join(";", teamNamesMap.Select(kvp =>
			$"{kvp.Key}:{kvp.Value.left}:{kvp.Value.right}"));

		Fixture.SendNetMqCommand("SetTeamNames", teamNamesPayload);

		Log(CurrentPhase, $"Team-Namen gesetzt ({numberOfGames} Begegnungen)");
		Log(CurrentPhase, "Navigiert zu /turnier");

		var allGames = new Dictionary<int, (List<int> turnsLeft, List<int> turnsRight)>();

		for (int game = 1; game <= numberOfGames; game++)
		{
			Log(CurrentPhase, $"▶ Starte Spiel {game}/{numberOfGames}");
			// Erwartete Teamnamen aus Variable abrufen
			var (expectedTeamLeft, expectedTeamRight) = teamNamesMap[game];

			// Validiere Teamnamen beim Spiel-Start
			await ValidateDisplayAsync(
				expectedTeamLeft: expectedTeamLeft,
				expectedTeamRight: expectedTeamRight,
				expectedGameNumber: game,
				expectedTurnNumber: 0);

			Log(CurrentPhase, $"  ✓ Team-Namen korrekt: {expectedTeamLeft} vs {expectedTeamRight}");

			var turnsLeft = new List<int>();
			var turnsRight = new List<int>();

			int validTurnsCount = 0;
			while (validTurnsCount < maxKehrenProSpiel)
			{
				int val;

				if (validTurnsCount < maxKehrenProSpiel - 2)
				{
					val = Rng.Next(0, maxPunkteProKehre + 1);
				}
				else if (validTurnsCount == maxKehrenProSpiel - 2)
				{
					// Ungültigen Wert testen und verwerfen
					int invalidVal = Rng.Next(maxPunkteProKehre + 1, maxPunkteProKehre + 5);
					var invalidResult = await EnterAndConfirm(invalidVal.ToString(), expectedDisplay: "");
					Log(CurrentPhase, $"  !!! Ungueltiger Wert {invalidVal} gesendet und verworfen");
					await Task.Delay(500);

					// Validiere dass die Anzeige unverändert ist (der ungültige Wert wurde verworfen)
					await ValidateDisplayAsync(
						expectedTurnsLeft: turnsLeft,
						expectedTurnsRight: turnsRight,
						expectedTeamLeft: expectedTeamLeft,
						expectedTeamRight: expectedTeamRight,
						expectedGameNumber: game,
						expectedTurnNumber: validTurnsCount);

					allGames[game] = (new List<int>(turnsLeft), new List<int>(turnsRight));
					await ValidateNetMqPublisherCompleteStateAsync(allGames);

					// Danach einen gültigen Wert senden
					val = Rng.Next(0, maxPunkteProKehre + 1);
				}
				else
				{
					val = Rng.Next(0, maxPunkteProKehre + 1);
				}

				var result = await EnterAndConfirm(val.ToString());
				await Task.Delay(500);

				TrackTurn(result, val, turnsLeft, turnsRight);
				string side = result.IsLeftSide ? "Links (*)" : "Rechts (/)";
				Log(CurrentPhase, $"  Turn {validTurnsCount + 1}/{maxKehrenProSpiel}: Wert {val} fuer {side} eingegeben");

				// Validiere die Anzeige nach jedem Entry
				await ValidateDisplayAsync(
					expectedTurnsLeft: turnsLeft,
					expectedTurnsRight: turnsRight,
					expectedTeamLeft: expectedTeamLeft,
					expectedTeamRight: expectedTeamRight,
					expectedGameNumber: game,
					expectedTurnNumber: validTurnsCount + 1);

				allGames[game] = (new List<int>(turnsLeft), new List<int>(turnsRight));
				await ValidateNetMqPublisherCompleteStateAsync(allGames);

				validTurnsCount++;
			}

			Log(CurrentPhase, $"  ✓ Spiel {game}/{numberOfGames}: Schleife beendet mit {maxKehrenProSpiel} Kehren");

			// Test: Letzte Kehre löschen und neue hinzufügen
			if (validTurnsCount > 0)
			{
				// Lösche das letzte Paar (Wert + 0) von beiden Seiten
				if (turnsLeft.Count > 0)
					turnsLeft.RemoveAt(turnsLeft.Count - 1);
				if (turnsRight.Count > 0)
					turnsRight.RemoveAt(turnsRight.Count - 1);

				await Fixture.Page.Keyboard.PressAsync("-");
				await Task.Delay(DEBOUNCE_DELAY_MS);
				Log(CurrentPhase, $"  - Last turn removed");
				await Task.Delay(500);

				// Validiere nach dem Löschen
				await ValidateDisplayAsync(
					expectedTurnsLeft: turnsLeft,
					expectedTurnsRight: turnsRight,
					expectedTeamLeft: expectedTeamLeft,
					expectedTeamRight: expectedTeamRight,
					expectedGameNumber: game,
					expectedTurnNumber: validTurnsCount - 1);

				allGames[game] = (new List<int>(turnsLeft), new List<int>(turnsRight));
				await ValidateNetMqPublisherCompleteStateAsync(allGames);

				int val = Rng.Next(0, maxPunkteProKehre + 1);
				var addResult = await EnterAndConfirm(val.ToString());
				await Task.Delay(500);

				TrackTurn(addResult, val, turnsLeft, turnsRight);
				string addSide = addResult.IsLeftSide ? "Links" : "Rechts";
				Log(CurrentPhase, $"  + New turn added ({addSide}): {val}");

				// Validiere die Anzeige nach dem Add
				await ValidateDisplayAsync(
					expectedTurnsLeft: turnsLeft,
					expectedTurnsRight: turnsRight,
					expectedTeamLeft: expectedTeamLeft,
					expectedTeamRight: expectedTeamRight,
					expectedGameNumber: game,
					expectedTurnNumber: validTurnsCount);

				allGames[game] = (new List<int>(turnsLeft), new List<int>(turnsRight));
				await ValidateNetMqPublisherCompleteStateAsync(allGames);
			}

			await Fixture.Page.Keyboard.PressAsync("+");
			await Task.Delay(500);
			Log(CurrentPhase, $"  ✓ Spiel {game}/{numberOfGames} abgeschlossen mit Taste '+'");
		}

		// Im nächsten Spiel (das nicht existiert) sollten keine Team-Namen angezeigt werden
		int expectedNextGame = numberOfGames + 1;
		await ValidateDisplayAsync(
			expectedGameNumber: expectedNextGame,
			expectedTurnNumber: null,
			expectedTeamLeft: null,
			expectedTeamRight: null);

		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(1000);
		LogPhaseEnd(CurrentPhase);
	}
}
