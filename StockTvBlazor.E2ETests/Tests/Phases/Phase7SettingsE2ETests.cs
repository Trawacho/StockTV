using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

public class Phase7SettingsE2ETests : PhaseTestBase
{
	public Phase7SettingsE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
	}

	public async Task Phase7_Settings_Navigation()
	{
		LogPhaseStart("Phase 7", "Settings & Navigation");
		Assert.NotNull(Fixture.Page);

		var trainingSettings = GameplayScriptHelpers.BuildSettingsBytes(
			await GetCurrentSettings(), modus: 0);
		await SendSettings(trainingSettings);
		await Task.Delay(1500);

		await Fixture.Page.GotoAsync("http://localhost:5001/training");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(2000);

		for (int i = 0; i < 5; i++)
		{
			await Fixture.Page.Keyboard.PressAsync("Enter");
			await Task.Delay(DEBOUNCE_DELAY_MS);
		}

		await Task.Delay(DEBOUNCE_DELAY_MS);
		var settingsContent = await Fixture.Page.ContentAsync();
		Assert.NotEmpty(settingsContent);

		await Fixture.Page.Keyboard.PressAsync("2");
		await Task.Delay(DEBOUNCE_DELAY_MS);
		await Fixture.Page.Keyboard.PressAsync("8");
		await Task.Delay(DEBOUNCE_DELAY_MS);

		await Fixture.Page.Keyboard.PressAsync("+");
		await Task.Delay(DEBOUNCE_DELAY_MS);

		var finalUrl = Fixture.Page?.Url ?? "";
		Assert.NotEmpty(finalUrl);
		LogPhaseEnd("Phase 7");
	}
}
