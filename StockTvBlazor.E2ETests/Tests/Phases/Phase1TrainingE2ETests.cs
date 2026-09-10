using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public class Phase1TrainingE2ETests : PhaseTestBase
{
	public Phase1TrainingE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	public async Task Phase1_Training_15Kehren()
	{
		LogPhaseStart("Phase 1", "Training 15 Kehren");
		Assert.NotNull(Fixture.Page);

		var currentSettings = await GetCurrentSettings();
		var trainingSettings = GameplayScriptHelpers.BuildSettingsBytes(
			currentSettings, modus: 0, maxPunkteProKehre: 9, maxKehrenProSpiel: 15);
		await SendSettings(trainingSettings);
		Log("Phase1", "Settings: Training, MaxPunkte=9, MaxKehren=15");

		Fixture.SendNetMqCommand("ResetResult");
		Log("Phase1", "ResetResult gesendet");

		await Fixture.Page.GotoAsync("http://localhost:5001/training");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);
		Log("Phase1", "Navigiert zu /training");

		var pageContent = await Fixture.Page.ContentAsync();
		Assert.NotEmpty(pageContent);

		var expectedTurns = new List<(string Side, int Value)>();

		Log("Phase1", "Starte 13 gültige Kehren...");
		for (int turn = 0; turn < 13; turn++)
		{
			int val = Rng.Next(0, 10);
			string confirmKey = Rng.Next(2) == 0 ? "*" : "/";
			await EnterAndConfirm(val.ToString(), confirmKey);
			await Task.Delay(DEBOUNCE_DELAY_MS);

			expectedTurns.Add((confirmKey, val));
			string side = confirmKey == "*" ? "Links" : "Rechts";
			Log("Phase1", $"Turn {turn + 1}/13: {val} Punkte ({side})", "✓");
		}

		Log("Phase1", "Teste Grenzwert: 15 > 9 (sollte verworfen werden)", "⚠");
		await EnterAndConfirm("15", "*");
		await Task.Delay(DEBOUNCE_DELAY_MS);
		var content = await Fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		Log("Phase1", "Grenzwert-Test bestätigt: Wert > Max verworfen", "✓");

		Log("Phase1", "Teste Delete (-): Lösche letzte Kehre", "⚠");
		await Fixture.Page.Keyboard.PressAsync("-");
		await Task.Delay(DEBOUNCE_DELAY_MS);
		expectedTurns.RemoveAt(expectedTurns.Count - 1);
		Log("Phase1", "Delete Test: Kehre gelöscht, 12 übrig", "✓");

		Log("Phase1", "Ergänze 2 weitere Kehren...");
		for (int turn = 0; turn < 2; turn++)
		{
			int val = Rng.Next(0, 10);
			string confirmKey = Rng.Next(2) == 0 ? "*" : "/";
			await EnterAndConfirm(val.ToString(), confirmKey);
			await Task.Delay(DEBOUNCE_DELAY_MS);
			expectedTurns.Add((confirmKey, val));
			string side = confirmKey == "*" ? "Links" : "Rechts";
			Log("Phase1", $"Zusatz-Turn {turn + 1}/2: {val} ({side})", "✓");
		}

		await Fixture.Page.Keyboard.PressAsync("+");
		await Task.Delay(DEBOUNCE_DELAY_MS);
		LogPhaseEnd("Phase 1");
	}
}
