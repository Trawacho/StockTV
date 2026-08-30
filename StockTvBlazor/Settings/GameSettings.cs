namespace StockTvBlazor.Settings;

public class GameSettings
{
	public enum Modus
	{
		Training = 0,
		BestOf = 1,
		Turnier = 2,
		Ziel = 100,
		Ziel2 = 101
	}

	public Modus CurrentModus { get; set; } = Modus.Training;

	public int MaxPunkteProKehre { get; set; } = 10;
	public int MaxKehrenProSpiel { get; set; } = 6;

	// Hier stand eine Liste "Kehren", die den Spielstand ueber einen Neustart retten sollte. Sie
	// trug seit jeher [JsonIgnore], landete also nie in der Datei - das Wiederherstellen beim
	// Start lief immer ins Leere, waehrend jede bestaetigte Kehre trotzdem einen vollstaendigen
	// Schreibvorgang ausloeste. Der Spielstand lebt allein in Match.Games.
}
