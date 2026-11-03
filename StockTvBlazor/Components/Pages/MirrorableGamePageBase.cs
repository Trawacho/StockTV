using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

// Gemeinsames Grundgerüst der Spielseiten (BestOf, Training, Turnier, Ziel):
// Demo-/Mirror-Query-Parameter, Fokus-, Navigations- und Tasten-Guards für das
// Spiegel-Fenster (Display2, siehe dort) sowie das ViewModel-/Service-Event-Wiring.
public abstract class MirrorableGamePageBase<TViewModel> : ComponentBase, IDisposable
	where TViewModel : IPageViewModel
{
	[Inject] protected SettingsService SettingsService { get; set; } = default!;

	[Inject] protected NavigationManager NavigationManager { get; set; } = default!;

	[SupplyParameterFromQuery(Name = "demo")]
	protected bool IsDemo { get; set; }

	// Zweite Anzeige (gegenüberliegende Bahnseite): Spalten gespiegelt, reine Anzeige ohne Eingabe
	[SupplyParameterFromQuery(Name = "mirror")]
	protected bool IsMirrored { get; set; }

	protected string MirrorClass => IsMirrored ? "mirrored" : "";

	protected ElementReference inputRef;

	private bool _disposed;

	protected abstract TViewModel ViewModel { get; }

	protected abstract IGameInputService GameService { get; }

	protected override void OnInitialized()
	{
		if (IsDemo) ViewModel.EnableDemoMode();
		ViewModel.OnViewModelChanged += HandleUpdate;
		SettingsService.OnNavigationRequested += HandleNavigationRequested;
		GameService.OnNavigationRequested += HandleNavigationRequested;
		GameService.OnGlobalRefresh += HandleUpdate;
	}

	public virtual void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		ViewModel.OnViewModelChanged -= HandleUpdate;
		SettingsService.OnNavigationRequested -= HandleNavigationRequested;
		GameService.OnNavigationRequested -= HandleNavigationRequested;
		GameService.OnGlobalRefresh -= HandleUpdate;
		ViewModel.Dispose();
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;

		// Im Spiegel-Fenster steuert Display2 den iframe - hier nicht selbst navigieren
		if (IsMirrored) return;

		InvokeAsync(() => NavigationManager.NavigateTo(url));
	}

	private async void HandleUpdate()
	{
		if (_disposed) return;
		await InvokeAsync(StateHasChanged);
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

	protected async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		if (IsMirrored) return;

		await GameService.ProcessKeyAsync(e.Key);
	}
}
