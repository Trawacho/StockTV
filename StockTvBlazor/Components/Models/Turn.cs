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
		private Turn()
		{

		}

		public int TurnNumber { get; set; }
		public int PointsRight { get; set; } = 0;
		public int PointsLeft { get; set; } = 0;

		public static Turn Create(int value, Settings.RICHTUNG richtung, bool isGreen)
		{
			// Logik: 
			// Wenn Richtung LINKS: Green ist Rechts (PointsRight), Red ist Links (PointsLeft).
			// Wenn Richtung RECHTS: Green ist Links (PointsLeft), Red ist Rechts (PointsRight).

			bool assignedToRight;
			if (isGreen)
			{
				assignedToRight = (richtung == Settings.RICHTUNG.LINKS);
			}
			else // Red
			{
				assignedToRight = (richtung == Settings.RICHTUNG.RECHTS);
			}

			return new Turn
			{
				PointsRight = assignedToRight ? value : 0,
				PointsLeft = !assignedToRight ? value : 0
			};
		}
	}
}
