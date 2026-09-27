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

	internal bool _modusEditActive = false;
	internal bool _isPressingModusEdit = false;
	private CancellationTokenSource? _modusEditPressCts;

	private static readonly TimeSpan ModusEditHoldDuration = TimeSpan.FromSeconds(5);

	internal bool _teamNameEditActive = false;
	internal string? _teamNameLeftInput;
	internal string? _teamNameRightInput;

	private bool _disposed = false;

	protected override void OnInitialized()
	{
		SettingsService.OnSettingsChanged += HandleSettingsChanged;
		SettingsService.OnNavigationRequested += HandleNavigationRequested;

		MatchService.OnNavigationRequested += HandleNavigationRequested;
		MatchService.OnGlobalRefresh += HandleUpdate;

		ZielService.OnGlobalRefresh += HandleUpdate;
		ZielService.OnNavigationRequested += HandleNavigationRequested;

		_spielRichtung = SettingsService.CurrentSettings.UI.CurrentRichtung;
		SetInteralUrl();
	}

	public string SpielModus => SettingsService.CurrentSettings.Game.CurrentModus.ToString();

	public bool CanEditModus =>
		!SettingsService.CurrentSettings.General.BlockLocalChanges &&
		!SettingsService.SettingsPageActive;

	public bool CanEditTeamNames =>
		SettingsService.CurrentSettings.Game.CurrentModus == StockTvBlazor.Settings.GameSettings.Modus.BestOf &&
		!SettingsService.CurrentSettings.General.BlockLocalChanges &&
		!MatchService.CurrentMatch.Games.Any(g => g.Turns.Count > 0);

	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;
		SettingsService.OnSettingsChanged -= HandleSettingsChanged;
		MatchService.OnGlobalRefresh -= HandleUpdate;
		ZielService.OnGlobalRefresh -= HandleUpdate;
		SettingsService.OnNavigationRequested -= HandleNavigationRequested;
		MatchService.OnNavigationRequested -= HandleNavigationRequested;
		ZielService.OnNavigationRequested -= HandleNavigationRequested;

		_modusEditPressCts?.Cancel();
		_modusEditPressCts?.Dispose();
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;

		SyncModusEditVisibility();
		SyncTeamNameEditVisibility();
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

		SyncModusEditVisibility();
		SyncTeamNameEditVisibility();
		SetInteralUrl();
	}

	private void SyncModusEditVisibility()
	{
		if (_modusEditActive && !CanEditModus)
			_modusEditActive = false;
	}

	private void SyncTeamNameEditVisibility()
	{
		if (_teamNameEditActive && !CanEditTeamNames)
			_teamNameEditActive = false;
	}

	private void SetInteralUrl()
	{
		if (SettingsService.SettingsPageActive || _modusEditActive)
			return;

		var modus = SettingsService.CurrentSettings.Game.CurrentModus;
		_internalUrl = SettingsService.GetModusUrl(modus);
	}

	private void HandleUpdate()
	{
		if (_disposed) return;

		SyncTeamNameEditVisibility();
		InvokeAsync(() => StateHasChanged());
	}

	internal void OnDisplayPressStart()
	{
		if (!CanEditModus || _modusEditActive) return;

		_modusEditPressCts?.Cancel();
		_modusEditPressCts?.Dispose();
		_modusEditPressCts = new CancellationTokenSource();
		_isPressingModusEdit = true;

		_ = RunModusEditPressTimerAsync(_modusEditPressCts.Token);
	}

	internal void OnDisplayPressEnd()
	{
		_modusEditPressCts?.Cancel();
		_isPressingModusEdit = false;
	}

	private async Task RunModusEditPressTimerAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(ModusEditHoldDuration, token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (_disposed || token.IsCancellationRequested || !CanEditModus || _modusEditActive)
			return;

		_modusEditActive = true;
		_isPressingModusEdit = false;

		await InvokeAsync(StateHasChanged);
	}

	internal void SuppressContextMenu() { }

	internal async Task HandleInput(string input)
	{
		_lastAction = input;

		if (SettingsService.SettingsPageActive)
		{
			await SettingsService.ProcessKeyAsync(input);
			return;
		}

		if (_modusEditActive)
		{
			HandleModusEditInput(input);
			return;
		}

		var modus = SettingsService.CurrentSettings.Game.CurrentModus;
		if (modus == StockTvBlazor.Settings.GameSettings.Modus.Ziel || modus == StockTvBlazor.Settings.GameSettings.Modus.Ziel2)
			await ZielService.ProcessKeyAsync(input);
		else
			await MatchService.ProcessKeyAsync(input);
	}

	private void HandleModusEditInput(string input)
	{
		switch (input)
		{
			case "4" or "ArrowLeft":
				SettingsService.CycleModus(false);
				break;

			case "6" or "ArrowRight":
				SettingsService.CycleModus(true);
				break;

			case "+":
				// Reihenfolge wichtig: _modusEditActive muss vor ConfirmModusSelection() auf
				// false gesetzt werden, da diese synchron OnNavigationRequested ausloest, das
				// wiederum SetInteralUrl() aufruft - deren Guard sonst die Iframe-Aktualisierung
				// faelschlich unterdruecken wuerde.
				_modusEditActive = false;
				SettingsService.ConfirmModusSelection();
				break;

				// Alle anderen Tasten (0-9 ausser 4/6, up/down, R/G, Loeschen) sind
				// waehrend der Modus-Bearbeitung bewusst wirkungslos.
		}
	}

	internal void ToggleTeamNameEdit()
	{
		if (!CanEditTeamNames) return;

		if (_teamNameEditActive)
		{
			_teamNameEditActive = false;
			return;
		}

		var begegnung = MatchService.CurrentMatch.Begegnungen.FirstOrDefault(b => b.Spielnummer == 1);
		var isLinks = SettingsService.CurrentSettings.UI.CurrentRichtung == StockTvBlazor.Settings.UiSettings.Richtung.Links;

		_teamNameLeftInput = begegnung?.TeamNameLeft(isLinks) ?? "";
		_teamNameRightInput = begegnung?.TeamNameRight(isLinks) ?? "";
		_teamNameEditActive = true;
	}

	internal void ClearTeamNameLeft() => _teamNameLeftInput = "";
	internal void ClearTeamNameRight() => _teamNameRightInput = "";

	internal void SaveTeamNames()
	{
		MatchService.SetBestOfTeamNames(_teamNameLeftInput?.Trim() ?? "", _teamNameRightInput?.Trim() ?? "");
		_teamNameEditActive = false;
	}

}
