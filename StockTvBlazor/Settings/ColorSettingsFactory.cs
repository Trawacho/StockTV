namespace StockTvBlazor.Settings;

public static class ColorSettingsFactory
{
	public static ColorSettings FromTheme(UiSettings.Theme theme, UiSettings.Richtung richtung)
	{
		return new ColorSettings
		{
			BackgroundColor = theme == UiSettings.Theme.Hell ? "#ffffff" : "#000000",
			ForegroundColor = theme == UiSettings.Theme.Hell ? "#000000" : "#d3d3d3",

			ForegroundA = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#9acd32",
				_ => "#008000"
			},

			ForegroundB = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#9acd32",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#ff0000",
				_ => "#ff0000"
			},

			TeamNameA = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#ff0000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#9acd32",
				_ => "#008000"
			},

			TeamNameB = (richtung, theme) switch
			{
				(UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#008000",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#ff0000",
				(UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#9acd32",
				(UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#ff0000",
				_ => "#ff0000"
			},

			ZielSummeGesamt = theme == UiSettings.Theme.Hell ? "#8b008b" : "#ff00ff",
			ZielSummeEinzel = theme == UiSettings.Theme.Hell ? "#008b8b" : "#00ffff",
			ZielSpielername = theme == UiSettings.Theme.Hell ? "#8b008b" : "#ff00ff"
		};
	}

	/// <summary>
	/// Swaps ForegroundA and ForegroundB colors
	/// Used for CustomTheme when orientation is Rechts
	/// </summary>
	public static ColorSettings SwapLeftRight(ColorSettings colors)
	{
		return new ColorSettings
		{
			BackgroundColor = colors.BackgroundColor,
			ForegroundColor = colors.ForegroundColor,
			ForegroundA = colors.ForegroundB,    // Swap
			ForegroundB = colors.ForegroundA,    // Swap
			TeamNameA = colors.TeamNameB,        // Swap
			TeamNameB = colors.TeamNameA,        // Swap
			ZielSummeGesamt = colors.ZielSummeGesamt,
			ZielSummeEinzel = colors.ZielSummeEinzel,
			ZielSpielername = colors.ZielSpielername,
			FontFamily = colors.FontFamily
		};
	}
}
