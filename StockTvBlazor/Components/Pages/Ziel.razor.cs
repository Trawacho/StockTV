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

	[SupplyParameterFromQuery(Name = "demo")]
	private bool IsDemo { get; set; }

	// Im Spiegel-Fenster (/display2) reine Anzeige: keine Eingabe, kein Fokus,
	// keine Eigennavigation. Das Ziel-Layout selbst wird bewusst nicht gespiegelt.
	[SupplyParameterFromQuery(Name = "mirror")]
	private bool IsMirrored { get; set; }

	private ElementReference inputRef;
	
	private bool _disposed = false;

	protected override void OnInitialized()
	{
		if (IsDemo) ViewModel.EnableDemoMode();
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
		if (IsMirrored) return;

		InvokeAsync(() => _navigationManager.NavigateTo(url));
	}

	private void HandleUpdate()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_disposed) return;
		if (firstRender && !IsDemo && !IsMirrored)
		{
			try
			{
				await inputRef.FocusAsync();
			}
			catch (JSDisconnectedException) { }
			catch (ObjectDisposedException) { }
			catch (TaskCanceledException) { }
		}
	}

	private async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		if (IsMirrored) return;

		await _zielService.ProcessKeyAsync(e.Key);
	}
}
