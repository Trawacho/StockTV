using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;
using System.Globalization;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class TableLayoutEditor : IDisposable
{
	[Inject] private SettingsService SettingsService { get; set; } = default!;

	private bool _disposed;
	private readonly HashSet<string> _openGroups = new() { "team" };

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

	private TableLayoutSettings Layout => SettingsService.CurrentSettings.UI.TableLayout;

	private int MidColumnWidth => SettingsService.CurrentSettings.UI.MidColumnWidth;

	private bool ShowGroup(string key) => _openGroups.Contains(key);

	private void ToggleGroup(string key)
	{
		if (!_openGroups.Add(key))
			_openGroups.Remove(key);
	}

	private void ToggleTeamGroup() => ToggleGroup("team");
	private void ToggleCenterGroup() => ToggleGroup("center");
	private void ToggleBottomGroup() => ToggleGroup("bottom");
	private void ToggleMidGrid3Group() => ToggleGroup("midgrid3");
	private void ToggleBestOfGroup() => ToggleGroup("bestof");
	private void ToggleZielMainGroup() => ToggleGroup("zielmain");
	private void ToggleZielBlockGroup() => ToggleGroup("zielblock");
	private void ToggleZielRowTopGroup() => ToggleGroup("zielrowtop");
	private void ToggleZielRowMidGroup() => ToggleGroup("zielrowmid");

	private void NotifyChanged() => SettingsService.NotifyTableLayoutChanged();

	private void OnMidColumnWidthChanged(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var value))
		{
			SettingsService.CurrentSettings.UI.MidColumnWidth = value;
			SettingsService.NotifyTableLayoutChanged();
		}
	}

	private void ResetAll() => SettingsService.ResetTableLayout();

	private static string KindHint(bool isRow) => isRow
		? "↕ Zeilenhöhen — werden übereinander angeordnet. Summe muss 100 % ergeben."
		: "↔ Spaltenbreiten — werden nebeneinander angeordnet. Summe muss 100 % ergeben.";

	private static bool SumInvalid(params double[] values) => Math.Abs(values.Sum() - 100) > 0.05;

	private static string SumWarning(params double[] values) =>
		$"⚠ Summe: {values.Sum().ToString("0.####", CultureInfo.InvariantCulture)} % — sollte 100 % sein";
}
