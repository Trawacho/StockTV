using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace StockTV.Classes
{
    /// <summary>
    /// Match-Class as Base for Training, Turnier and BestOf with n-Games<br></br>
    /// Functions to add or delete Turns, Save and Load Turns to local Settings, ...
    /// </summary>
    public class Match
    {
        #region EventHandler for TurnsChanged

        public event EventHandler TurnsChanged;
        protected void RaiseTurnsChanged()
        {
            var handler = TurnsChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Match()
        {
            _games = new List<Game>
            {
                new Game(1)
            };

            Begegnungen = new List<Begegnung>();

            LoadTurnsFromLocalSettings();
        }

        #endregion


        #region private Fields

        /// <summary>
        /// internal List of Games
        /// </summary>
        private readonly List<Game> _games;

        #endregion


        #region Public Properties

        /// <summary>
        /// the current Game
        /// </summary>
        public Game CurrentGame
        {
            get
            {
                if (_games.Count == 0)
                    _games.Add(new Game(1));

                return _games.Last();
            }
        }

        /// <summary>
        /// Sum of MatchPoints for left
        /// </summary>
        public int MatchPointsLeft
        {
            get
            {
                return _games.Sum(g => g.GamePointsLeft);
            }
        }

        /// <summary>
        /// Sum of MatchPoints for right
        /// </summary>
        public int MatchPointsRight
        {
            get
            {
                return _games.Sum(g => g.GamePointsRight);
            }
        }

        /// <summary>
        /// All Games from Match
        /// </summary>
        public IEnumerable<Game> Games
        {
            get
            {
                return _games;
            }
        }

        /// <summary>
        /// List of Begegnungen if sent per NetMQServer
        /// </summary>
        public List<Begegnung> Begegnungen { get; set; }

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
                RaiseTurnsChanged();
            }
        }

        /// <summary>
        /// Delete the last turn from Game while GameCount > 1
        /// </summary>
        public void DeleteLastTurn()
        {
            if (_games.Count > 1 && CurrentGame.Turns.Count == 0)
            {
                _games.RemoveAt(_games.Count - 1);
            }
            else
            {
                CurrentGame.DeleteLastTurn();
            }

            SaveTurnsToLocalSettings();
            RaiseTurnsChanged();
        }

        /// <summary>
        /// reset of the current game or starts the next Match (BestOf, Turnier) when max of <see cref="GameSettings.TurnsPerGame"/> are reached
        /// </summary>
        public void Reset(bool force = false)
        {
            if (force)
            {
                this.Begegnungen.Clear();
                this._games.Clear();
                this._games.Add(new Game(1));
                SaveTurnsToLocalSettings();
                RaiseTurnsChanged();
                return;
            }


            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier ||
                Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.BestOf)
            {
                if (CurrentGame.Turns.Count == Settings.Instance.GameSettings.TurnsPerGame)
                {
                    _games.Add(new Game(Convert.ToByte(_games.Count + 1)));
                }
            }
            else
            {
                CurrentGame.Turns.Clear();
            }

            SaveTurnsToLocalSettings();
            RaiseTurnsChanged();
        }

        /// <summary>
        /// Return a byte[] with HeaderInformation in first 10 bytes following with Json-serialized Games (UTF8)
        /// </summary>
        /// <returns></returns>
        public byte[] SerializeJson()
        {
            var values = new List<byte>();

            values.AddRange(Settings.Instance.GetSettings());
            string json = JsonSerializer.Serialize(Games);
            values.AddRange(Encoding.UTF8.GetBytes(json));
            return values.ToArray();
        }

        /// <summary>
        /// Returns a byte[] with HeaderInformations and Match Results
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            /* 
             *  the byte array starts with ten bytes, containing the settings, starting with courtnumber, groupnumber, modus, direction,.....
             *  starting with the 11th byte the values from the games are following, always two bytes per game.
             *  the first byte is the sum of the left team, the second is the sum of the right team followed by the next game with also 2 bytes length
             *  
             *  e.g.
             *  1  2  3  4  5  6  7  8  9  10 11 12 13 14 15 16
             *  01 02 09 03 15 05 03 00 00 00 09 03 03 15 05 03 
             *  Court 1
             *     Group 2
             *        Modus 09
             *           Direction 03
             *  ...
             *                                 Game1: 9:3
             *                                      Game2: 3:15
             *                                           Game3: 5:3
             *  
             */


            List<byte> values = new List<byte>();

            values.AddRange(Settings.Instance.GetSettings());

            //Add for each Game the sum of the turn-value for left and right
            foreach (Game g in Games)
            {
                values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsLeft)));
                values.Add(Convert.ToByte(g.Turns.Sum(t => t.PointsRight)));
            }

            //Convert the list of values to an array
            return values.ToArray();
        }

        /// <summary>
        /// Save all turns as long as <see cref="GameSettings.GameModus"/> isn´t <see cref="GameSettings.GameModis.Training"/>
        /// </summary>
        internal void SaveTurnsToLocalSettings()
        {
            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Training)
                return;

            var turns = new List<ITurn>();

            foreach (var g in Games)
            {
                foreach (var t in g.Turns)
                {
                    turns.Add(t);
                }
            }
            Settings.Instance.SaveTurns(turns);
        }

        /// <summary>
        /// if GameModus is not Training, the Turns are loaded from the local settings
        /// </summary>
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
