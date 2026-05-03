using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public class InputBase : ComponentBase, IDisposable
{
	[Inject] protected SettingsService SettingsService { get; set; } = default!;
	[Inject] protected MatchService MatchService { get; set; } = default!;
	[Inject] protected ZielService ZielService { get; set; } = default!;

	internal string _lastAction = "START";
	internal string _internalUrl = "";
	internal StockTvBlazor.Settings.UiSettings.Richtung _spielRichtung;

	private bool _disposed = false;

	protected override void OnInitialized()
	{
		SettingsService.OnSettingsChanged += HandleSettingsChanged;
		SettingsService.OnNavigationRequested += HandleNavigationRequested;

		MatchService.OnNavigationRequested += HandleNavigationRequested;
		MatchService.OnGlobalRefresh += HandleUpdate;

		_spielRichtung = SettingsService.CurrentSettings.UI.CurrentRichtung;
		SetInteralUrl();
	}

	public string SpielModus => SettingsService.CurrentSettings.Game.CurrentModus.ToString();

	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;
		SettingsService.OnSettingsChanged -= HandleSettingsChanged;
		MatchService.OnGlobalRefresh -= HandleUpdate;
		SettingsService.OnNavigationRequested -= HandleNavigationRequested;
		MatchService.OnNavigationRequested -= HandleNavigationRequested;
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;

		SetInteralUrl();
		_spielRichtung = SettingsService.CurrentSettings.UI.CurrentRichtung;
		InvokeAsync(() =>
		{
			StateHasChanged();
		});
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;

		SetInteralUrl();
	}

	private void SetInteralUrl()
	{
		if(SettingsService.SettingsPageActive)
			return;

		var modus = SettingsService.CurrentSettings.Game.CurrentModus;
		_internalUrl = SettingsService.GetModusUrl(modus);
	}

	private void HandleUpdate()
	{
		if (_disposed) return;

		InvokeAsync(() => StateHasChanged());
	}

	internal async Task HandleInput(string input)
	{
		_lastAction = input;
		if(SettingsService.SettingsPageActive)
			await SettingsService.ProcessKeyAsync(input);
		else{
			if (SettingsService.CurrentSettings.Game.CurrentModus == StockTvBlazor.Settings.GameSettings.Modus.Ziel )
				await ZielService.ProcessKeyAsync(input);
			else
				await MatchService.ProcessKeyAsync(input);
		}
	}

}