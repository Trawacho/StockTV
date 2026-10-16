using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceModusTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task CycleModus_Forward_ChangesModusAndAppliesDefaults()
	{
		var service = await CreateInitializedServiceAsync();

		service.CycleModus(true);

		Assert.Equal(GameSettings.Modus.BestOf, service.CurrentSettings.Game.CurrentModus);
		Assert.Equal(6, service.CurrentSettings.Game.MaxKehrenProSpiel);
		Assert.Equal(10, service.CurrentSettings.Game.MaxPunkteProKehre);
	}

	[Fact]
	public async Task CycleModus_Backward_WrapsAndAppliesDefaults()
	{
		var service = await CreateInitializedServiceAsync();

		service.CycleModus(false);

		Assert.Equal(GameSettings.Modus.Ziel2, service.CurrentSettings.Game.CurrentModus);
		Assert.Equal(6, service.CurrentSettings.Game.MaxKehrenProSpiel);
		Assert.Equal(10, service.CurrentSettings.Game.MaxPunkteProKehre);
	}

	[Fact]
	public async Task CycleModus_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CycleModus(true);

		Assert.Equal(1, raised);
	}

	[Fact]
	public async Task CycleModus_IgnoredWhenBlockLocalChangesActive()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.General.BlockLocalChanges = true;
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CycleModus(true);

		Assert.Equal(GameSettings.Modus.Training, service.CurrentSettings.Game.CurrentModus);
		Assert.Equal(0, raised);
	}

	[Fact]
	public async Task ConfirmModusSelection_FiresOnNavigationRequestedWithCorrectUrl()
	{
		var service = await CreateInitializedServiceAsync();
		service.CycleModus(true); // Training -> BestOf
		string? navigatedUrl = null;
		service.OnNavigationRequested += url => navigatedUrl = url;

		service.ConfirmModusSelection();

		Assert.Equal("/bestof", navigatedUrl);
	}

	[Fact]
	public async Task ConfirmModusSelection_IgnoredWhenBlockLocalChangesActive()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.General.BlockLocalChanges = true;
		var navigated = false;
		service.OnNavigationRequested += _ => navigated = true;

		service.ConfirmModusSelection();

		Assert.False(navigated);
	}
}
