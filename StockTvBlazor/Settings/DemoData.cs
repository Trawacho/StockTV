namespace StockTvBlazor.Settings;

internal static class DemoData
{
	// Gemeinsam (Training, Turnier, BestOf)
	public const int LeftPointsSum = 8;
	public const int RightPointsSum = 5;
	public const string LeftPoints = "4-0-4-0";
	public const string RightPoints = "0-2-0-3";
	public const string InputValue = "4";
	public const string LeftTeamName = "Team Links";
	public const string RightTeamName = "Team Rechts";
	public const int KehreAnzahl = 4;
	public const int SpielNummer = 2;

	// BestOf
	public const int LeftMatchPoints = 3;
	public const int RightMatchPoints = 1;

	// Ziel
	public const string ZielSpielerName = "Max Mustermann";
	public const int ZielMassenVorneSumme = 24;
	public const int ZielSchiessenSumme = 17;
	public const int ZielMassenSeiteSumme = 0;
	public const int ZielKombinierenSumme = 0;
	public const string ZielAnzahlVersuche = "6/12";
	public const string ZielLastValue = "8";

	public static int ZielGesamtSumme =>
		ZielMassenVorneSumme + ZielSchiessenSumme + ZielMassenSeiteSumme + ZielKombinierenSumme;

	public static string ZielSummeDerVersuche =>
		$"{ZielMassenVorneSumme} - {ZielSchiessenSumme} - {ZielMassenSeiteSumme} - {ZielKombinierenSumme}";
}
