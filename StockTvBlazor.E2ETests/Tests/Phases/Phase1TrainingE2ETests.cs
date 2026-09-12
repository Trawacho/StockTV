using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase1TrainingE2ETests : PhaseTestBase
{
	public Phase1TrainingE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	[Fact]
	public async Task Phase1_Training_15Kehren()
	{

		const int maxPunkteProKehre = 9;
		const int maxKehrenProSpiel = 15;
		const int numberOfValidTurns = 13;
		const int numberOfAdditionalTurns = 2;
		int invalidValue = maxPunkteProKehre + 6;

		CurrentPhase = "Phase 1";
		LogPhaseStart(CurrentPhase, $"Training {maxKehrenProSpiel} Kehren");
		Assert.NotNull(Fixture.Page);

		Log(CurrentPhase, $"Starte Training mit {numberOfValidTurns} gültigen Kehren und {numberOfAdditionalTurns} zusätzlichen Kehren (MaxPunkte={maxPunkteProKehre}, MaxKehren={maxKehrenProSpiel})");
		await ConfigureAndValidateSettings(
			modus: 0,
			maxPunkteProKehre: maxPunkteProKehre,
			maxKehrenProSpiel: maxKehrenProSpiel);

		await Fixture.Page.GotoAsync("http://localhost:5001/training");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1500);
		Log(CurrentPhase, "Navigiert zu /training");

		Log(CurrentPhase, "Sende ResetResult an Server...");
		Fixture.SendNetMqCommand("ResetResult");
		Log(CurrentPhase, "ResetResult gesendet");

		var pageContent = await Fixture.Page.ContentAsync();
		Assert.NotEmpty(pageContent);

		var turnsLeft = new List<int>();
		var turnsRight = new List<int>();

		// Bestimme zufällig, bei welcher Kehre ein Reset stattfinden soll (zwischen 3 und numberOfValidTurns-2)
		int resetAtTurn = numberOfValidTurns > 4 ? Rng.Next(3, numberOfValidTurns - 1) : -1;
		if (resetAtTurn > 0)
		{
			Log(CurrentPhase, $"Starte {numberOfValidTurns} gültige Kehren (mit Reset bei Kehre {resetAtTurn})...");
		}
		else
		{
			Log(CurrentPhase, $"Starte {numberOfValidTurns} gültige Kehren (ohne Reset)...");
		}

		int turnCountSinceReset = 0;
		bool resetHasOccurred = false;

		for (int turn = 0; turn < numberOfValidTurns; turn++)
		{
			int val = Rng.Next(0, maxPunkteProKehre + 1);
			var result = await EnterAndConfirm(val.ToString());

			TrackTurn(result, val, turnsLeft, turnsRight);
			string side = result.IsLeftSide ? "Links" : "Rechts";
			Log(CurrentPhase, $"Kehre {turn + 1}/{numberOfValidTurns}: {val} Punkte ({side})", "✓");

			// Validiere die Anzeige nach jedem Turn
			await ValidateDisplayAsync(
				expectedTurnsLeft: turnsLeft,
				expectedTurnsRight: turnsRight,
				expectedGameNumber: 1,
				expectedTurnNumber: turnCountSinceReset + 1);

			turnCountSinceReset++;

			// Reset durchführen (nur 1x, gesichert durch resetHasOccurred Flag)
			if (!resetHasOccurred && turn + 1 == resetAtTurn)
			{
				await PressKeyAsync("+", $"Reset bei Kehre {turnCountSinceReset} mit Taste '+'");

				// Alles wird zurückgesetzt
				turnsLeft.Clear();
				turnsRight.Clear();
				turnCountSinceReset = 0;
				resetHasOccurred = true;

				Log(CurrentPhase, "Spiel wurde zurückgesetzt, beginne neues Trainingsspiel", "✓");
			}
		}

		// Test: Ungültiger Wert (> maxPunkteProKehre) sollte verworfen werden
		Log(CurrentPhase, $"Teste Grenzwert: {invalidValue} > {maxPunkteProKehre} (sollte verworfen werden)", "⚠");
		await EnterAndConfirm(invalidValue.ToString(), "*", expectedDisplay: "");

		// Validiere dass die Anzeige unverändert ist (der ungültige Wert wurde verworfen)
		await ValidateDisplayAsync(
			expectedTurnsLeft: turnsLeft,
			expectedTurnsRight: turnsRight,
			expectedGameNumber: 1,
			expectedTurnNumber: turnCountSinceReset);
		Log(CurrentPhase, "Grenzwert-Test bestätigt: Wert > Max verworfen", "✓");

		// Test: Delete (-)
		if (turnsLeft.Count > 0)
			turnsLeft.RemoveAt(turnsLeft.Count - 1);
		if (turnsRight.Count > 0)
			turnsRight.RemoveAt(turnsRight.Count - 1);

		await PressKeyAsync("-", "Teste Delete (-): Lösche letzte Kehre");

		turnCountSinceReset--;

		// Validiere nach dem Löschen
		await ValidateDisplayAsync(
			expectedTurnsLeft: turnsLeft,
			expectedTurnsRight: turnsRight,
			expectedGameNumber: 1,
			expectedTurnNumber: turnCountSinceReset);
		Log(CurrentPhase, $"Delete Test: Kehre gelöscht, {turnCountSinceReset} übrig", "✓");

		// Ergänze weitere Kehren
		Log(CurrentPhase, $"Ergänze {numberOfAdditionalTurns} weitere Kehren...");
		for (int turn = 0; turn < numberOfAdditionalTurns; turn++)
		{
			int val = Rng.Next(0, maxPunkteProKehre + 1);
			var result = await EnterAndConfirm(val.ToString());

			TrackTurn(result, val, turnsLeft, turnsRight);
			string addSide = result.IsLeftSide ? "Links" : "Rechts";
			Log(CurrentPhase, $"Zusatz-Turn {turn + 1}/{numberOfAdditionalTurns}: {val} ({addSide})", "✓");

			// Validiere die Anzeige nach jedem Turn (vor dem Inkrementieren wie in der Hauptschleife)
			await ValidateDisplayAsync(
				expectedTurnsLeft: turnsLeft,
				expectedTurnsRight: turnsRight,
				expectedGameNumber: 1,
				expectedTurnNumber: turnCountSinceReset + 1);

			turnCountSinceReset++;
		}

		await PressKeyAsync("+", "Spiel mit '+' bestätigt");

		LogPhaseEnd(CurrentPhase);
	}
}
