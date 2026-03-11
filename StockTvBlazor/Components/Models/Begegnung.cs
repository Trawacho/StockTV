namespace StockTvBlazor.Components.Models
{
	public interface IBegegnung
	{
		byte Spielnummer { get; set; }

		string TeamNameLeft(bool colorSchemeRightToLeft);
		string TeamNameRight(bool colorSchemeRightToLeft);
	}

	public class Begegnung : IBegegnung
	{
		public Begegnung(byte spielNummer, string TeamA, string TeamB)
		{
			Spielnummer = spielNummer;
			_mannschaft_A = TeamA;
			_mannschaft_B = TeamB;
		}

		public byte Spielnummer { get; set; }
		private readonly string _mannschaft_A;
		private readonly string _mannschaft_B;

		public string TeamNameLeft(bool colorSchemeRightToLeft)
		{
			return colorSchemeRightToLeft ? _mannschaft_B : _mannschaft_A;
		}

		public string TeamNameRight(bool colorSchemeRightToLeft)
		{
			return colorSchemeRightToLeft ? _mannschaft_A : _mannschaft_B;
		}
	}
}
