using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

/// <summary>
/// Zeigt das von der zentralen Verwaltung gesetzte Werbebild formatfuellend an.
/// </summary>
/// <remarks>
/// Betreten wird die Seite ueber NetMQ <c>GoToImage</c>, verlassen ueber <c>ClearImage</c> -
/// beides laeuft ueber <see cref="SettingsService.RequestNavigation"/>. Eine Eigennavigation
/// gibt es nicht; im gespiegelten Fenster (<c>?mirror=true</c>) ist sie ohnehin unerwuenscht,
/// dort tauscht Display2 selbst die iframe-Adresse aus.
/// </remarks>
public partial class Marketing : IDisposable
{
	[Inject] private MarketingImageService _marketingImageService { get; set; } = default!;
	[Inject] private SettingsService _settingsService { get; set; } = default!;
	[Inject] private NavigationManager _navigation { get; set; } = default!;

	[SupplyParameterFromQuery(Name = "mirror")]
	private bool IsMirrored { get; set; }

	private bool _disposed;

	private bool HasImage => _marketingImageService.HasImage;

	/// <summary>
	/// Zeitstempel als Query-Parameter, damit der Browser ein ausgetauschtes Bild nicht aus dem
	/// Zwischenspeicher zeigt - die Adresse bleibt sonst dieselbe.
	/// </summary>
	private string ImageUrl => $"/marketing/image?v={_version}";

	private long _version = DateTimeOffset.UtcNow.Ticks;

	protected override void OnInitialized()
	{
		_marketingImageService.OnChanged += HandleImageChanged;
		_settingsService.OnNavigationRequested += HandleNavigationRequested;
	}

	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;
		_marketingImageService.OnChanged -= HandleImageChanged;
		_settingsService.OnNavigationRequested -= HandleNavigationRequested;
	}

	private void HandleImageChanged()
	{
		if (_disposed) return;

		_version = DateTimeOffset.UtcNow.Ticks;
		InvokeAsync(StateHasChanged);
	}

	private void HandleNavigationRequested(string url)
	{
		// Im Spiegelfenster nie selbst navigieren: Display2 tauscht die iframe-Adresse aus, zwei
		// gleichzeitige Navigationen im selben Dokument enden als JS-Ausnahme (siehe CLAUDE.md).
		if (_disposed || IsMirrored) return;

		InvokeAsync(() => _navigation.NavigateTo(url));
	}
}
