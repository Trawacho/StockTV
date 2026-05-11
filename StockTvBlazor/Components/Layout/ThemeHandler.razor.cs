using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Layout;

public partial class ThemeHandler : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;

	private int _updateCounter;
	private Settings.Settings _settings => _settingsService.CurrentSettings;

	protected override void OnInitialized()
		=> _settingsService.OnSettingsChanged += HandleUpdate;

	private async void HandleUpdate()
	{
		_updateCounter++;

		if (System.Diagnostics.Debugger.IsAttached)
		{
			var c = _settings.UI.Colors;
			Console.WriteLine($"-----------------------------------------");
			Console.WriteLine($"bg-color: {c.BackgroundColor}");
			Console.WriteLine($"fg-color: {c.ForegroundColor}");
			Console.WriteLine($"fg-left: {c.ForegroundLeft}");
			Console.WriteLine($"fg-right: {c.ForegroundRight}");
			Console.WriteLine($"fg-color-ziel-gesamt: {c.ZielSummeGesamt}");
			Console.WriteLine($"fg-color-ziel-einzel: {c.ZielSummeEinzel}");
		}

		await InvokeAsync(StateHasChanged);
	}

	public void Dispose()
		=> _settingsService.OnSettingsChanged -= HandleUpdate;
}
