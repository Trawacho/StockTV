namespace StockTvBlazor.Settings
{
	/// <summary>
	/// Der gesamte Einstellungsbestand im Arbeitsspeicher. Auf der Platte liegt er in zwei
	/// Dateien, aufgeteilt danach, wer sie schreibt und wie oft (siehe <see cref="DeviceSettings"/>):
	/// <list type="bullet">
	/// <item><c>_config/stocktv.device.json</c> - <see cref="Device"/>, nur durch Installateur
	/// bzw. REST-API, im Spielbetrieb nie</item>
	/// <item><c>_config/stocktv.config.json</c> - <see cref="General"/>, <see cref="Game"/>,
	/// <see cref="UI"/>, durch Bedienung und zentrale Verwaltung</item>
	/// </list>
	/// Der Spielstand liegt in einer dritten Datei und gehoert bewusst nicht hierher, weil er
	/// keine Einstellung ist - siehe <c>Services/GameStateStore.cs</c>.
	/// </summary>
	public class Settings
	{
		public DeviceSettings Device { get; set; } = new();
		public GeneralSettings General { get; set; } = new();
		public GameSettings Game { get; set; } = new();
		public UiSettings UI { get; set; } = new();
	}

	/// <summary>Welche der Konfigurationsdateien ein Speichervorgang betrifft.</summary>
	/// <remarks>
	/// Ohne diese Unterscheidung wuerde jede Aenderung wieder alle Dateien schreiben - und die
	/// Gerätedatei waere nicht mehr die selten geschriebene, als die sie gedacht ist.
	/// </remarks>
	public enum SettingsScope
	{
		/// <summary>stocktv.config.json - General, Game, UI</summary>
		Config,

		/// <summary>stocktv.device.json - Protokollierung, Netzwerk, REST-Schnittstelle</summary>
		Device
	}
}
