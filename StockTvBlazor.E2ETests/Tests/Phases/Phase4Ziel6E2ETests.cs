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

	[Fact(Skip = "WIP: Ziel mode requires dedicated input helpers")]
	public async Task Phase4_Ziel_6Kehren()
	{
		LogPhaseStart("Phase 4", "Ziel 6 Kehren/Disziplin");
		Assert.NotNull(Fixture.Page);

		var currentSettings = await GetCurrentSettings();
		var zielSettings = GameplayScriptHelpers.BuildSettingsBytes(
			currentSettings, modus: 100, maxKehrenProSpiel: 6);
		await SendSettings(zielSettings);
		await Task.Delay(1500);

		Fixture.SendNetMqCommand("ResetResult");
		await Task.Delay(1500);

		await Fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);

		var validValuesPerDisziplin = new[]
		{
			new[] { 0, 2, 4, 6, 8, 10 },
			new[] { 0, 2, 5, 10 },
			new[] { 0, 2, 4, 6, 8, 10 },
			new[] { 0, 2, 4, 6, 8, 10 }
		};

		for (int disziplin = 0; disziplin < 4; disziplin++)
		{
			var validValues = validValuesPerDisziplin[disziplin];
			for (int attempt = 0; attempt < 6; attempt++)
			{
				int value;
				if (attempt < 4)
				{
					value = validValues[Rng.Next(validValues.Length)];
				}
				else if (attempt == 4)
				{
					int[] allValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 15 };
					value = allValues.FirstOrDefault(v => !validValues.Contains(v));
				}
				else
				{
					value = validValues[Rng.Next(validValues.Length)];
				}

				await EnterAndConfirm(value.ToString(), "*");
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}

			if (Rng.Next(2) == 0)
			{
				await Fixture.Page.Keyboard.PressAsync("-");
				await Task.Delay(DEBOUNCE_DELAY_MS);
				int value = validValues[Rng.Next(validValues.Length)];
				await EnterAndConfirm(value.ToString(), "*");
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
		}

		var content = await Fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		LogPhaseEnd("Phase 4");
	}
}
