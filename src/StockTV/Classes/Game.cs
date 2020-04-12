using System.Collections.Generic;
using System.Linq;

namespace StockTV.Classes
{
    public class Game
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameNumber">Number of the new Game</param>
        public Game(int gameNumber)
        {
            Turns = new List<Turn>();
            GameNumber = gameNumber;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Number of the Game
        /// </summary>
        public int GameNumber { get; }

        /// <summary>
        /// List of all Turns for this Game
        /// </summary>
        public List<Turn> Turns
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
                if (CountOfTurns < Settings.Instance.GameSettings.TurnsPerGame)
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
                if (CountOfTurns < Settings.Instance.GameSettings.TurnsPerGame)
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
        /// the Count of Turns in this Game
        /// </summary>
        public int CountOfTurns
        {
            get
            {
                return Turns.Count;
            }
        }

        /// <summary>
        /// True if the input is correct to Display the settings page
        /// </summary>
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
