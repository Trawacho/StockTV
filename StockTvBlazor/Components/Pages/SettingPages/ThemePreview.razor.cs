using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class ThemePreview : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;

	private string _internalUrl = "";
	private bool _disposed;
	private GameSettings.Modus _selectedModus = GameSettings.Modus.Training;

	protected override void OnInitialized()
	{
		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		UpdateUrl();
	}

	private void UpdateUrl()
	{
		_internalUrl = SettingsService.GetModusUrl(_selectedModus) + "?demo=true";
	}

	private void OnModusSelected(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var value))
		{
			_selectedModus = (GameSettings.Modus)value;
			UpdateUrl();
		}
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	public void Dispose()
	{
		_disposed = true;
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
	}
}
