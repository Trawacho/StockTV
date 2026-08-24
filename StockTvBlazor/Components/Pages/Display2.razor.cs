using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

/// <summary>
/// Wrapper-Seite für den zweiten Bildschirm (gegenüberliegende Bahnseite).
/// Zeigt den jeweils aktiven Modus gespiegelt als iframe an - analog zu <see cref="Input"/>.
/// Diese Seite navigiert selbst nie, dadurch bleibt die Kiosk-URL dauerhaft "/display2".
/// </summary>
public partial class Display2 : IDisposable
{
	[Inject] private SettingsService _settingsService { get; set; } = default!;
	[Inject] private MatchService _matchService { get; set; } = default!;
	[Inject] private ZielService _zielService { get; set; } = default!;

	[SupplyParameterFromQuery(Name = "demo")]
	private bool IsDemo { get; set; }

	private string _internalUrl = "";

	private bool _disposed;

	protected override void OnInitialized()
	{
		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;

		_matchService.OnNavigationRequested += HandleNavigationRequested;
		_zielService.OnNavigationRequested += HandleNavigationRequested;

		SetInternalUrl();
	}

	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
		_matchService.OnNavigationRequested -= HandleNavigationRequested;
		_zielService.OnNavigationRequested -= HandleNavigationRequested;
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;

		UpdateUrl();
	}

	private void HandleNavigationRequested(string url)
	{
		if (_disposed) return;

		UpdateUrl();
	}

	private void UpdateUrl()
	{
		var previous = _internalUrl;
		SetInternalUrl();

		// iframe nur neu laden, wenn sich die Ziel-URL tatsächlich geändert hat
		if (previous == _internalUrl) return;

		InvokeAsync(StateHasChanged);
	}

	private void SetInternalUrl()
	{
		// Settings werden bewusst mit angezeigt, damit sie von beiden Seiten lesbar sind
		// (ungespiegelt - eine gespiegelte Einstellungsliste wäre unlesbar).
		if (_settingsService.SettingsPageActive)
		{
			_internalUrl = "/settings?mirror=true";
			return;
		}

		var modus = _settingsService.CurrentSettings.Game.CurrentModus;
		var url = SettingsService.GetModusUrl(modus);

		// mirror=true heißt für die eingebettete Seite: reine Anzeige (keine Eingabe,
		// kein Fokus, keine Eigennavigation). Die Ziel-Modi werden dabei bewusst nicht
		// gespiegelt - Ziel.razor hat keine mirrored-CSS-Klasse.
		_internalUrl = IsDemo ? $"{url}?mirror=true&demo=true" : $"{url}?mirror=true";
	}
}
