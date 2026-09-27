using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class FontEditor : IDisposable
{
	[Inject] private SettingsService SettingsService { get; set; } = default!;
	[Inject] private FontService FontService { get; set; } = default!;

	private bool _disposed;
	private readonly HashSet<string> _openGroups = new();

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

	private bool ShowGroup(string key) => _openGroups.Contains(key);

	private void ToggleGroup(string key)
	{
		if (!_openGroups.Add(key))
			_openGroups.Remove(key);
	}

	private void ToggleSchriftartGroup() => ToggleGroup("schriftart");
	private void ToggleTeamKopfGroup() => ToggleGroup("teamkopf");
	private void ToggleScoreCellGroup() => ToggleGroup("scorecell");
	private void ToggleZielGroup() => ToggleGroup("ziel");

	private void NotifyChanged() => SettingsService.NotifyCellFontWeightChanged();

	private void ResetAll() => SettingsService.ResetCellFontWeight();

	private void OnFontFamilyChanged(ChangeEventArgs e)
	{
		SettingsService.CurrentSettings.UI.FontFamily = e.Value?.ToString() ?? "";
		SettingsService.NotifyFontFamilyChanged();
	}
}
