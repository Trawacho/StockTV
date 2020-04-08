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

        public bool CanSettingsShow
        {
            get
            {
                return Games[0].IsSettingsInput;
            }
        }

        public Game CurrentGame
        {
            get
            {
                if (Games.Count == 0)
                    Games.Add(new Game(1));

                return Games.Last();
            }
        }

        public int MatchPointsLeft
        {
            get
            {
                return Games.Sum(g => g.GamePointsLeft);
            }
        }

        public int MatchPointsRight
        {
            get
            {
                return Games.Sum(g => g.GamePointsRight);
            }
        }

        #endregion


        #region Public Functions

        public void AddTurn(Turn turn)
        {
            if (Settings.Instance.GameSettings.Modus == GameSettings.Modis.Lkms  &&
                Settings.Instance.GameSettings.MaxCountOfTurnsPerGame == CurrentGame.CountOfTurns)
            {
                Games.Add(new Game(Games.Count + 1));
            }

            turn.TurnNumber = CurrentGame.CountOfTurns + 1;


            if (Settings.Instance.GameSettings.MaxCountOfTurnsPerGame > CurrentGame.CountOfTurns)
            {
                CurrentGame.Turns.Add(turn);
            }
        }

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
