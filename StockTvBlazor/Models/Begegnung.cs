namespace StockTvBlazor.Models
{
	public interface IBegegnung
	{
		int Spielnummer { get; set; }

		string TeamNameLeft(bool colorSchemeRightToLeft);
		string TeamNameRight(bool colorSchemeRightToLeft);
	}

	public class Begegnung(int spielNummer, string TeamA, string TeamB) : IBegegnung
	{
		public int Spielnummer { get; set; } = spielNummer;
		private readonly string _mannschaft_A = TeamA;
		private readonly string _mannschaft_B = TeamB;

		public string TeamNameLeft(bool isColorSchemeRightToLeft)
		{
			return isColorSchemeRightToLeft ? _mannschaft_B : _mannschaft_A;
		}

		public string TeamNameRight(bool isColorSchemeRightToLeft)
		{
			return isColorSchemeRightToLeft ? _mannschaft_A : _mannschaft_B;
		}
	}
}
