namespace StockTvBlazor.Settings
{
	/// <summary>
	/// Alles, was zum Geraet gehoert und nicht zum Spiel: Protokollierung, Netzwerk und die
	/// REST-Schnittstelle. Wird in einer eigenen Datei (<c>_config/stocktv.device.json</c>)
	/// abgelegt.
	/// </summary>
	/// <remarks>
	/// Die Trennung ist keine Kosmetik. Diese Datei enthaelt als einzige ein Geheimnis (den
	/// API-Schluessel) und wird als einzige im Spielbetrieb nie geschrieben - waehrend der
	/// Spielstand pro Kehre auf die Platte geht. Laege beides zusammen, koennte ein Stromausfall
	/// waehrend eines Spiels den Schluessel mitreissen und das Geraet aus der Ferne unerreichbar
	/// machen.
	///
	/// Daraus folgt die Regel fuer die Zuordnung: Alles, was <c>SetSettings</c> aus dem Netz
	/// oder die Einstellungsseite aendern kann, gehoert NICHT hierher, sondern in
	/// <c>stocktv.config.json</c>. Deshalb steht z.B. die Bahnnummer dort, obwohl sie nach
	/// Geraeteidentitaet klingt.
	/// </remarks>
	public class DeviceSettings
	{
		public bool FileLoggingEnabled { get; set; } = true;

		public NetworkSettings Network { get; set; } = new();

		public RestApiSettings RestApi { get; set; } = new();
	}
}
