using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public class Match
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Match()
        {
            Games = new List<Game>();
            Games.Add(new Game(1));
        }

        #endregion


        #region private Fields

        /// <summary>
        /// internal List of Games
        /// </summary>
        private List<Game> Games;

        #endregion


        #region Public Properties

        /// <summary>
        /// Checks the Input if Settings Page can be displayed
        /// </summary>
        public bool CanSettingsShow
        {
            get
            {
                return Games[0].IsSettingsInput;
            }
        }

        /// <summary>
        /// the current Game
        /// </summary>
        public Game CurrentGame
        {
            get
            {
                if (Games.Count == 0)
                    Games.Add(new Game(1));

                return Games.Last();
            }
        }

        /// <summary>
        /// Sum of MatchPoints for left
        /// </summary>
        public int MatchPointsLeft
        {
            get
            {
                return Games.Sum(g => g.GamePointsLeft);
            }
        }

        /// <summary>
        /// Sum of MatchPoints for right
        /// </summary>
        public int MatchPointsRight
        {
            get
            {
                return Games.Sum(g => g.GamePointsRight);
            }
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Add a new Turn to Current Game. Add also a new Game if <see cref="Settings.GameSettings.MaxCountOfTurnsPerGame"/> are reached
        /// </summary>
        /// <param name="turn"></param>
        public void AddTurn(Turn turn)
        {
            if (Settings.Instance.GameSettings.Modus == GameSettings.Modis.Lkms  &&
                Settings.Instance.GameSettings.TurnsPerGame == CurrentGame.CountOfTurns)
            {
                Games.Add(new Game(Games.Count + 1));
            }

            turn.TurnNumber = CurrentGame.CountOfTurns + 1;


            if (Settings.Instance.GameSettings.TurnsPerGame > CurrentGame.CountOfTurns)
            {
                CurrentGame.Turns.Add(turn);
            }
        }

        /// <summary>
        /// Delete the last turn from Game while GameCount > 1
        /// </summary>
        public void DeleteLastTurn()
        {
            if (Games.Count > 1 && CurrentGame.CountOfTurns == 0)
            {
                Games.RemoveAt(Games.Count - 1);
            }
         
            CurrentGame.DeleteLastTurn();
        }

        /// <summary>
        /// reset of the current game
        /// </summary>
        public void Reset()
        {
            CurrentGame.Turns.Clear();
        }

        #endregion

    }
}
