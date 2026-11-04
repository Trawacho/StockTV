using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

/// <summary>Zeile "Anspiel" im Tastaturmenue (/settings).</summary>
public class SettingsMenuAnspielFrameTests
{
	private static async Task<SettingsService> CreateInitializedServiceAsync()
	{
		var service = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await service.InitializeAsync();
		return service;
	}

	[Fact]
	public async Task ArrowDownFromTheme_SelectsAnspiel_ThenRichtung()
	{
		var service = await CreateInitializedServiceAsync();
		using var vm = new SettingsViewModel(service);
		Assert.True(vm.IsThemeActive);

		await service.ProcessKeyAsync("2");
		Assert.True(vm.IsAnspielFrameActive);

		await service.ProcessKeyAsync("2");
		Assert.True(vm.IsRichtungActive);

		await service.ProcessKeyAsync("8");
		Assert.True(vm.IsAnspielFrameActive);
	}

	[Theory]
	[InlineData("6")]
	[InlineData("4")]
	[InlineData("ArrowRight")]
	[InlineData("ArrowLeft")]
	public async Task EitherArrow_TogglesFrame_AndBack(string key)
	{
		var service = await CreateInitializedServiceAsync();
		using var vm = new SettingsViewModel(service);
		await service.ProcessKeyAsync("2");
		Assert.True(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal("Rahmen", vm.AnspielFrameValue);

		await service.ProcessKeyAsync(key);
		Assert.False(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal("Zeichen", vm.AnspielFrameValue);

		await service.ProcessKeyAsync(key);
		Assert.True(service.CurrentSettings.UI.AnspielFrame.ShowFrame);
		Assert.Equal("Rahmen", vm.AnspielFrameValue);
	}

	[Fact]
	public async Task Toggle_KeepsFrameWidth_AndRaisesSettingsChanged()
	{
		var service = await CreateInitializedServiceAsync();
		service.CurrentSettings.UI.AnspielFrame.FrameWidthPx = 6;
		await service.ProcessKeyAsync("2");
		var raised = 0;
		service.OnSettingsChanged += () => raised++;

		await service.ProcessKeyAsync("6");

		Assert.True(raised > 0);
		Assert.Equal(6, service.CurrentSettings.UI.AnspielFrame.FrameWidthPx);
	}
}
