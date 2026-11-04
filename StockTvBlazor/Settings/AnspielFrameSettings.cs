using System.Text.Json.Serialization;

namespace StockTvBlazor.Settings;

/// <summary>
/// Anspiel-Indikator: Die Mannschaft mit Anspiel kommt mit » bzw. « im Teamnamen an. Statt des
/// Zeichens kann ein Rahmen in der Teamnamen-Farbe angezeigt werden.
/// </summary>
/// <remarks>
/// Gilt fuer alle Themes, auch Hell und Dunkel - frueher hing die Einstellung am eigenen Theme
/// (siehe <see cref="UiSettings.MigrateLegacyAnspielFrame"/>). Rein lokale UI-Einstellung, nicht
/// Teil des NetMQ SetSettings/GetSettings-Protokolls.
/// </remarks>
public class AnspielFrameSettings
{
	public const int MinWidthPx = 1;
	public const int MaxWidthPx = 10;
	public const int DefaultWidthPx = 3;

	/// <summary>Rahmen statt Sonderzeichen anzeigen.</summary>
	public bool ShowFrame { get; set; } = true;

	/// <summary>Rahmenstaerke in Pixeln.</summary>
	public int FrameWidthPx { get; set; } = DefaultWidthPx;

	/// <summary>Die Rahmenstaerke, auf den erlaubten Bereich begrenzt - fuer eine von Hand
	/// bearbeitete Datei mit 0 oder 50.</summary>
	[JsonIgnore]
	public int EffectiveWidthPx => Math.Clamp(FrameWidthPx, MinWidthPx, MaxWidthPx);
}
