using System.Text.Json.Serialization;

namespace StockTvBlazor.Components.Models
{
	[JsonDerivedType(typeof(Turn), typeDiscriminator: "Kehre")]
	public interface ITurn
	{
		int PointsLeft { get; set; }
		int PointsRight { get; set; }
		int TurnNumber { get; set; }
	}

	public class Turn : ITurn
	{
		public Turn()
		{

		}

		public int TurnNumber { get; set; }
		public int PointsRight { get; set; } = 0;
		public int PointsLeft { get; set; } = 0;
	}
}
