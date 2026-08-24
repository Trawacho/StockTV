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

	// Wird die Seite im Spiegel-Fenster (/display2) im iframe angezeigt, ist sie reine
	// Anzeige: keine Eingabe, kein Fokus und keine eigene Navigation - den iframe steuert
	// Display2. Sonst würde beim Moduswechsel gleichzeitig navigiert und der iframe
	// ausgetauscht.
	[SupplyParameterFromQuery(Name = "mirror")]
	private bool IsMirrored { get; set; }

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
		if (IsMirrored) return;

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
		if (firstRender && !IsMirrored)
		{
			try
			{
				await inputRef.FocusAsync();
			}
			catch (ObjectDisposedException) { }
			catch (TaskCanceledException) { }
		}
	}

	public async Task HandleGlobalKeyDown(KeyboardEventArgs e)
	{
		if (IsMirrored) return;

		await _settingsService.ProcessKeyAsync(e.Key);
		StateHasChanged();
	}
}
