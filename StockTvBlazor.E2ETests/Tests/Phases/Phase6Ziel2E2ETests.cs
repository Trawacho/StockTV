using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase6Ziel2E2ETests : PhaseTestBase
{
	public Phase6Ziel2E2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	[Fact]
	public async Task Phase6_Ziel2_2Runden()
	{
		CurrentPhase = "Phase 6";
		LogPhaseStart(CurrentPhase, "Ziel2 2 Runden (6 Kehren pro Disziplin)");
		Assert.NotNull(Fixture.Page);

		// Konstanten
		const int maxKehrenProSpiel = 6;
		const int maxVersucheProRunde = 4 * maxKehrenProSpiel;  // 24 per round
		const int maxVersucheDisplay = 2 * maxVersucheProRunde;  // 48 for display

		// Konfiguriere Ziel2-Modus (modus 101)
		Log(CurrentPhase, $"Starte Ziel2 mit 2 Runden à {maxKehrenProSpiel} Versuchen pro Disziplin");
		await ConfigureAndValidateSettings(
			modus: 101,
			maxPunkteProKehre: 10,
			maxKehrenProSpiel: maxKehrenProSpiel,
			validatePersistence: true);

		// Navigate to Ziel page
		await Fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1500);
		Log(CurrentPhase, "Navigiert zu /ziel");

		Fixture.ClearPublisherMessages();

		Log(CurrentPhase, "Sende ResetResult an Server...");
		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(500);

		// Set player name via NetMQ
		string spielername = "TestSpieler_Phase6";
		SetZielTeilnehmer(spielername);
		await Task.Delay(500);

		var validValuesPerDisziplin = new[]
		{
			new[] { 0, 2, 4, 6, 8, 10 },  // MassenVorne
			new[] { 0, 2, 5, 10 },         // Schiessen
			new[] { 0, 2, 4, 6, 8, 10 },  // MassenSeite
			new[] { 0, 2, 4, 6, 8, 10 }   // Kombinieren
		};

		string[] disziplinNamen = { "MassenVorne", "Schiessen", "MassenSeite", "Kombinieren" };

		// Validate player name is displayed
		await ValidateZielSpielernameAsync(spielername);

		int totalVersucheCount = 0;
		int currentRound = 1;
		var disziplinSummen = new Dictionary<string, int>
		{
			["MassenVorne"] = 0,
			["Schiessen"] = 0,
			["MassenSeite"] = 0,
			["Kombinieren"] = 0
		};
		var disziplinVersuche = new Dictionary<string, List<int>>
		{
			["MassenVorne"] = new List<int>(),
			["Schiessen"] = new List<int>(),
			["MassenSeite"] = new List<int>(),
			["Kombinieren"] = new List<int>()
		};
		int runde1Summe = 0;

		// RUNDE 1
		Log(CurrentPhase, "=== RUNDE 1 ===");

		for (int disziplin = 0; disziplin < 4; disziplin++)
		{
			var validValues = validValuesPerDisziplin[disziplin];
			Log(CurrentPhase, $"▶ Runde {currentRound}, Disziplin {disziplin + 1}/4: {disziplinNamen[disziplin]}");

			int versucheInDisziplin = 0;

			while (versucheInDisziplin < maxKehrenProSpiel)
			{
				int value;

				// Invalid value on 5. attempt
				if (versucheInDisziplin == maxKehrenProSpiel - 2)
				{
					int[] allValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 15];
					value = allValues.FirstOrDefault(v => !validValues.Contains(v));

					bool wasAccepted = await EnterAndConfirmZielAsync(value.ToString());
					Assert.False(wasAccepted, "Invalid value should be rejected");
					Log(CurrentPhase, $"Versuch {versucheInDisziplin + 1}/{maxKehrenProSpiel}: Ungültiger Wert {value} korrekt abgelehnt", LogSymbol.Check);

					await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);

					value = validValues[Rng.Next(validValues.Length)];
				}
				else
				{
					value = validValues[Rng.Next(validValues.Length)];
				}

				bool wasAccepted2 = await EnterAndConfirmZielAsync(value.ToString());
				Assert.True(wasAccepted2, "Valid value should be accepted");

				totalVersucheCount++;
				versucheInDisziplin++;
				disziplinSummen[disziplinNamen[disziplin]] += value;
				disziplinVersuche[disziplinNamen[disziplin]].Add(value);

				if (versucheInDisziplin % 2 == 0)
				{
					Log(CurrentPhase, $"  Versuch {versucheInDisziplin}/{maxKehrenProSpiel}: {value} ({totalVersucheCount}/{maxVersucheDisplay})");
					// Validate NetMQ less frequently
					await ValidateZielNetMqPublisherAsync(totalVersucheCount, disziplinSummen);
				}

				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);
				await Task.Delay(250);
			}

			Log(CurrentPhase, $"Runde {currentRound} Disziplin {disziplin + 1} fertig", LogSymbol.Check);
		}

		Log(CurrentPhase, $"Runde 1 abgeschlossen: {totalVersucheCount}/{maxVersucheProRunde} Versuche", LogSymbol.Check);

		// Validate Runde 1 before transition
		runde1Summe = disziplinSummen.Values.Sum();
		await ValidateZielStatePersistenceAsync(disziplinVersuche, expectedRunde: 1, expectedRunde1Summe: 0);

		await Task.Delay(1000);  // Pause for automatic round transition

		// RUNDE 2 - Reset sums and attempts for new round
		Log(CurrentPhase, "=== RUNDE 2 (nach automatischem Reset) ===");
		currentRound = 2;
		// Reset discipline sums for round 2 (they get cleared in ZielBewerb.AddVersuch when transitioning to round 2)
		disziplinSummen["MassenVorne"] = 0;
		disziplinSummen["Schiessen"] = 0;
		disziplinSummen["MassenSeite"] = 0;
		disziplinSummen["Kombinieren"] = 0;
		// Reset attempt lists for round 2
		disziplinVersuche["MassenVorne"] = new List<int>();
		disziplinVersuche["Schiessen"] = new List<int>();
		disziplinVersuche["MassenSeite"] = new List<int>();
		disziplinVersuche["Kombinieren"] = new List<int>();

		for (int disziplin = 0; disziplin < 4; disziplin++)
		{
			var validValues = validValuesPerDisziplin[disziplin];
			Log(CurrentPhase, $"▶ Runde {currentRound}, Disziplin {disziplin + 1}/4: {disziplinNamen[disziplin]}");

			int versucheInDisziplin = 0;

			while (versucheInDisziplin < maxKehrenProSpiel)
			{
				int value;

				// Invalid value on 5. attempt
				if (versucheInDisziplin == maxKehrenProSpiel - 2)
				{
					int[] allValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 15];
					value = allValues.FirstOrDefault(v => !validValues.Contains(v));

					bool wasAccepted = await EnterAndConfirmZielAsync(value.ToString());
					Assert.False(wasAccepted, "Invalid value should be rejected");
					Log(CurrentPhase, $"Versuch {versucheInDisziplin + 1}/{maxKehrenProSpiel}: Ungültiger Wert {value} korrekt abgelehnt", LogSymbol.Check);

					await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);

					value = validValues[Rng.Next(validValues.Length)];
				}
				else
				{
					value = validValues[Rng.Next(validValues.Length)];
				}

				bool wasAccepted2 = await EnterAndConfirmZielAsync(value.ToString());
				Assert.True(wasAccepted2, "Valid value should be accepted");

				totalVersucheCount++;
				versucheInDisziplin++;
				disziplinSummen[disziplinNamen[disziplin]] += value;
				disziplinVersuche[disziplinNamen[disziplin]].Add(value);

				if (versucheInDisziplin % 2 == 0)
				{
					Log(CurrentPhase, $"  Versuch {versucheInDisziplin}/{maxKehrenProSpiel}: {value} ({totalVersucheCount}/{maxVersucheDisplay})");
				}

				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);
				await Task.Delay(250);
			}

			// Test delete on last discipline of round 2
			if (disziplin == 3)
			{
				Log(CurrentPhase, "Teste Löschen auf letzter Disziplin", LogSymbol.Check);

				await DeleteZielAttemptAsync();
				totalVersucheCount--;
				versucheInDisziplin--;

				// Remove from tracking lists
				var lastValue = disziplinVersuche[disziplinNamen[disziplin]].Last();
				disziplinVersuche[disziplinNamen[disziplin]].RemoveAt(disziplinVersuche[disziplinNamen[disziplin]].Count - 1);
				disziplinSummen[disziplinNamen[disziplin]] -= lastValue;

				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);

				int val = validValues[Rng.Next(validValues.Length)];
				bool wasAccepted3 = await EnterAndConfirmZielAsync(val.ToString());
				Assert.True(wasAccepted3, "Replacement value should be accepted");

				totalVersucheCount++;
				versucheInDisziplin++;
				disziplinSummen[disziplinNamen[disziplin]] += val;
				disziplinVersuche[disziplinNamen[disziplin]].Add(val);

				Log(CurrentPhase, $"Neuer Wert hinzugefügt: {val}", LogSymbol.Check);
				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheDisplay);
			}

			Log(CurrentPhase, $"Runde {currentRound} Disziplin {disziplin + 1} fertig", LogSymbol.Check);
		}

		Log(CurrentPhase, $"Phase 6 abgeschlossen: {totalVersucheCount}/{maxVersucheDisplay} Versuche (2 Runden)", LogSymbol.Check);

		// Validate Runde 2 persistence
		await ValidateZielStatePersistenceAsync(disziplinVersuche, expectedRunde: 2, expectedRunde1Summe: runde1Summe);

		Fixture.SendNetMqCommand("ResetResult");
		LogPhaseEnd(CurrentPhase);
	}
}
