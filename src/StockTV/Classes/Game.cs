using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public class Game
    {
        public Game(int gameNumber)
        {
            Turns = new List<Turn>();
            _GameNumber = gameNumber;
        }

        private readonly int _GameNumber;
        public int GameNumber
        {
            get
            {
                return _GameNumber;
            }
        }

        public List<Turn> Turns
        {
            get; set;
        }


        public int GamePointsLeft
        {
            get
            {
                if (CountOfTurns < Settings.Instance.GameSettings.MaxCountOfTurnsPerGame)
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
                if (CountOfTurns < Settings.Instance.GameSettings.MaxCountOfTurnsPerGame)
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


        public int CountOfTurns
        {
            get
            {
                return Turns.Count;
            }
        }

        public bool IsSettingsInput
        {
            get
            {
                if (Turns.Count != 3)
                    return false;

                return (Turns[0]?.PointsLeft == 4 &&
                        Turns[1]?.PointsLeft == 7 &&
                        Turns[2]?.PointsLeft == 11);
            }
        }

        internal void DeleteLastTurn()
        {
            if (Turns.Count > 0)
            {
                Turns.RemoveAt(Turns.Count - 1);
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
    }
}
