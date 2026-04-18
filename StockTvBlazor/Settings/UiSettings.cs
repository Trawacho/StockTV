namespace StockTvBlazor.Settings;

public class UiSettings
{
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

    public Theme CurrentTheme { get; set; } = Theme.Hell;
    public Richtung CurrentRichtung { get; set; } = Richtung.Links;

    public int MidColumnWidth { get; set; } = 90;

    public ColorSettings Colors => new(CurrentTheme, CurrentRichtung);
}