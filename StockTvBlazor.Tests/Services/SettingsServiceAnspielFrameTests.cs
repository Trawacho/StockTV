using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class SettingsServiceAnspielFrameTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task NotifyAnspielFrameChanged_RaisesOnSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.CurrentSettings.UI.AnspielFrame.ShowFrame = false;
		service.NotifyAnspielFrameChanged();

		Assert.Equal(1, raised);
		Assert.False(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
	}

	[Fact]
	public async Task ResetAnspielFrame_RestoresDefaults()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.AnspielFrame.ShowFrame = false;
		service.CurrentSettings.UI.AnspielFrame.FrameWidthPx = 9;
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		service.ResetAnspielFrame();

		Assert.Equal(1, raised);
		Assert.True(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal(3, service.CurrentSettings.UI.AnspielFrame.FrameWidthPx);
	}
}
