namespace StockTvBlazor.Settings;

public class ColorSettings
{
    private readonly UiSettings.Theme _theme;
    private readonly UiSettings.Richtung _richtung;

    public ColorSettings(UiSettings.Theme theme, UiSettings.Richtung richtung)
    {
        _theme = theme;
        _richtung = richtung;
    }

    public string BackgroundColor =>
        _theme == UiSettings.Theme.Hell ? "#ffffff" : "#000000";

    public string ForegroundColor =>
        _theme == UiSettings.Theme.Hell ? "#000000" : "#d3d3d3";

    public string ForegroundLeft => (_richtung, _theme) switch
    {
        (UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#ff0000",
        (UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#008000",
        (UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#ff0000",
        (UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#9acd32",
        _ => "#008000"
    };

    public string ForegroundRight => (_richtung, _theme) switch
    {
        (UiSettings.Richtung.Links, UiSettings.Theme.Hell) => "#008000",
        (UiSettings.Richtung.Rechts, UiSettings.Theme.Hell) => "#ff0000",
        (UiSettings.Richtung.Links, UiSettings.Theme.Dunkel) => "#9acd32",
        (UiSettings.Richtung.Rechts, UiSettings.Theme.Dunkel) => "#ff0000",
        _ => "#ff0000"
    };

    public string ZielSummeGesamt =>
        _theme == UiSettings.Theme.Hell ? "#8b008b" : "#ff00ff";

    public string ZielSummeEinzel =>
        _theme == UiSettings.Theme.Hell ? "#008b8b" : "#00ffff";
}