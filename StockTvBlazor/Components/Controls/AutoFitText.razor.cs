using Microsoft.AspNetCore.Components;

namespace StockTvBlazor.Components.Controls;

/// <summary>
/// Rein deklarative, JS-freie Textskalierung. Die Schriftgröße wird via CSS
/// Container Query Units berechnet (siehe wwwroot/css/StockTV_AutoFit.css).
/// Da die App serverseitig rendert, ist die Textlänge bereits bekannt und wird
/// als CSS-Variable <c>--len</c> übergeben, sodass langer Text schrumpft.
/// </summary>
public partial class AutoFitText
{
	/// <summary>Anzuzeigender Text; bestimmt zugleich <c>--len</c>.</summary>
	[Parameter] public string? Text { get; set; }

	/// <summary>Minimale Schriftgröße in px (Ersatz für das frühere data-autofit-min).</summary>
	[Parameter] public int Min { get; set; } = 8;

	/// <summary>Vertikaler Text (writing-mode vertical-*), z. B. Teamnamen.</summary>
	[Parameter] public bool Vertical { get; set; }

	/// <summary>Optionale zusätzliche CSS-Klassen.</summary>
	[Parameter] public string? Class { get; set; }

	private int Len => System.Math.Max(1, (Text ?? string.Empty).Length);

	private string CssClass =>
		"autofit"
		+ (Vertical ? " vertical" : string.Empty)
		+ (string.IsNullOrEmpty(Class) ? string.Empty : " " + Class);

	private string Style =>
		System.FormattableString.Invariant($"--len:{Len}; --min:{Min}px");
}
