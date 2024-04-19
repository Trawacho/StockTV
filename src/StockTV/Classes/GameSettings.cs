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
                    TurnsPerGame = _turnsPerGameTrainingDefault;
                    break;
                case GameModis.BestOf:
                    TurnsPerGame = _turnsPerGameTurnierDefault;
                    break;
                case GameModis.Turnier:
                    TurnsPerGame = _turnsPerGameTurnierDefault;
                    break;
                case GameModis.Ziel:
                    TurnsPerGame = _turnsPerZielSectionDefault;
                    break;
                default:
                    break;
            }

            PointsPerTurn = _pointsPerTurnDefault;

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

        private byte _turnsPerGame;
        /// <summary>
        /// Max count of Turns per Game
        /// </summary>
        public byte TurnsPerGame
        {
            get => _turnsPerGame;
            internal set
            {
                if (_turnsPerGame == value ||
                           value < _turnsPerGameMin ||
                           value > _turnsPerGameMax)
                    return;

                SetSaveProperty(ref _turnsPerGame, value, nameof(TurnsPerGame));
            }
        }

        /// <summary>
        /// Min Value for <see cref="TurnsPerGame"/>
        /// </summary>
        const byte _turnsPerGameMin = 4;

        /// <summary>
        /// Max Value for <see cref="TurnsPerGame"/>
        /// </summary>
        const byte _turnsPerGameMax = 99;

        /// <summary>
        /// Default for <see cref="TurnsPerGame"/> in <see cref="GameModis.Training"/>
        /// </summary>
        const byte _turnsPerGameTrainingDefault = 30;

        /// <summary>
        /// Default for <see cref="TurnsPerGame"/> in <see cref="GameModis.Turnier"/> or <see cref="GameModis.BestOf"/>
        /// </summary>
        const byte _turnsPerGameTurnierDefault = 6;

        /// <summary>
        /// Default count of attempts per section in Zielbewerb
        /// </summary>
        const byte _turnsPerZielSectionDefault = 6;
        #endregion

        #region Punkte Pro Kehre

        private byte _pointsPerTurn;
        /// <summary>
        /// Max Points per single Turn
        /// </summary>
        public byte PointsPerTurn
        {
            get => _pointsPerTurn;
            set
            {
                if (_pointsPerTurn == value || value < PointsPerTurnMin || value > PointsPerTurnMax)
                    return;

                SetSaveProperty(ref _pointsPerTurn, value, nameof(PointsPerTurn));
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
        const byte _pointsPerTurnDefault = 30;

        #endregion


        private GameModis _modis;
        /// <summary>
        /// Modus for the Game
        /// </summary>
        public GameModis GameModus
        {
            get => _modis;
            private set => SetSaveProperty(ref _modis, value, nameof(GameModus));
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
