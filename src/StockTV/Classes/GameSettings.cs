using System;
using System.Collections.Generic;
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
        internal void SetModus(GameModis modus)
        {
            GameModus = modus;

            switch (modus)
            {
                case GameModis.Training:
                    TurnsPerGame = TurnsPerGameTrainingDefault;
                    break;
                case GameModis.BestOf:
                    TurnsPerGame = TurnsPerGameTurnierDefault;
                    break;
                case GameModis.Turnier:
                    TurnsPerGame = TurnsPerGameTurnierDefault;
                    break;
                case GameModis.Ziel:
                    TurnsPerGame = TurnsPerZielSectionDefault;
                    break;
                default:
                    break;
            }

            PointsPerTurn = PointsPerTurnDefault;

        }

        internal void SetModus(byte value)
        {
            var e = (GameSettings.GameModis)Enum.Parse(typeof(GameSettings.GameModis), value.ToString());
            SetModus(e);
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

            var gamesettings = new GameSettings(gamemodus.ToEnum<GameSettings.GameModis>())
            {
                TurnsPerGame = byte.Parse(turnspergame ?? "30"),
                PointsPerTurn = byte.Parse(pointsperturn ?? "30")
            };

            return gamesettings;
        }

        #endregion


        #region Properties

        #region Kehren Pro Spiel

        private byte turnsPerGame;
        /// <summary>
        /// Max count of Turns per Game
        /// </summary>
        public byte TurnsPerGame
        {
            get => turnsPerGame;
            internal set
            {
                if (turnsPerGame == value ||
                           value < TurnsPerGameMin ||
                           value > TurnsPerGameMax)
                    return;

                SetSaveProperty(ref turnsPerGame, value, nameof(TurnsPerGame));
            }
        }

        /// <summary>
        /// Min Value for <see cref="TurnsPerGame"/>
        /// </summary>
        const byte TurnsPerGameMin = 4;

        /// <summary>
        /// Max Value for <see cref="TurnsPerGame"/>
        /// </summary>
        const byte TurnsPerGameMax = 99;

        /// <summary>
        /// Default for <see cref="TurnsPerGame"/> in <see cref="GameModis.Training"/>
        /// </summary>
        const byte TurnsPerGameTrainingDefault = 30;

        /// <summary>
        /// Default for <see cref="TurnsPerGame"/> in <see cref="GameModis.Turnier"/> or <see cref="GameModis.BestOf"/>
        /// </summary>
        const byte TurnsPerGameTurnierDefault = 6;

        /// <summary>
        /// Default count of attempts per section in Zielbewerb
        /// </summary>
        const byte TurnsPerZielSectionDefault = 6;
        #endregion

        #region Punkte Pro Kehre

        private byte pointsPerTurn;
        /// <summary>
        /// Max Points per single Turn
        /// </summary>
        public byte PointsPerTurn
        {
            get => pointsPerTurn;
            set
            {
                if (pointsPerTurn == value || value < PointsPerTurnMin || value > PointsPerTurnMax)
                    return;

                SetSaveProperty(ref pointsPerTurn, value, nameof(PointsPerTurn));
            }
        }

        /// <summary>
        /// Max Value for <see cref="PointsPerTurn"/>
        /// </summary>
        public byte PointsPerTurnMax = 99;

        /// <summary>
        /// Min Value for <see cref="PointsPerTurn"/>
        /// </summary>
        public byte PointsPerTurnMin = 4;

        /// <summary>
        /// Default Value for <see cref="PointsPerTurn"/>
        /// </summary>
        const byte PointsPerTurnDefault = 30;

        #endregion


        private GameModis modis;
        /// <summary>
        /// Modus for the Game
        /// </summary>
        public GameModis GameModus
        {
            get => modis;
            private set => SetSaveProperty(ref modis, value, nameof(GameModus));
        }

        #endregion

        private bool SetSaveProperty<T>(ref T storage, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[propertyName] = value.ToString();

            return true;
        }

    }
}
