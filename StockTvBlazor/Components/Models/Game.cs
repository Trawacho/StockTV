namespace StockTvBlazor.Components.Models
{
	public class Game
	{
		public Game(Settings settings, int gameNumber)
		{
			GameNumber = gameNumber;
			_settings = settings;
		}

		private readonly List<ITurn> _turns = [];
		public List<ITurn> Turns
		{
			get
			{
				return _turns;
			}
		}
		public int GameNumber { get; }

		private readonly Settings _settings;

		public int GamePointsLeft
		{
			get
			{
				if (Turns.Count < _settings.GameSettings.TurnsPerGame)
					return 0;

				if (LeftPointsSum > RightPointsSum)
				{
					return 2;
				}
				else if (LeftPointsSum == RightPointsSum)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}
		public int GamePointsRight
		{
			get
			{
				if (Turns.Count < _settings.GameSettings.TurnsPerGame)
					return 0;

				if (RightPointsSum > LeftPointsSum)
				{
					return 2;
				}
				else if (LeftPointsSum == RightPointsSum)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}

		}

		private int LeftPointsSum
		{
			get
			{
				return Turns.Sum(t => t.PointsLeft);
			}
		}

		private int RightPointsSum
		{
			get
			{
				return Turns.Sum(t => t.PointsRight);
			}
		}

		public void DeleteLastTurn()
		{
			if (Turns.Count > 0)
			{
				Turns.RemoveAt(Turns.Count - 1);
			}
		}

	}
}
