using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public class Phase3BestOfE2ETests : PhaseTestBase
{
	public Phase3BestOfE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	public async Task Phase3_BestOf_3Spiele()
	{
		LogPhaseStart("Phase 3", "BestOf 3 Spiele");
		Assert.NotNull(Fixture.Page);

		// Konstanten für Test-Parameter
		const int maxPunkteProKehre = 8;
		const int maxKehrenProSpiel = 6;
		const int numberOfGames = 3;

		var currentSettings = await GetCurrentSettings();
		var bestofSettings = GameplayScriptHelpers.BuildSettingsBytes(
			currentSettings, modus: 1, maxPunkteProKehre: maxPunkteProKehre, maxKehrenProSpiel: maxKehrenProSpiel);
		await SendSettings(bestofSettings);
		await Task.Delay(1500);

		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(1500);

		// Team-Namen dynamisch generieren
		var teamNamesPayload = string.Join(";", Enumerable.Range(1, numberOfGames).Select(i => $"{i}:TeamX:TeamY"));
		Fixture.SendNetMqCommand("SetTeamNames", teamNamesPayload);
		await Task.Delay(1500);

		await Fixture.Page.GotoAsync("http://localhost:5001/bestof");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);

		for (int game = 1; game <= numberOfGames; game++)
		{
			var gameTurns = new List<int>();

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
					await EnterAndConfirm(invalidVal.ToString(), GetConfirmKey());
					await Task.Delay(DEBOUNCE_DELAY_MS);

					// Danach einen gültigen Wert senden
					val = Rng.Next(0, maxPunkteProKehre + 1);
				}
				else
				{
					val = Rng.Next(0, maxPunkteProKehre + 1);
				}

				await EnterAndConfirm(val.ToString());
				await Task.Delay(DEBOUNCE_DELAY_MS);

				gameTurns.Add(val);
				validTurnsCount++;

				var payload = GetLatestGetResultPayload();
				if (payload != null)
				{
					var games = GameplayScriptHelpers.StripPrefixAndParseGames(payload);
					if (games != null && games.Count > 0)
					{
						var currentGame = games.FirstOrDefault(g => g.GameNumber == game);
						if (currentGame != null && currentGame.Turns.Count != gameTurns.Count)
						{
							System.Diagnostics.Debug.WriteLine($"BestOf turn mismatch: expected {gameTurns.Count}, got {currentGame.Turns.Count}");
						}
					}
				}
			}

			if (gameTurns.Count > 0)
			{
				await Fixture.Page.Keyboard.PressAsync("-");
				await Task.Delay(DEBOUNCE_DELAY_MS);
				gameTurns.RemoveAt(gameTurns.Count - 1);

				int val = Rng.Next(0, 7);
				await EnterAndConfirm(val.ToString());
				await Task.Delay(DEBOUNCE_DELAY_MS);
				gameTurns.Add(val);

				var payload = GetLatestGetResultPayload();
				if (payload != null)
				{
					var games = GameplayScriptHelpers.StripPrefixAndParseGames(payload);
				}
			}

			await Fixture.Page.Keyboard.PressAsync("+");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			var matchPointsLeft = await Fixture.Page.Locator(".score-cell.left-match-points").TextContentAsync();
			Assert.NotNull(matchPointsLeft);
		}

		Fixture.SendNetMqCommand("ResetResult");
		var trainingSettings = GameplayScriptHelpers.BuildSettingsBytes(
			await GetCurrentSettings(), modus: 0);
		await SendSettings(trainingSettings);

		await Task.Delay(1000);
		LogPhaseEnd("Phase 3");
	}
}
