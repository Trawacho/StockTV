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

	/// <summary>
	/// Text besteht nur aus schmalen Zeichen (Ziffern, "-", "/", Leerzeichen), z. B. Punktestände.
	/// Nutzt exakte, zur Laufzeit gemessene Font-Metriken (cap-height, Ziffernbreite)
	/// statt geschätzter Faktoren (siehe wwwroot/css/StockTV_AutoFit.css).
	/// </summary>
	[Parameter] public bool Numeric { get; set; }

	/// <summary>
	/// Ziel-Ausnutzung des Containers in Prozent (nur bei <see cref="Numeric"/> relevant).
	/// Sicherheitsmarge unterhalb der theoretischen 100%-Grenze gegen Rundungsfehler
	/// und Font-Overshoot (z. B. bei runden Ziffern wie "0"/"8").
	/// </summary>
	[Parameter] public int Fill { get; set; } = 96;

	/// <summary>Optionale zusätzliche CSS-Klassen.</summary>
	[Parameter] public string? Class { get; set; }

	private int Len => System.Math.Max(1, (Text ?? string.Empty).Length);

	private string CssClass =>
		"autofit"
		+ (Vertical ? " vertical" : string.Empty)
		+ (Numeric ? " numeric" : string.Empty)
		+ (string.IsNullOrEmpty(Class) ? string.Empty : " " + Class);

	private string Style =>
		System.FormattableString.Invariant($"--len:{Len}; --min:{Min}px; --fill:{Fill}");
}
