using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

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
            games = new List<Game>();
            games.Add(new Game(1));
        }

        #endregion


        #region private Fields

        /// <summary>
        /// internal List of Games
        /// </summary>
        private readonly List<Game> games;

        #endregion


        #region Public Properties

        /// <summary>
        /// Checks the Input if Settings Page can be displayed
        /// </summary>
        public bool CanSettingsShow
        {
            get
            {
                return games[0].IsSettingsInput;
            }
        }

        /// <summary>
        /// the current Game
        /// </summary>
        public Game CurrentGame
        {
            get
            {
                if (games.Count == 0)
                    games.Add(new Game(1));

                return games.Last();
            }
        }

        /// <summary>
        /// Sum of MatchPoints for left
        /// </summary>
        public int MatchPointsLeft
        {
            get
            {
                return games.Sum(g => g.GamePointsLeft);
            }
        }

        /// <summary>
        /// Sum of MatchPoints for right
        /// </summary>
        public int MatchPointsRight
        {
            get
            {
                return games.Sum(g => g.GamePointsRight);
            }
        }

        /// <summary>
        /// All Games from Match
        /// </summary>
        public IEnumerable<Game> Games
        {
            get
            {
                return games;
            }
        }
        #endregion


        #region Public Functions

        /// <summary>
        /// Add a new Turn to Current Game until <see cref="GameSettings.TurnsPerGame"/> are reached
        /// </summary>
        /// <param name="turn"></param>
        public void AddTurn(Turn turn)
        {
            turn.TurnNumber = Convert.ToByte(CurrentGame.Turns.Count + 1);

            if (Settings.Instance.GameSettings.TurnsPerGame > CurrentGame.Turns.Count)
            {
                CurrentGame.Turns.Add(turn);
            }
        }

        /// <summary>
        /// Delete the last turn from Game while GameCount > 1
        /// </summary>
        public void DeleteLastTurn()
        {
            if (games.Count > 1 && CurrentGame.Turns.Count == 0)
            {
                games.RemoveAt(games.Count - 1);
            }
            else
            {
                CurrentGame.DeleteLastTurn();
            }
        }

        /// <summary>
        /// reset of the current game or starts the next Match (BestOf, Turnier) when max of <see cref="GameSettings.TurnsPerGame"/> are reached
        /// </summary>
        public void Reset(bool force = false)
        {
            if (force)
            {
                this.games.Clear();
                this.games.Add(new Game(1));
                return;
            }


            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier ||
                Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.BestOf)
            {
                if (CurrentGame.Turns.Count == Settings.Instance.GameSettings.TurnsPerGame)
                {
                    games.Add(new Game(Convert.ToByte(games.Count + 1)));
                }
                else
                {
                    CurrentGame.Turns.Clear();
                }
            }
            else
            {
                CurrentGame.Turns.Clear();
            }
        }


        public byte[] Serialize(bool compressed = false, byte courtNumber = 0)
        {
            var values = new List<byte>();

            //First byte is CourtNumber
            if (courtNumber == 0)
                courtNumber = Settings.Instance.CourtNumber;
            
            values.Add(courtNumber);
            
            //Second byte is Number of Turns per Game
            values.Add(Convert.ToByte(Settings.Instance.GameSettings.TurnsPerGame));

            //Add for each turn in each Game the value of the left and then the value of the right
            foreach (var g in Games)
            {
                foreach (var t in g.Turns)
                {
                    values.Add(t.PointsLeft);
                    values.Add(t.PointsRight);
                }
            }

            //Convert the list of values to an array
            var data = values.ToArray();

            if (!compressed)
                return data;

            var output = new MemoryStream();
            using (var datastream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                datastream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
        #endregion

    }
}
