using System.Text.Json.Serialization;

namespace StockTvBlazor.Models
{
	public class Turn
	{
		[JsonConstructor]
		private Turn()
		{

		}

		public int TurnNumber { get; set; }
		public int PointsRight { get; set; } = 0;
		public int PointsLeft { get; set; } = 0;

		public static Turn Create(int value, Settings.UiSettings.Richtung richtung, bool isGreen)
		{
			// Logik: 
			// Wenn Richtung LINKS: Green ist Rechts (PointsRight), Red ist Links (PointsLeft).
			// Wenn Richtung RECHTS: Green ist Links (PointsLeft), Red ist Rechts (PointsRight).

			bool assignedToRight;
			if (isGreen)
			{
				assignedToRight = (richtung == Settings.UiSettings.Richtung.Links);
			}
			else // Red
			{
				assignedToRight = (richtung == Settings.UiSettings.Richtung.Rechts);
			}

			return new Turn
			{
				PointsRight = assignedToRight ? value : 0,
				PointsLeft = !assignedToRight ? value : 0
			};
		}
	}
}
