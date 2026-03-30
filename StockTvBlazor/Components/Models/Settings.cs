using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockTvBlazor.Components.Models;

public class Settings
{

    /// <summary>
    /// Logging der Daten in eine Datei. 
	/// </summary>
    public bool FileLoggingEnabled { get; set; } = true;

    /// <summary>
    /// Auflistung der möglichen Darstellungsmodes: Hell oder Dunkel
    /// </summary>
    public enum THEME
	{
		HELL = 0,
		DUNKEL = 1
	}
	/// <summary>
	/// Auflistung der möglichen Richtungen: Links oder Rechts. Legt fest, welche Seite die "linke" und welche die "rechte" Seite ist. Das hat Auswirkungen auf die Farbdarstellung der Punkte.
	/// </summary>
	public enum RICHTUNG
	{
		LINKS = 0,
		RECHTS = 1
	}

	/// <summary>
	/// Auflistung der möglichen Modi: Trainingsmodus, Turniermodus, BestOf-Modus oder Zielmodus. Je nach Modus werden die Punkte und die Darstellung unterschiedlich gehandhabt.
	/// </summary>
	public enum MODUS
	{
		TRAINING = 0,
		BESTOF = 1,
		TURNIER = 2,
		ZIEL = 100
	}

	/// <summary>
	/// Darstellungmodus: Hell oder Dunkel
	/// </summary>
	public THEME Theme { get; set; } = THEME.HELL;
	/// <summary>
	/// Richtung: Links oder Rechts. Legt fest, welche Seite die "linke" und welche die "rechte" Seite ist. Das hat Auswirkungen auf die Farbdarstellung der Punkte.
	/// </summary>
	public RICHTUNG Richtung { get; set; } = RICHTUNG.LINKS;
	/// <summary>
	/// Modus: Trainingsmodus, Turniermodus, BestOf-Modus oder Zielmodus. Je nach Modus werden die Punkte und die Darstellung unterschiedlich gehandhabt.
	/// </summary>
	public MODUS Modus { get; set; } = MODUS.TRAINING;
	/// <summary>
	/// Jede Bahn hat eine Nummer. 
	/// </summary>
	public int BahnNummer { get; set; } = 1;
	/// <summary>
	/// Spielgruppe: Jeder Bahn gehört zu einer Spiegruppe. Defaultmäßig ist die Spielgrupe 0
	/// </summary>
	public int Spielgruppe { get; set; } = 0;
	/// <summary>
	/// Maximalwert der Punkte pro Kehre.
	/// </summary>
	public int MaxPunkteProKehre { get; set; } = 10;
	/// <summary>
	/// Wieviel Kehren pro Spiel, damit mit der GELBEN Taste in das nächste Spiel gewechselt werden kann. Hat im Trainingsmodues keine Auswirkung
	/// </summary>
	public int MaxKehrenProSpiel { get; set; } = 6;
	/// <summary>
	/// Ob die Anwendung die Daten per Netzwerk empfangen und sennden kann/soll.
	/// </summary>
	public bool Networking { get; set; } = false;
	/// <summary>
	/// Wenn True, ist dann schnelle löschen mit 0 im inputValue nicht möglich. Auch kann man nicht in die ConfigPage wechseln
	/// </summary>
	public bool BlockLocalChanges { get; set; } = false;
	/// <summary>
	/// Hier wird festgelegt, welche Version der Nachrichtenschnittstelle verwendet wird.
	/// </summary>
	public int MessageVersion => 1;

	/// <summary>
	// Breite der mittleren Spalte.
	// Ein Wert von 90 bedeutet, dass 90% der Breite für die mittlere Spalte verwendet wird, die restlichen 10% werden gleichmäßig auf die linke und rechte Spalte verteilt.
	// Dieser Wert hat nur Auswirkungen, wenn TeamNamen angezeigt werden.
	/// </summary>
	public int MidColumnWidth { get; set; } = 90;

    [JsonIgnore]
    public List<Turn> Kehren { get; set; } = [];

	[JsonIgnore]
	public string SpielgruppeLetter => Spielgruppe switch
	{
		0 => "",
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

	#region Color Properties

	[JsonIgnore]
	public Color BackgroundColor => Theme switch
	{
		THEME.HELL => Color.White,
		_ => Color.Black
	};

	[JsonIgnore]
	public Color ForegroundColorRight => (Richtung, Theme) switch
	{
		(RICHTUNG.LINKS, THEME.HELL) => Color.Green,
		(RICHTUNG.RECHTS, THEME.HELL) => Color.Red,
		(RICHTUNG.LINKS, THEME.DUNKEL) => Color.YellowGreen,
		(RICHTUNG.RECHTS, THEME.DUNKEL) => Color.Red,
		_ => Color.Red
	};

	[JsonIgnore]
	public Color ForegroundColorLeft => (Richtung, Theme) switch
	{
		(RICHTUNG.LINKS, THEME.HELL) => Color.Red,
		(RICHTUNG.RECHTS, THEME.HELL) => Color.Green,
		(RICHTUNG.LINKS, THEME.DUNKEL) => Color.Red,
		(RICHTUNG.RECHTS, THEME.DUNKEL) => Color.YellowGreen,
		_ => Color.Green
	};

	[JsonIgnore]
    public Color ForegroundColor => Theme switch
    {
        THEME.HELL => Color.Black,
        _ => Color.White
    }; 
	//public Color ForegroundColor => Theme switch
 //   {
 //       THEME.HELL => Color.Black,
 //       _ => Color.LightGray
 //   };

    [JsonIgnore]
	public Color ForegroundZielSummeGesamt => Theme switch
	{
		THEME.HELL => Color.DarkMagenta,
		_ => Color.Magenta
	};

	[JsonIgnore]
	public Color ForegroundZielSummeEinzel => Theme switch
	{
		THEME.HELL => Color.DarkCyan,
		_ => Color.Cyan
	};

    #endregion


}

