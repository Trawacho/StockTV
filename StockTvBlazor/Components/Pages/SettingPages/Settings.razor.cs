using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class Settings : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;
	[Inject] private NavigationManager _navigationManager { get; set; } = default!;
	[Inject] private SettingsViewModel ViewModel { get; set; } = default!;

	private ElementReference inputRef;
	private bool _disposed;

	protected override void OnInitialized()
	{
		_settingsService.OnSettingsChanged += HandleChanged;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;
	}

	private void HandleChanged()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;
		InvokeAsync(() => _navigationManager.NavigateTo(url));
	}

	public void Dispose()
	{
		_disposed = true;
		_settingsService.OnSettingsChanged -= HandleChanged;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
		ViewModel.Dispose();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
			await inputRef.FocusAsync();
	}

	public async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		await _settingsService.ProcessKeyAsync(e.Key);
		StateHasChanged();
	}
}
