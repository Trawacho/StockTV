using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceFontFamilyTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task NotifyFontFamilyChanged_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CurrentSettings.UI.FontFamily = "Arial";
		service.NotifyFontFamilyChanged();

		Assert.Equal(1, raised);
		Assert.Equal("Arial", service.CurrentSettings.UI.FontFamily);
	}
}
