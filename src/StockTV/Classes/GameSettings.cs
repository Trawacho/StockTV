using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace StockTV.Classes
{
    public class GameSettings
    {

        #region Public Enumeration 

        /// <summary>
        /// Enumeration for different Modis
        /// </summary>
        public enum Modis
        {
            Normal,
            Lkms
        }

        #endregion


        #region Contructor

        /// <summary>
        /// Default - Constructor
        /// </summary>
        /// <param name="modus"></param>
        public GameSettings(Modis modus = Modis.Normal)
        {
            SetModus(modus);
        }

        #endregion


        #region Public Functions

        /// <summary>
        /// Set the modus
        /// </summary>
        /// <param name="modus"></param>
        private void SetModus(Modis modus)
        {
            switch (modus)
            {
                case Modis.Normal:
                    Modus = modus;
                    TurnsPerGame = 30;
                    break;
                case Modis.Lkms:
                    Modus = Modis.Lkms;
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
            if (Modus == Modis.Normal)
            {
                SetModus(Modis.Lkms);
            }
            else
            {
                SetModus(Modis.Normal);
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
            var gamemodus = localSettings.Values["GameSettingsModus"] as string;
            var turnspergame = localSettings.Values["TurnsPerGame"] as string;
            var pointsperturn = localSettings.Values["PointsPerTurn"] as string;

            var gamesettings = new GameSettings(gamemodus.ToEnum<GameSettings.Modis>());
            gamesettings.TurnsPerGame = Int32.Parse(turnspergame ?? "30");
            gamesettings.PointsPerTurn = Int32.Parse(pointsperturn ?? "30");

            return gamesettings;
        }

        #endregion


        #region Properties

        private int turnsPerGame;
        /// <summary>
        /// Max count of Turns per Game
        /// </summary>
        public int TurnsPerGame
        {
            get
            {
                return turnsPerGame;
            }
            private set
            {
                if (turnsPerGame == value || value < 4 || value > 99)
                    return;

                turnsPerGame = value;
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["TurnsPerGame"] = value.ToString();
            }
        }

        private int pointsPerTurn;
        /// <summary>
        /// Max Points per single Turn
        /// </summary>
        public int PointsPerTurn
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
                localSettings.Values["PointsPerTurn"] = value.ToString();
            }
        }

        private Modis modis;
        /// <summary>
        /// Modus for the Game
        /// </summary>
        public Modis Modus
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
                localSettings.Values["GameSettingsModus"] = value.ToString();
            }
        }

        #endregion

    }
}
