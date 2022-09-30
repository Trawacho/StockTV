using System.Collections.Generic;
using System.Linq;

namespace StockTV.Classes
{
    public interface IGame
    {
        byte GameNumber { get; }
        List<ITurn> Turns { get; set; }
    }

    public class Game : IGame
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameNumber">Number of the new Game</param>
        public Game(byte gameNumber)
        {
            Turns = new List<ITurn>();
            GameNumber = gameNumber;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Number of the Game
        /// </summary>
        public byte GameNumber { get; }

        /// <summary>
        /// List of all Turns for this Game
        /// </summary>
        public List<ITurn> Turns
        {
            get; set;
        }

        /// <summary>
        /// After the MaxCountOfTurnsPerGame are reached, it returns the Points for Left
        /// </summary>
        public int GamePointsLeft
        {
            get
            {
                if (Turns.Count < Settings.Instance.GameSettings.TurnsPerGame)
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

        /// <summary>
        /// After the MaxCountOfTurnsPerGame are reached, it returns the Points for Right
        /// </summary>
        public int GamePointsRight
        {
            get
            {
                if (Turns.Count < Settings.Instance.GameSettings.TurnsPerGame)
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

        /// <summary>
        /// Sum of the LEFT Points of all Turns in this Game
        /// </summary>
        private int LeftPointsSum
        {
            get
            {
                return Turns.Sum(t => t.PointsLeft);
            }
        }

        /// <summary>
        /// Sum of the RIGHT Points of all Turns in this Game
        /// </summary>
        private int RightPointsSum
        {
            get
            {
                return Turns.Sum(t => t.PointsRight);
            }
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Delete the Last Turn in this Game
        /// </summary>
        public void DeleteLastTurn()
        {
            if (Turns.Count > 0)
            {
                Turns.RemoveAt(Turns.Count - 1);
            }
        }

        #endregion

    }
}
