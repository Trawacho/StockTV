using System;
using Windows.Storage;

namespace StockTV.Classes
{
    /// <summary>
    /// Singleton-Class for the Settings from the app
    /// </summary>
    public sealed class Settings
    {

        private static readonly Lazy<Settings> settings = new Lazy<Settings>(() => new Settings());

        private void Load()
        {
            ColorScheme = ColorScheme.Load();
            GameSettings = GameSettings.Load();

            var localSettings = ApplicationData.Current.LocalSettings;
            var coursenr = localSettings.Values[nameof(CourtNumber)] as string;
            this.CourtNumber = Int32.Parse(coursenr ?? "1");
        }

        /// <summary>
        /// Instanz of the settings
        /// </summary>
        public static Settings Instance { get { return settings.Value; } }

        /// <summary>
        /// Defualt private constructor
        /// </summary>
        private Settings()
        {
            Load();
        }

        /// <summary>
        /// Holds the current ColorScheme
        /// </summary>
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Holds the current GameSettings
        /// </summary>
        public GameSettings GameSettings { get; set; }

        public void CourtNumberChange(bool up = true)
        {
            if (up)
            {
                CourtNumber++;
            }
            else
            {
                CourtNumber--;
            }
        }

        private int courtNumber;
        /// <summary>
        /// Number of the Course
        /// </summary>
        public int CourtNumber
        {
            get { return courtNumber; }
            set
            {

                if (courtNumber == value ||
                          value < 1 ||
                          value > 99)
                    return;

                courtNumber = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(CourtNumber)] = value.ToString();
            }
        }

    }



}
