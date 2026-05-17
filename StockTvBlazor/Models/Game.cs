namespace StockTvBlazor.Models
{
	public class Game
	{
		private readonly Settings.Settings _settings;
		private readonly List<Turn> _turns = [];

		public Game(Settings.Settings settings, int gameNumber)
		{
			_settings = settings;
			GameNumber = gameNumber;
		}
		public Game(int gameNumber)
		{
			GameNumber = gameNumber;
			_settings = new();
		}
		public int GameNumber { get; }
		public List<Turn> Turns
		{
			get
			{
				return _turns;
			}
		}


		internal int GamePointsLeft
		{
			get
			{
				if (Turns.Count < _settings.Game.MaxKehrenProSpiel)
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

		internal int GamePointsRight
		{
			get
			{
				if (Turns.Count < _settings.Game.MaxKehrenProSpiel)
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

		internal int LeftPointsSum
		{
			get
			{
				return Turns.Sum(t => t.PointsLeft);
			}
		}

		internal int RightPointsSum
		{
			get
			{
				return Turns.Sum(t => t.PointsRight);
			}
		}

		internal string LeftPoints => string.Join("-", Turns.OrderBy(t => t.TurnNumber).Select(t => t.PointsLeft));

		internal string RightPoints => string.Join("-", Turns.OrderBy(t => t.TurnNumber).Select(t => t.PointsRight));

		public void DeleteLastTurn()
		{
			if (Turns.Count > 0)
			{
				Turns.RemoveAt(Turns.Count - 1);
			}
		}

	}
}
