using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceCellFontWeightTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task ResetCellFontWeight_RestoresDefaults()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.CellFontWeight.TeamNameWeight = 100;
		service.CurrentSettings.UI.CellFontWeight.ZielSummeWeight = 900;

		service.ResetCellFontWeight();

		var defaults = new CellFontWeightSettings();
		Assert.Equal(defaults.TeamNameWeight, service.CurrentSettings.UI.CellFontWeight.TeamNameWeight);
		Assert.Equal(defaults.ZielSummeWeight, service.CurrentSettings.UI.CellFontWeight.ZielSummeWeight);
	}

	[Fact]
	public async Task ResetCellFontWeight_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.ResetCellFontWeight();

		Assert.Equal(1, raised);
	}

	[Fact]
	public async Task NotifyCellFontWeightChanged_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CurrentSettings.UI.CellFontWeight.InputValueWeight = 900;
		service.NotifyCellFontWeightChanged();

		Assert.Equal(1, raised);
		Assert.Equal(900, service.CurrentSettings.UI.CellFontWeight.InputValueWeight);
	}
}
