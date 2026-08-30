using System.Text.Json.Serialization;
using StockTvBlazor.Services;

namespace StockTvBlazor.Settings
{
	/// <summary>
	/// Die REST-Schnittstelle, ueber die das zentrale Verwaltungsprogramm Spielstaende abruft,
	/// Einstellungen setzt und das Geraet verwaltet.
	/// </summary>
	/// <remarks>
	/// Laeuft bewusst auf einem eigenen Port und in einem eigenen Web-Host, getrennt von der
	/// Blazor-Anzeige auf 8080 (siehe <c>Api/StockTvApiHost.cs</c>). Damit ist die Schnittstelle
	/// aus einem Browser, der die Anzeige geoeffnet hat, nicht erreichbar - und ein belegter
	/// Port verhindert hoechstens die Fernsteuerung, nie die Anzeige.
	/// </remarks>
	public class RestApiSettings
	{
		public bool Enabled { get; set; } = true;

		/// <summary>0.0.0.0 = alle Schnittstellen, 127.0.0.1 = nur lokal.</summary>
		public string BindAddress { get; set; } = "0.0.0.0";

		public int Port { get; set; } = 8099;

		/// <summary>
		/// Pflicht, sobald die Schnittstelle ueber Loopback hinaus erreichbar ist - sonst waere
		/// der Neustart-Endpunkt ein Fernausschalter fuer jeden im Netz. Ist der Schluessel dann
		/// leer, startet die Schnittstelle gar nicht erst und schreibt den Grund ins Log.
		/// Wird beim Speichern verschluesselt abgelegt.
		/// </summary>
		[JsonConverter(typeof(SecretStringConverter))]
		public string ApiKey { get; set; } = "";

		public string ApiKeyHeader { get; set; } = "X-Api-Key";

		public bool SwaggerEnabled { get; set; } = true;

		public string SwaggerRoute { get; set; } = "swagger";
	}
}
