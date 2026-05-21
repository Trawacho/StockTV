namespace StockTvBlazor.Settings;

public interface ITheme
{
	Guid Id { get; set; }
	string Name { get; set; }
	bool IsBuiltIn { get; }
	ColorSettings GetColors(UiSettings.Richtung richtung);
}

public abstract class ThemeBase
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = "";
	public virtual bool IsBuiltIn => false;

	public abstract ColorSettings GetColors(UiSettings.Richtung richtung);
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
	/// Single color set - Richtung determines Left/Right assignment
	/// </summary>
	public ColorSettings Colors { get; set; } = new();

	public ColorSettings GetColors(UiSettings.Richtung richtung)
	{
		if (richtung == UiSettings.Richtung.Links)
			return Colors;

		// Swap Left/Right for Rechts orientation
		return ColorSettingsFactory.SwapLeftRight(Colors);
	}
}
