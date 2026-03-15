using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.Models
{
	public class Game(SettingsService settingsService, int gameNumber)
	{
		private readonly List<ITurn> _turns = [];
		public List<ITurn> Turns
		{
			get
			{
				return _turns;
			}
		}
		internal int GameNumber { get; } = gameNumber;

		private readonly SettingsService _settingsService = settingsService;
		private Settings Settings => _settingsService.CurrentSettings;

		internal int GamePointsLeft
		{
			get
			{
				if (Turns.Count < Settings.MaxKehrenProSpiel)
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
				if (Turns.Count < Settings.MaxKehrenProSpiel)
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

		internal string LeftPoints
		{
			get
			{
				string temp = string.Empty;
				foreach (var item in Turns.OrderBy(x => x.TurnNumber))
				{
					temp += String.IsNullOrEmpty(temp) ? "" : "-";
					temp += $"{item.PointsLeft}";
				}

				return temp;
			}
		}

		internal string RightPoints
		{
			get
			{
				string temp = string.Empty;
				foreach (var item in Turns.OrderBy(x => x.TurnNumber))
				{
					temp += String.IsNullOrEmpty(temp) ? "" : "-";
					temp += $"{item.PointsRight}";
				}

				return temp;
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
