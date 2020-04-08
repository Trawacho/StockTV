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
        public enum Modis
        {
            Normal,
            Lkms
        }


        public GameSettings(Modis modus = Modis.Normal)
        {
            SetModus(modus);
        }

        private Modis modis;
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

        public static GameSettings Load()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var gamemodus = localSettings.Values["GameSettingsModus"] as string;

            return new GameSettings(gamemodus.ToEnum<GameSettings.Modis>());
        }


        public int MaxCountOfTurnsPerGame { get; set; }

        public int MaxPointsPerTurn { get; set; } = 30;


    }
}
