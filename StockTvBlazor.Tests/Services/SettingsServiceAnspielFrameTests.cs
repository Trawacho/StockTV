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
	public async Task SetAnspielFrame_BothValues_AreApplied()
	{
		var service = await CreateInitializedServiceAsync();
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		var error = service.SetAnspielFrame(false, 6);

		Assert.Null(error);
		Assert.Equal(1, raised);
		Assert.False(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal(6, service.CurrentSettings.UI.AnspielFrame.FrameWidthPx);
	}

	[Fact]
	public async Task SetAnspielFrame_OmittedValue_StaysUnchanged()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.AnspielFrame.FrameWidthPx = 5;

		Assert.Null(service.SetAnspielFrame(false, null));

		Assert.False(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal(5, service.CurrentSettings.UI.AnspielFrame.FrameWidthPx);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(true, 0)]
	[InlineData(true, 11)]
	[InlineData(null, -1)]
	public async Task SetAnspielFrame_InvalidRequest_IsRejected_AndChangesNothing(bool? show, int? width)
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.AnspielFrame.ShowFrame = false;
		service.CurrentSettings.UI.AnspielFrame.FrameWidthPx = 4;
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		var error = service.SetAnspielFrame(show, width);

		Assert.NotNull(error);
		Assert.Equal(0, raised);
		Assert.False(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal(4, service.CurrentSettings.UI.AnspielFrame.FrameWidthPx);
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
