using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace StockTV.Classes
{
    /// <summary>
    /// Singleton-Class for the Settings from the app
    /// </summary>
    public sealed class Settings
    {

        private static readonly Lazy<Settings> settings = new Lazy<Settings>(() => new Settings());

        /// <summary>
        /// Instanz of the settings
        /// </summary>
        public static Settings Instance { get { return settings.Value; } }

        /// <summary>
        /// Defualt private constructor
        /// </summary>
        private Settings()
        {
            ColorScheme = ColorScheme.Load();
            GameSettings = GameSettings.Load();
        }

        /// <summary>
        /// Holds the current ColorScheme
        /// </summary>
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Holds the current GameSettings
        /// </summary>
        public GameSettings GameSettings { get; set; }

    }

   

}
