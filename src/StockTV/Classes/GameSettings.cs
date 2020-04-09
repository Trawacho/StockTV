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
        public void SetModus(Modis modus)
        {
            switch (modus)
            {
                case Modis.Normal:
                    Modus = modus;
                    MaxCountOfTurnsPerGame = 30;
                    break;
                case Modis.Lkms:
                    Modus = Modis.Lkms;
                    MaxCountOfTurnsPerGame = 6;
                    break;
                default:
                    break;
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

            return new GameSettings(gamemodus.ToEnum<GameSettings.Modis>());
        }

        #endregion


        #region Properties

        /// <summary>
        /// Max count of Turns per Game
        /// </summary>
        public int MaxCountOfTurnsPerGame { get; set; }

        /// <summary>
        /// Max Points per single Turn
        /// </summary>
        public int MaxPointsPerTurn { get; set; } = 30;

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
