using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Layout;

/// <summary>
/// Meldet dem <see cref="SettingsService"/>, welche Seite im Bedienfenster offen ist, damit
/// die zweite Anzeige (Display2) ihr folgen kann. Kopflos - rendert nichts und liegt deshalb
/// im MainLayout, wie der ThemeHandler.
///
/// Ausgenommen sind die Spiegelinstanzen selbst: Display2 und die von ihm eingebetteten
/// Seiten (<c>?mirror=true</c>) duerfen sich nicht melden, sonst saehe die zweite Anzeige
/// sich selbst als Hauptfenster und wuerde sich rekursiv spiegeln.
/// </summary>
public sealed class MainWindowTracker : ComponentBase, IDisposable
{
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;

	[Inject] private SettingsService SettingsService { get; set; } = default!;

	private bool _disposed;

	protected override void OnInitialized()
	{
		// Nur echte, interaktive Fenster melden. Beim Prerendering laeuft OnInitialized auch
		// fuer jeden schlichten HTTP-Abruf - unter anderem fuer die Bereitschaftspruefung des
		// Kiosks auf "/". Ohne diesen Riegel wuerde die zweite Anzeige auf eine Seite
		// umschalten, die gar niemand ansieht.
		if (!RendererInfo.IsInteractive)
			return;

		NavigationManager.LocationChanged += HandleLocationChanged;
		Report(NavigationManager.Uri);
	}

	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		if (_disposed) return;

		Report(e.Location);
	}

	private void Report(string absoluteUri)
	{
		string relative = "/" + NavigationManager.ToBaseRelativePath(absoluteUri);

		if (IsMirrorInstance(relative))
			return;

		SettingsService.ReportMainWindowUrl(relative);
	}

	/// <summary>Erkennt die Spiegel-Anzeige und die von ihr eingebetteten Seiten.</summary>
	internal static bool IsMirrorInstance(string relativeUrl)
		=> relativeUrl.Contains("mirror=true", StringComparison.OrdinalIgnoreCase)
		|| relativeUrl.TrimStart('/').StartsWith("display2", StringComparison.OrdinalIgnoreCase);

	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;
		NavigationManager.LocationChanged -= HandleLocationChanged;
	}
}
