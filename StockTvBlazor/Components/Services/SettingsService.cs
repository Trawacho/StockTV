using StockTvBlazor.Components.Extensions;
using StockTvBlazor.Components.Models;
using System.Text.Json;
using static StockTvBlazor.Components.Models.Settings;

namespace StockTvBlazor.Components.Services
{
    public class SettingsService
    {
        private Settings _settings = null!;
        private readonly string _settingsFileName = "stocktv.config.json";
        private string _settingsFilePath
        {
            get
            {
                string appDataPath = AppContext.BaseDirectory;
                string settingsFolderPath = Path.Combine(appDataPath, "_config");
                return Path.Combine(settingsFolderPath, _settingsFileName);
            }
        }

        private readonly ILogger _logger;

        public event Action? OnSettingsChanged;
        public event Action<string>? OnNavigationRequested;

        private readonly FileLoggerProvider _fileLoggerProvider;

        public SettingsService(
            ILogger<SettingsService> logger,
            FileLoggerProvider fileLoggerProvider)
        {
            _logger = logger;
            _fileLoggerProvider = fileLoggerProvider;

            //_logger.LogDebug("SettingsService created - Instanz: " + GetHashCode());
        }

        public async Task InitializeAsync()
        {
            _settings = await LoadSettingsAsync();

            // FileLoggerProvider sofort korrekt setzen
            _fileLoggerProvider.Enabled = _settings.FileLoggingEnabled;

            _logger.LogInformation("FileLogging initial: {State}",
                _settings.FileLoggingEnabled ? "aktiviert" : "deaktiviert");
        }


        public Settings CurrentSettings
        {
            get
            {
                if (_settings == null)
                    throw new InvalidOperationException("SettingsService wurde nicht initialisiert.");

                return _settings;
            }
            private set
            {
                _settings = value;
                OnSettingsChanged?.Invoke();
            }
        }

        public void ToggleFileLogging()
        {
            var settings = CurrentSettings;

            settings.FileLoggingEnabled = !settings.FileLoggingEnabled;
            _fileLoggerProvider.Enabled = settings.FileLoggingEnabled;

            _logger.LogInformation("FileLogging wurde {State}",
                settings.FileLoggingEnabled ? "aktiviert" : "deaktiviert");

            OnSettingsChanged?.Invoke();
        }

        public void ChangeModus(bool forward)
        {
            var newModus = forward
                   ? CurrentSettings.Modus.Next()
                   : CurrentSettings.Modus.Previous();

            if (newModus == Settings.MODUS.TRAINING)
            {
                CurrentSettings.MaxKehrenProSpiel = 30;
                CurrentSettings.MaxPunkteProKehre = 15;
            }
            else
            {
                CurrentSettings.MaxKehrenProSpiel = 6;
                CurrentSettings.MaxPunkteProKehre = 9;
            }

            CurrentSettings.Modus = newModus;
            OnNavigationRequested?.Invoke(GetModusUrl(newModus));
            OnSettingsChanged?.Invoke();
        }

        public void ChangeTheme(bool forward)
        {
            CurrentSettings.Theme = forward
                ? CurrentSettings.Theme.Next()
                : CurrentSettings.Theme.Previous();
            OnSettingsChanged?.Invoke();
        }

        public void ChangeRichtung(bool forward)
        {
            CurrentSettings.Richtung = forward
                ? CurrentSettings.Richtung.Next()
                : CurrentSettings.Richtung.Previous();
            OnSettingsChanged?.Invoke();
        }

        public void ChangeBlockLocalChanges()
        {
            CurrentSettings.BlockLocalChanges = !CurrentSettings.BlockLocalChanges;
            OnSettingsChanged?.Invoke();
        }

        public void ChangeSpielgruppe(bool forward)
        {
            if (forward)
            {
                if (CurrentSettings.Spielgruppe < 10)
                {
                    CurrentSettings.Spielgruppe++;
                }
            }
            else
            {
                if (CurrentSettings.Spielgruppe > 0)
                {
                    CurrentSettings.Spielgruppe--;
                }
            }

            OnSettingsChanged?.Invoke();
        }

        public void ChangeBahnNummer(bool forward)
        {
            if (forward)
            {
                if (CurrentSettings.BahnNummer < 30) // Assuming 99 is the maximum value
                    CurrentSettings.BahnNummer++;
            }
            else
            {
                if (CurrentSettings.BahnNummer > 1) // Assuming 1 is the minimum value
                    CurrentSettings.BahnNummer--;
            }
            OnSettingsChanged?.Invoke();
        }

        public void ChangeMaxMaxKehrenProSpiel(bool forward)
        {
            if (forward)
            {
                if (CurrentSettings.MaxKehrenProSpiel < 30) // Assuming 30 is the maximum value
                    CurrentSettings.MaxKehrenProSpiel++;
            }
            else
            {
                if (CurrentSettings.MaxKehrenProSpiel > 4) // Assuming 4 is the minimum value
                    CurrentSettings.MaxKehrenProSpiel--;
            }
            OnSettingsChanged?.Invoke();
        }

        public void ChangeMaxPunkteProKehre(bool forward)
        {
            if (forward)
            {
                if (CurrentSettings.MaxPunkteProKehre < 15) // Assuming 15 is the maximum value
                    CurrentSettings.MaxPunkteProKehre++;
            }
            else
            {
                if (CurrentSettings.MaxPunkteProKehre > 0) // Assuming 0 is the minimum value
                    CurrentSettings.MaxPunkteProKehre--;
            }
            OnSettingsChanged?.Invoke();
        }

        public void ChangeNetworking()
        {
            //todo: implement network connection logic when enabling networking
            CurrentSettings.Networking = !CurrentSettings.Networking;
            OnSettingsChanged?.Invoke();
        }

        private static string GetModusUrl(MODUS modus) => modus switch
        {
            MODUS.TRAINING => "/training",
            MODUS.BESTOF => "/bestof",
            MODUS.TURNIER => "/turnier",
            MODUS.ZIEL => "/ziel",
            _ => "/training"
        };




        #region Load and Save

        private async Task<Settings> LoadSettingsAsync()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json) ?? new();
                    return settings;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Fehler beim Laden der Config: {ex.Message}");
                    return new Settings();
                }
            }
            else
            {
                _logger.LogWarning("Keine Config-Datei gefunden, Standardwerte werden verwendet.");
                return new Settings();
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                // 1. Sicherstellen, dass der Ordner existiert
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 2. Asynchron serialisieren und schreiben
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);

                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Hier ggf. Logger injizieren und Fehler loggen
                _logger.LogError($"Fehler beim Speichern der Config: {ex.Message}");
            }
        }

        internal async Task SaveTurnsAsync(List<Turn> turns)
        {
            if (CurrentSettings.Modus == Settings.MODUS.TRAINING)
            {
                // Im Trainingsmodus werden die Kehren nicht gespeichert, da sie nur temporär sind
                return;
            }
            CurrentSettings.Kehren.Clear();
            CurrentSettings.Kehren.AddRange(turns);
            await SaveSettingsAsync();
        }

        #endregion

        #region Get and Set Settings as Byte Array for Networking
        public byte[] GetSettings()
        {
            var data = new List<byte>
            {
                Convert.ToByte(CurrentSettings.BahnNummer),			//Bahnnummer
                Convert.ToByte(CurrentSettings.Spielgruppe),		//SpielGruppe    
                Convert.ToByte((int)CurrentSettings.Modus),			//Modus
                Convert.ToByte(CurrentSettings.Richtung),			//Spielrichtung
                Convert.ToByte(CurrentSettings.Theme),				//FarbModus (hell,dunkel)
                Convert.ToByte(CurrentSettings.MaxPunkteProKehre),	//Anzahl max. Punkte pro Kehre
                Convert.ToByte(CurrentSettings.MaxKehrenProSpiel),	//Anzahl der Kehren
                Convert.ToByte(CurrentSettings.MidColumnWidth) ,    //Breite der mittleren Spalte (nur bei der Anzeige von TeamNamen relevant)
                Convert.ToByte(CurrentSettings.MessageVersion),		//Version des Datenpakets. 
                0
            };
            return [.. data];
        }

        public void SetSettings(byte[] settings)
        {
            if (settings == null || settings.Length < 10)
            {
                _logger.LogWarning("SetSettings: Ungültiges Byte-Array (Länge {Len})", settings?.Length ?? -1);
                return;
            }

            if (!Enum.IsDefined(typeof(Settings.MODUS), (int)settings[2]) ||
                !Enum.IsDefined(typeof(Settings.RICHTUNG), (int)settings[3]) ||
                !Enum.IsDefined(typeof(Settings.THEME), (int)settings[4]))
            {
                _logger.LogWarning("SetSettings: Ungültige Enum-Werte im Paket");
                return;
            }


            CurrentSettings.BahnNummer = settings[0];
            CurrentSettings.Spielgruppe = settings[1];

            if (CurrentSettings.Modus != (Settings.MODUS)settings[2])
            {
                CurrentSettings.Modus = (Settings.MODUS)settings[2];
                OnNavigationRequested?.Invoke(GetModusUrl(CurrentSettings.Modus));
            }

            CurrentSettings.Richtung = (Settings.RICHTUNG)settings[3];
            CurrentSettings.Theme = (Settings.THEME)settings[4];
            CurrentSettings.MaxPunkteProKehre = settings[5];
            CurrentSettings.MaxKehrenProSpiel = settings[6];
            CurrentSettings.MidColumnWidth = settings[7];    //Breite der mittleren Spalte (nur bei der Anzeige von TeamNamen relevant)
            _ = settings[8];    //Version des Datenpakets. wird hier nicht verwendet
            _ = settings[9];    //Reserved for future use

            OnSettingsChanged?.Invoke();
        }

        #endregion
    }
}
