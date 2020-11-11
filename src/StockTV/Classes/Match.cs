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

            LoadTurnsFromLocalSettings();
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
                SaveTurnsToLocalSettings();
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

            SaveTurnsToLocalSettings();
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
                SaveTurnsToLocalSettings();
                return;
            }


            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier ||
                Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.BestOf)
            {
                if (CurrentGame.Turns.Count == Settings.Instance.GameSettings.TurnsPerGame)
                {
                    games.Add(new Game(Convert.ToByte(games.Count + 1)));
                }
            }
            else
            {
                CurrentGame.Turns.Clear();
            }

            SaveTurnsToLocalSettings();
        }


        public byte[] Serialize(bool compressed = false, byte courtNumber = 0)
        {
            /* 
             *  the byte[] should have as first byte the courtnumber, follow by the values of the Games
             *  for each Game the first byte is the sum of values from the turns of the left
             *  the next byte is the sum of values form the turns of the right
             *  followed by the next game
             *  
             *  e.g.
             *  01 09 03 15 05 03 00
             *  Court 1
             *  Game1: 9:3
             *  Game2: 15:5
             *  Game3: 3:0
             *  
             */


            var values = new List<byte>();

            //First byte is CourtNumber
            if (courtNumber == 0)
                courtNumber = Settings.Instance.CourtNumber;

            values.Add(courtNumber);

            //Add for each Game the sum of the turn-value for left and right
            foreach (var g in Games)
            {
                values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsLeft)));
                values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsRight)));
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

        /// <summary>
        /// Save all turns as long as <see cref="GameSettings.GameModus"/> isn´t <see cref="GameSettings.GameModis.Training"/>
        /// </summary>
        internal void SaveTurnsToLocalSettings()
        {
            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Training)
                return;

            var turns = new List<Turn>();

            foreach (var g in Games)
            {
                foreach (var t in g.Turns)
                {
                    turns.Add(t);
                }
            }
            Settings.Instance.SaveTurns(turns);
        }

        private void LoadTurnsFromLocalSettings()
        {
            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Training)
                return;

            foreach (var t in Settings.Instance.LoadTurns())
            {
                this.AddTurn(t);
                this.Reset();
            }

        }
        #endregion

    }
}
