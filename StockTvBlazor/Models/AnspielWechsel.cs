namespace StockTvBlazor.Models;

/// <summary>
/// Anspiel je Kehre (IER 422): Die mit » bzw. « markierte Mannschaft spielt Kehre 1 und alle ungeraden
/// Kehren an, die andere alle geraden. Die zentrale Verwaltung markiert nur das Anspiel der ersten Kehre;
/// hier wandert die Markierung in geraden Kehren auf die andere Seite. Rahmen und Zeichen hängen an der
/// Markierung und ziehen dadurch mit.
/// </summary>
public static class AnspielWechsel
{
	public const char ZeichenRechts = '»';
	public const char ZeichenLinks = '«';

	public static bool IstMarkiert(string? name) =>
		!string.IsNullOrEmpty(name) && (name.Contains(ZeichenRechts) || name.Contains(ZeichenLinks));

	public static string OhneZeichen(string name) =>
		name.Replace(ZeichenRechts.ToString(), "").Replace(ZeichenLinks.ToString(), "").Trim();

	/// <summary>
	/// Namen links und rechts mit der Anspiel-Markierung für die laufende Kehre. In ungeraden Kehren bleibt
	/// alles, wie es gesendet wurde; in geraden wechselt die Markierung auf die andere Mannschaft – das
	/// Zeichen steht wieder am äußeren Rand (links « vorn, rechts » hinten).
	/// </summary>
	public static (string Links, string Rechts) FuerKehre(string links, string rechts, int laufendeKehre)
	{
		if (laufendeKehre % 2 != 0 || string.IsNullOrEmpty(links) || string.IsNullOrEmpty(rechts))
			return (links, rechts);

		var linksMarkiert = IstMarkiert(links);
		if (linksMarkiert == IstMarkiert(rechts))
			return (links, rechts); // keine oder beide markiert: nichts zu wechseln

		return linksMarkiert
			? (OhneZeichen(links), $"{OhneZeichen(rechts)} {ZeichenRechts}")
			: ($"{ZeichenLinks} {OhneZeichen(links)}", OhneZeichen(rechts));
	}
}
