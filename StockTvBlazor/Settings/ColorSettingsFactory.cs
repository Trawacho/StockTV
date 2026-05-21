namespace StockTvBlazor.Settings;

public static class ColorSettingsFactory
{
	public static ColorSettings FromTheme(UiSettings.Theme theme, UiSettings.Richtung richtung)
	{
		return new ColorSettings
		{
			BackgroundColor = theme == UiSettings.Theme.Hell ? "#ffffff" : "#000000",
			ForegroundColor = theme == UiSettings.Theme.Hell ? "#000000" : "#d3d3d3",

			ForegroundLeft = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#9acd32",
				_ => "#008000"
			},

			ForegroundRight = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#9acd32",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#ff0000",
				_ => "#ff0000"
			},

			ZielSummeGesamt = theme == UiSettings.Theme.Hell ? "#8b008b" : "#ff00ff",
			ZielSummeEinzel = theme == UiSettings.Theme.Hell ? "#008b8b" : "#00ffff"
		};
	}

	/// <summary>
	/// Swaps ForegroundLeft and ForegroundRight colors
	/// Used for CustomTheme when orientation is Rechts
	/// </summary>
	public static ColorSettings SwapLeftRight(ColorSettings colors)
	{
		return new ColorSettings
		{
			BackgroundColor = colors.BackgroundColor,
			ForegroundColor = colors.ForegroundColor,
			ForegroundLeft = colors.ForegroundRight,    // Swap
			ForegroundRight = colors.ForegroundLeft,    // Swap
			ZielSummeGesamt = colors.ZielSummeGesamt,
			ZielSummeEinzel = colors.ZielSummeEinzel
		};
	}
}
