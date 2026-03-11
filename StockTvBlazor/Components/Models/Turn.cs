namespace StockTvBlazor.Components.Models
{
	public interface ITurn
	{
		int PointsLeft { get; set; }
		int PointsRight { get; set; }
		int TurnNmber { get; set; }
	}

	public class Turn : ITurn
	{
		public Turn()
		{

		}

		public int TurnNmber { get; set; }
		public int PointsRight { get; set; } = 0;
		public int PointsLeft { get; set; } = 0;
	}
}
