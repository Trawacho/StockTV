using System.Text.Json.Serialization;

namespace StockTvBlazor.Settings;

public interface ITheme
{
	Guid Id { get; set; }
	string Name { get; set; }
	bool IsBuiltIn { get; }
	ColorSettings GetColors(UiSettings.Richtung richtung);
}

public class BuiltInTheme : ITheme
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = "";
	public bool IsBuiltIn => true;
	public UiSettings.Theme ThemeType { get; set; }

	public ColorSettings GetColors(UiSettings.Richtung richtung)
		=> ColorSettingsFactory.FromTheme(ThemeType, richtung);
}

public class CustomTheme : ITheme
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = "";
	public bool IsBuiltIn => false;

	/// <summary>
	/// Optional: Welches Standard-Theme (Hell/Dunkel) ist die Basis dieses Custom Themes
	/// </summary>
	public UiSettings.Theme? BaseTheme { get; set; }

	/// <summary>
	/// Single color set - Richtung determines A/B assignment
	/// </summary>
	public ColorSettings Colors { get; set; } = new();

	/// <summary>
	/// Nur noch zum Einlesen aelterer Dateien: Der Anspiel-Rahmen hing frueher am Theme und gilt
	/// jetzt fuer alle (<see cref="UiSettings.AnspielFrame"/>). Wird beim Laden uebernommen und
	/// danach auf null gesetzt - damit faellt er beim naechsten Speichern aus der Datei.
	/// </summary>
	[JsonPropertyName("ShowFrameOnAnspiel")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? LegacyShowFrameOnAnspiel { get; set; }

	/// <inheritdoc cref="LegacyShowFrameOnAnspiel"/>
	[JsonPropertyName("FrameWidthPx")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? LegacyFrameWidthPx { get; set; }

	public ColorSettings GetColors(UiSettings.Richtung richtung)
	{
		if (richtung == UiSettings.Richtung.Links)
			return Colors;

		// Swap A/B for Rechts orientation
		return ColorSettingsFactory.SwapLeftRight(Colors);
	}
}
