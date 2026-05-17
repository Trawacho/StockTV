using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class Turnier : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;
	[Inject] private NavigationManager _navigationManager { get; set; } = default!;
	[Inject] private MatchService _matchService { get; set; } = default!;
	[Inject] private TurnierViewModel ViewModel { get; set; } = default!;
	[Inject] private IJSRuntime JS { get; set; } = default!;

	[SupplyParameterFromQuery(Name = "demo")]
	private bool IsDemo { get; set; }

	private ElementReference inputRef;
	private bool _disposed = false;
	private bool TeamNamesAvailable => ViewModel.TeamNamesAvailable;

	protected override void OnInitialized()
	{
		if (IsDemo) ViewModel.EnableDemoMode();
		ViewModel.OnViewModelChanged += HandleUpdate;
		_matchService.OnGlobalRefresh += HandleUpdate;
		_matchService.OnNavigationRequested += HandleNavigationRequested;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;
	}

	public void Dispose()
	{
		_disposed = true;
		ViewModel.OnViewModelChanged -= HandleUpdate;
		_matchService.OnGlobalRefresh -= HandleUpdate;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
		_matchService.OnNavigationRequested -= HandleNavigationRequested;
		ViewModel.Dispose();
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;
		InvokeAsync(() => _navigationManager.NavigateTo(url));
	}

	private async void HandleUpdate()
	{
		if (_disposed) return;
		await InvokeAsync(StateHasChanged);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await JS.InvokeVoidAsync("stockTvAutoFit.observe", ".page-fullscreen");
		if (firstRender)
			await inputRef.FocusAsync();
	}

	private async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		await _matchService.ProcessKeyAsync(e.Key);
	}
}
