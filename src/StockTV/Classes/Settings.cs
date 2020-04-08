using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace StockTV.Classes
{
    public sealed class Settings
    {

        private static readonly Lazy<Settings> settings = new Lazy<Settings>(() => new Settings());

        public static Settings Instance { get { return settings.Value; } }

        private Settings()
        {
            ColorScheme = ColorScheme.Load();
            GameSettings = GameSettings.Load();
        }

        public ColorScheme ColorScheme { get; set; }

        public GameSettings GameSettings { get; set; }

    }

   

}
