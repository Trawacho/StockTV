using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class Ziel : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;
	[Inject] private ZielService _zielService { get; set; } = default!;
	[Inject] private NavigationManager _navigationManager { get; set; } = default!;
	[Inject] private ZielViewModel ViewModel { get; set; } = default!;
	[Inject] private IJSRuntime JS { get; set; } = default!;

	private ElementReference inputRef;
	private bool _disposed = false;

	protected override void OnInitialized()
	{
		ViewModel.OnViewModelChanged += HandleUpdate;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;
		_zielService.OnNavigationRequested += HandleNavigationRequested;
		_zielService.OnGlobalRefresh += HandleUpdate;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		ViewModel.OnViewModelChanged -= HandleUpdate;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
		_zielService.OnNavigationRequested -= HandleNavigationRequested;
		_zielService.OnGlobalRefresh -= HandleUpdate;
		ViewModel.Dispose();
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;
		InvokeAsync(() => _navigationManager.NavigateTo(url));
	}

	private void HandleUpdate()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await JS.InvokeVoidAsync("stockTvAutoFit.observe", ".ziel-main-grid");
		if (firstRender)
			await inputRef.FocusAsync();
	}

	private async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		await _zielService.ProcessKeyAsync(e.Key);
	}
}
