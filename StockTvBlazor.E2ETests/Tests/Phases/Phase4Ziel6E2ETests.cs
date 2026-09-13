using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase4Ziel6E2ETests : PhaseTestBase
{
	public Phase4Ziel6E2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	[Fact]
	public async Task Phase4_Ziel_6Kehren()
	{
		CurrentPhase = "Phase 4";
		LogPhaseStart(CurrentPhase, "Ziel 6 Kehren pro Disziplin");
		Assert.NotNull(Fixture.Page);

		// Konstanten
		const int maxKehrenProSpiel = 6;
		const int maxVersucheGesamt = 4 * maxKehrenProSpiel;  // 24 total

		// Konfiguriere Ziel-Modus
		Log(CurrentPhase, $"Starte Ziel mit {maxKehrenProSpiel} Versuchen pro Disziplin ({maxVersucheGesamt} total)");
		await ConfigureAndValidateSettings(
			modus: 100,
			maxPunkteProKehre: 10,
			maxKehrenProSpiel: maxKehrenProSpiel,
			validatePersistence: false);

		// Navigate to Ziel page
		await Fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(1500);
		Log(CurrentPhase, "Navigiert zu /ziel");

		Fixture.ClearPublisherMessages();

		Log(CurrentPhase, "Sende ResetResult an Server...");
		Fixture.SendNetMqCommand("ResetResult");

		// Set player name via NetMQ
		string spielername = "TestSpieler_Phase4";
		SetZielTeilnehmer(spielername);

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

		for (int disziplin = 0; disziplin < 4; disziplin++)
		{
			var validValues = validValuesPerDisziplin[disziplin];
			Log(CurrentPhase, $"▶ Starte Disziplin {disziplin + 1}/4: {disziplinNamen[disziplin]}");

			int versucheInDisziplin = 0;

			while (versucheInDisziplin < maxKehrenProSpiel)
			{
				int value;

				// Test invalid input on specific attempt
				if (versucheInDisziplin == maxKehrenProSpiel - 2)
				{
					// 5. Versuch von 6: ungültiger Wert
					int[] allValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 15];
					value = allValues.FirstOrDefault(v => !validValues.Contains(v));

					bool wasAccepted = await EnterAndConfirmZielAsync(value.ToString());
					Assert.False(wasAccepted, "Invalid value should be rejected");
					Log(CurrentPhase, $"Versuch {versucheInDisziplin + 1}/{maxKehrenProSpiel}: Ungültiger Wert {value} korrekt abgelehnt", LogSymbol.Check);

					// Validate display after invalid attempt (attempt count should NOT increase)
					await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheGesamt);

					// Now send valid value
					value = validValues[Rng.Next(validValues.Length)];
				}
				else
				{
					value = validValues[Rng.Next(validValues.Length)];
				}

				// Enter valid value
				bool wasAccepted2 = await EnterAndConfirmZielAsync(value.ToString());
				Assert.True(wasAccepted2, "Valid value should be accepted");

				totalVersucheCount++;
				versucheInDisziplin++;
				disziplinSummen[disziplinNamen[disziplin]] += value;
				disziplinVersuche[disziplinNamen[disziplin]].Add(value);

				Log(CurrentPhase, $"  Versuch {versucheInDisziplin}/{maxKehrenProSpiel}: Wert {value} akzeptiert");

				// Validate display
				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheGesamt);

				// Validate NetMQ Publisher
				await ValidateZielNetMqPublisherAsync(totalVersucheCount, disziplinSummen);

				await Task.Delay(200);
			}

			// Test delete on last discipline only
			if (disziplin == 3)
			{
				Log(CurrentPhase, "Teste Löschen auf letzter Disziplin (Taste -)", LogSymbol.Check);

				// Delete last
				await DeleteZielAttemptAsync();
				totalVersucheCount--;
				versucheInDisziplin--;

				// Remove from tracking lists
				var lastValue = disziplinVersuche[disziplinNamen[disziplin]].Last();
				disziplinVersuche[disziplinNamen[disziplin]].RemoveAt(disziplinVersuche[disziplinNamen[disziplin]].Count - 1);
				disziplinSummen[disziplinNamen[disziplin]] -= lastValue;

				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheGesamt);

				// Add new one
				int val = validValues[Rng.Next(validValues.Length)];
				bool wasAccepted3 = await EnterAndConfirmZielAsync(val.ToString());
				Assert.True(wasAccepted3, "Replacement value should be accepted");

				totalVersucheCount++;
				versucheInDisziplin++;
				disziplinSummen[disziplinNamen[disziplin]] += val;
				disziplinVersuche[disziplinNamen[disziplin]].Add(val);

				Log(CurrentPhase, $"Neuer Wert hinzugefügt: {val}", LogSymbol.Check);
				await ValidateDisplayZielAsync(totalVersucheCount, maxVersucheGesamt);
			}

			Log(CurrentPhase, $"Disziplin {disziplin + 1}/4 abgeschlossen ({maxKehrenProSpiel} Versuche)", LogSymbol.Check);
		}

		Log(CurrentPhase, $"Phase 4 abgeschlossen: {totalVersucheCount}/{maxVersucheGesamt} Versuche", LogSymbol.Check);

		// Validate ziel-state.json persistence
		await ValidateZielStatePersistenceAsync(disziplinVersuche, expectedRunde: 1, expectedRunde1Summe: 0);

		Fixture.SendNetMqCommand("ResetResult");
		LogPhaseEnd(CurrentPhase);
	}
}
