using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class FontWeightEditor : IDisposable
{
	[Inject] private SettingsService SettingsService { get; set; } = default!;

	private bool _disposed;

	protected override void OnInitialized()
	{
		SettingsService.OnSettingsChanged += HandleSettingsChanged;
	}

	public void Dispose()
	{
		_disposed = true;
		SettingsService.OnSettingsChanged -= HandleSettingsChanged;
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	private CellFontWeightSettings Weights => SettingsService.CurrentSettings.UI.CellFontWeight;

	private void NotifyChanged() => SettingsService.NotifyCellFontWeightChanged();

	private void ResetAll() => SettingsService.ResetCellFontWeight();
}
