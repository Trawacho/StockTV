using System;
using Windows.Storage;

namespace StockTV.Classes
{
    public class GameSettings
    {

        #region Public Enumeration 

        /// <summary>
        /// Enumeration for different Modis
        /// </summary>
        public enum GameModis
        {
            Training = 0,
            BestOf = 1,
            Turnier = 2,
            Ziel = 100
        }

        #endregion


        #region Contructor

        /// <summary>
        /// Default - Constructor
        /// </summary>
        /// <param name="modus"></param>
        public GameSettings(GameModis modus = GameModis.Training)
        {
            SetModus(modus);
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Set the modus
        /// </summary>
        /// <param name="modus"></param>
        private void SetModus(GameModis modus)
        {
            GameModus = modus;

            switch (modus)
            {
                case GameModis.Training:
                    TurnsPerGame = 30;
                    break;
                case GameModis.BestOf:
                    TurnsPerGame = 6;
                    break;
                case GameModis.Turnier:
                    TurnsPerGame = 6;
                    break;
                default:
                    break;
            }

            PointsPerTurn = 30;

        }

        /// <summary>
        /// change to Next or Previous modus
        /// </summary>
        /// <param name="next"></param>
        public void ModusChange(bool next = true)
        {
            if (next)
            {
                SetModus(GameModus.Next());
            }
            else
            {
                SetModus(GameModus.Previous());
            }
        }

        /// <summary>
        /// Increase or decrease the value
        /// </summary>
        /// <param name="up"></param>
        public void TurnsPerGameChange(bool up = true)
        {
            if (up)
            {
                TurnsPerGame++;
            }
            else
            {
                TurnsPerGame--;
            }
        }

        /// <summary>
        /// Increase or decrease the value
        /// </summary>
        /// <param name="up"></param>
        public void PointsPerTurnChange(bool up = true)
        {
            if (up)
            {
                PointsPerTurn++;
            }
            else
            {
                PointsPerTurn--;
            }
        }

        #endregion


        #region Static Functions


        /// <summary>
        /// Returns the localy saved GameSettings 
        /// </summary>
        /// <returns></returns>
        public static GameSettings Load()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var gamemodus = localSettings.Values[nameof(GameModus)] as string;
            var turnspergame = localSettings.Values[nameof(TurnsPerGame)] as string;
            var pointsperturn = localSettings.Values[nameof(PointsPerTurn)] as string;

            var gamesettings = new GameSettings(gamemodus.ToEnum<GameSettings.GameModis>());
            //if(gamesettings.GameModus == GameModis.Ziel)
            //{
            //    gamesettings.GameModus = GameModis.Training;
            //}
            gamesettings.TurnsPerGame = byte.Parse(turnspergame ?? "30");
            gamesettings.PointsPerTurn = byte.Parse(pointsperturn ?? "30");

            return gamesettings;
        }

        #endregion


        #region Properties

        private byte turnsPerGame;
        /// <summary>
        /// Max count of Turns per Game
        /// </summary>
        public byte TurnsPerGame
        {
            get
            {
                return turnsPerGame;
            }
            private set
            {
                if (turnsPerGame == value ||
                           value < 4 ||
                           value > 99)
                    return;

                turnsPerGame = value;
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(TurnsPerGame)] = value.ToString();
            }
        }

        private byte pointsPerTurn;
        /// <summary>
        /// Max Points per single Turn
        /// </summary>
        public byte PointsPerTurn
        {
            get
            {
                return pointsPerTurn;
            }
            set
            {
                if (pointsPerTurn == value || value < 15 || value > 99)
                    return;

                pointsPerTurn = value;
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(PointsPerTurn)] = value.ToString();
            }
        }

        private GameModis modis;
        /// <summary>
        /// Modus for the Game
        /// </summary>
        public GameModis GameModus
        {
            get
            {
                return modis;
            }
            private set
            {
                if (modis == value)
                    return;

                modis = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(GameModus)] = value.ToString();
            }
        }

        #endregion

    }
}
