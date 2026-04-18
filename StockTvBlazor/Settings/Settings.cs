namespace StockTvBlazor.Settings
{
    public class Settings
    {
        public GeneralSettings General { get; set; } = new();
        public GameSettings Game { get; set; } = new();
        public UiSettings UI { get; set; } = new();
        public NetworkSettings Network { get; set; } = new();
    }
}
