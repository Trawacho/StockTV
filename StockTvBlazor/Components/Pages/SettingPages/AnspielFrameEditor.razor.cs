using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages.SettingPages;

/// <summary>
/// Reiter "Anspiel" auf /themes: zentrale Einstellung des Anspiel-Rahmens fuer alle Themes.
/// </summary>
public partial class AnspielFrameEditor : IDisposable
{
	[Inject] private SettingsService SettingsService { get; set; } = default!;

	private bool _disposed;

	private AnspielFrameSettings Frame => SettingsService.CurrentSettings.UI.AnspielFrame;

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

	private void NotifyChanged()
	{
		// Auf den erlaubten Bereich ziehen, damit die Datei keinen Wert enthaelt, den das
		// Eingabefeld nicht zulaesst.
		Frame.FrameWidthPx = Frame.EffectiveWidthPx;
		SettingsService.NotifyAnspielFrameChanged();
	}

	private void Reset() => SettingsService.ResetAnspielFrame();
}
