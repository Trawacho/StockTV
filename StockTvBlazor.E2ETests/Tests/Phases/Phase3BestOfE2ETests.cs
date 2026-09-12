using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase3BestOfE2ETests : PhaseTestBase
{
	public Phase3BestOfE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	[Fact]
	public async Task Phase3_BestOf_3Spiele()
	{
		CurrentPhase = "Phase 3";
		LogPhaseStart(CurrentPhase, "BestOf 3 Spiele");
		Assert.NotNull(Fixture.Page);

		// Konstanten für Test-Parameter
		const int maxPunkteProKehre = 8;
		const int maxKehrenProSpiel = 6;
		const int numberOfGames = 3;

		// Team-Namen dynamisch generieren
		var teamNamesMap = new Dictionary<int, (string left, string right)>();
		for (int i = 1; i <= numberOfGames; i++)
		{
			teamNamesMap[i] = ($"TeamA{i}", $"TeamB{i}");
		}

		Log(CurrentPhase, $"Test-Parameter: MaxPunkte={maxPunkteProKehre}, MaxKehren={maxKehrenProSpiel}, Spiele={numberOfGames}");
		await ConfigureAndValidateSettings(
			modus: 1,
			maxPunkteProKehre: maxPunkteProKehre,
			maxKehrenProSpiel: maxKehrenProSpiel,
			richtung: 1);

		await Fixture.Page.GotoAsync("http://localhost:5001/bestof");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1500);
		Log(CurrentPhase, "Navigiert zu /bestof");

		Fixture.ClearPublisherMessages();

		Log(CurrentPhase, "Sende ResetResult an Server...");
		Fixture.SendNetMqCommand("ResetResult");
		Log(CurrentPhase, "ResetResult gesendet");

		SendTeamNames(teamNamesMap);

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

			var turnsLeft = new List<int>();
			var turnsRight = new List<int>();

			int validTurnsCount = 0;
			while (validTurnsCount < maxKehrenProSpiel)
			{
				// Teste '+' Taste vor jedem Turn (außer erstem/letztem) - sollte keine Auswirkung haben
				if (validTurnsCount > 0 && validTurnsCount < maxKehrenProSpiel)
				{
					await PressKeyAsync("+", $"Taste '+' gedrückt (vor der letzten Kehre)");
				}

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

				await PressKeyAsync("-", "Last turn removed");

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

			await PressKeyAsync("+", $"✓ Spiel {game}/{numberOfGames} abgeschlossen mit Taste '+'");
			await ValidateGameSummaryAsync(game, allGames);
			await ValidateMatchPointsAsync(game, expectedTeamLeft, expectedTeamRight);
			
		}

		// Im nächsten Spiel (das nicht existiert) sollten keine Team-Namen angezeigt werden
		int expectedNextGame = numberOfGames + 1;
		await ValidateDisplayAsync(
			expectedGameNumber: expectedNextGame);

		Fixture.SendNetMqCommand("ResetResult");
		LogPhaseEnd(CurrentPhase);
	}
}
