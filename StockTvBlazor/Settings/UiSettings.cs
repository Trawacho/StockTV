using System.Text.Json.Serialization;

namespace StockTvBlazor.Settings;

public class UiSettings
{
	// Bleibt für ColorSettingsFactory (Hell/Dunkel-Logik)
	public enum Theme
	{
		Hell,
		Dunkel
	}

	public enum Richtung
	{
		Links,
		Rechts
	}

	public Richtung CurrentRichtung { get; set; } = Richtung.Links;
	public int MidColumnWidth { get; set; } = 90;
	public TableLayoutSettings TableLayout { get; set; } = new();
	public CellFontWeightSettings CellFontWeight { get; set; } = new();
	public string FontFamily { get; set; } = "";
	public AnspielFrameSettings AnspielFrame { get; set; } = new();

	public Guid? ActiveThemeId { get; set; }
	public List<CustomTheme> CustomThemes { get; set; } = new();

	// Stabile IDs für Built-in Themes
	public static readonly Guid HellThemeId = new("00000000-0000-0000-0000-000000000001");
	public static readonly Guid DunkelThemeId = new("00000000-0000-0000-0000-000000000002");

	private static readonly List<BuiltInTheme> _builtInThemes =
	[
		new BuiltInTheme { Id = HellThemeId,   Name = "Hell",   ThemeType = Theme.Hell },
		new BuiltInTheme { Id = DunkelThemeId, Name = "Dunkel", ThemeType = Theme.Dunkel }
	];

	/// <summary>
	/// Alle verfügbaren Themes: Built-in + Custom – einheitlich als ITheme-Liste.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyList<ITheme> AllThemes =>
	[
		.. _builtInThemes,
		.. CustomThemes
	];

	/// <summary>
	/// Das aktuell aktive Theme. Fallback: Hell.
	/// </summary>
	[JsonIgnore]
	public ITheme ActiveTheme =>
		AllThemes.FirstOrDefault(t => t.Id == ActiveThemeId)
		?? _builtInThemes[0];

	/// <summary>
	/// Die aktuellen Farben basierend auf aktivem Theme und Richtung.
	/// </summary>
	[JsonIgnore]
	public ColorSettings Colors => ActiveTheme.GetColors(CurrentRichtung);

	/// <summary>
	/// Aktiviert ein Theme anhand der ID (Built-in oder Custom).
	/// </summary>
	public void ActivateTheme(Guid id)
	{
		if (AllThemes.Any(t => t.Id == id))
			ActiveThemeId = id;
	}

	/// <summary>
	/// Fügt ein neues Custom Theme hinzu und aktiviert es optional direkt.
	/// </summary>
	public void AddCustomTheme(CustomTheme theme, bool activate = false)
	{
		CustomThemes.Add(theme);
		if (activate)
			ActiveThemeId = theme.Id;
	}

	/// <summary>
	/// Uebernimmt die frueher pro Theme gespeicherte Rahmenstaerke in die zentrale Einstellung.
	/// </summary>
	/// <remarks>
	/// Eingeschaltet ist der Rahmen jetzt standardmaessig fuer alle - uebernommen wird deshalb nur
	/// die Staerke eines Themes, das ihn schon bisher zeigte (das aktive hat Vorrang). Die alten
	/// Werte werden danach entfernt; ein zweiter Aufruf aendert nichts mehr.
	/// </remarks>
	/// <returns>true, wenn alte Werte gefunden wurden.</returns>
	public bool MigrateLegacyAnspielFrame()
	{
		var legacy = CustomThemes.Where(t => t.LegacyShowFrameOnAnspiel is not null || t.LegacyFrameWidthPx is not null).ToList();
		if (legacy.Count == 0)
			return false;

		var source = legacy
			.Where(t => t.LegacyShowFrameOnAnspiel == true && t.LegacyFrameWidthPx is not null)
			.OrderByDescending(t => t.Id == ActiveThemeId)
			.FirstOrDefault();

		if (source is not null)
			AnspielFrame.FrameWidthPx = source.LegacyFrameWidthPx!.Value;

		foreach (var theme in legacy)
		{
			theme.LegacyShowFrameOnAnspiel = null;
			theme.LegacyFrameWidthPx = null;
		}

		return true;
	}

	/// <summary>
	/// Entfernt ein Custom Theme. Falls es aktiv war, wird auf Hell zurückgefallen.
	/// </summary>
	public void RemoveCustomTheme(Guid id)
	{
		CustomThemes.RemoveAll(t => t.Id == id);

		if (ActiveThemeId == id)
			ActiveThemeId = HellThemeId;
	}
}
