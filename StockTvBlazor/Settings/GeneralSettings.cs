using System.Text.Json.Serialization;

namespace StockTvBlazor.Settings
{
	public class GeneralSettings
	{
		[JsonIgnore]
		public bool BlockLocalChanges { get; set; } = false;

		public int BahnNummer { get; set; } = 1;
		public int Spielgruppe { get; set; } = 0;

		// Nur-Lesen und damit beim Laden ohnehin verworfen - ohne JsonIgnore stuende der Wert
		// trotzdem in der Datei und liesse sich dort scheinbar aendern.
		[JsonIgnore]
		public int MessageVersion => 1;

		[JsonIgnore]
		public string SpielgruppeLetter => Spielgruppe switch
		{
			1 => "A",
			2 => "B",
			3 => "C",
			4 => "D",
			5 => "E",
			6 => "F",
			7 => "G",
			8 => "H",
			9 => "I",
			10 => "J",
			_ => ""
		};
	}
}
