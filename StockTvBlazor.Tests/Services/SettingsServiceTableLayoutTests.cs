using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceTableLayoutTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task ResetTableLayout_RestoresDefaultsAndMidColumnWidth()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.TableLayout.CenterRowHeaderHeight = 1;
		service.CurrentSettings.UI.MidColumnWidth = 5;

		service.ResetTableLayout();

		var defaults = new TableLayoutSettings();
		Assert.Equal(defaults.CenterRowHeaderHeight, service.CurrentSettings.UI.TableLayout.CenterRowHeaderHeight);
		Assert.Equal(defaults.ZielRowMidCWidth, service.CurrentSettings.UI.TableLayout.ZielRowMidCWidth);
		Assert.Equal(90, service.CurrentSettings.UI.MidColumnWidth);
	}

	[Fact]
	public async Task ResetTableLayout_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.ResetTableLayout();

		Assert.Equal(1, raised);
	}

	[Fact]
	public async Task NotifyTableLayoutChanged_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CurrentSettings.UI.TableLayout.CenterRowMidHeight = 42;
		service.NotifyTableLayoutChanged();

		Assert.Equal(1, raised);
		Assert.Equal(42, service.CurrentSettings.UI.TableLayout.CenterRowMidHeight);
	}
}
