using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class ThemePreview : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;

	private string _internalUrl = "";
	private bool _disposed;

	protected override void OnInitialized()
	{
		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;
		UpdateUrl();
	}

	private void UpdateUrl()
	{
		var modus = _settingsService.CurrentSettings.Game.CurrentModus;
		_internalUrl = SettingsService.GetModusUrl(modus);
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;
		UpdateUrl();
		InvokeAsync(StateHasChanged);
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;
		UpdateUrl();
		InvokeAsync(StateHasChanged);
	}

	public void Dispose()
	{
		_disposed = true;
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
	}
}
