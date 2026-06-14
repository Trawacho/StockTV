using System.Text.Json.Serialization;
using StockTvBlazor.Models;

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

	[JsonIgnore]
	public List<Turn> Kehren { get; set; } = new();
}
