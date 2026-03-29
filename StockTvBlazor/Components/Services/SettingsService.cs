using StockTvBlazor.Components.Models;
using System.Text.Json;

namespace StockTvBlazor.Components.Services
{
	public class SettingsService
	{
		private Settings? _settings;
		private readonly string _settingsFilePath = "stocktv.config.json";
		private readonly ILogger _logger;

		public event Action? OnSettingsChanged;
		public event Action? OnModusChanged;

		public SettingsService(ILogger<SettingsService> logger)
		{
			_logger = logger;
			_logger.LogDebug("SettingsService created - Instanz: " + GetHashCode());
		}

		public Settings CurrentSettings
		{
			get
			{
				if (_settings == null)
					throw new NullReferenceException("SettingsService wurde nicht initialisiert");

				return _settings;
			}
		}

		public async Task InitializeAsync()
		{
			_settings = await LoadSettingsAsync();
		}

		public void ChangeModus(bool forward)
		{
			var values = Enum.GetValues<Settings.MODUS>();
			int currentIndex = Array.IndexOf(values, CurrentSettings.Modus);


			if (forward)
			{
				// Aufwärts: (Index + 1) Modulo Länge
				currentIndex = (currentIndex + 1) % values.Length;
			}
			else
			{
				// Abwärts: (Index - 1 + Länge) Modulo Länge (verhindert negative Werte)
				currentIndex = (currentIndex - 1 + values.Length) % values.Length;
			}

			var newModus = values[currentIndex];
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
			OnSettingsChanged?.Invoke();
		}

		public void ChangeTheme(bool forward)
		{
			var values = Enum.GetValues<Settings.THEME>();
			int currentIndex = Array.IndexOf(values, CurrentSettings.Theme);
			if (forward)
			{
				currentIndex = (currentIndex + 1) % values.Length;
			}
			else
			{
				currentIndex = (currentIndex - 1 + values.Length) % values.Length;
			}
			CurrentSettings.Theme = values[currentIndex];
			OnSettingsChanged?.Invoke();
		}

		public void ChangeRichtung(bool forward)
		{
			var values = Enum.GetValues<Settings.RICHTUNG>();
			int currentIndex = Array.IndexOf(values, CurrentSettings.Richtung);
			if (forward)
			{
				currentIndex = (currentIndex + 1) % values.Length;
			}
			else
			{
				currentIndex = (currentIndex - 1 + values.Length) % values.Length;
			}
			CurrentSettings.Richtung = values[currentIndex];
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
					Console.WriteLine($"Fehler beim Laden der Config: {ex.Message}");
					return new Settings();
				}
			}
			else
			{
				Console.WriteLine("Keine Config-Datei gefunden, Standardwerte werden verwendet.");
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

				// 3. Event feuern (optional hier oder in der aufrufenden Methode)
				OnSettingsChanged?.Invoke();
			}
			catch (Exception ex)
			{
				// Hier ggf. Logger injizieren und Fehler loggen
				Console.WriteLine($"Fehler beim Speichern der Config: {ex.Message}");
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
			CurrentSettings.BahnNummer = settings[0];
			CurrentSettings.Spielgruppe = settings[1];

			if (CurrentSettings.Modus != (Settings.MODUS)settings[2])
			{
				CurrentSettings.Modus = (Settings.MODUS)settings[2];
				OnModusChanged?.Invoke();
			}

			CurrentSettings.Modus = (Settings.MODUS)settings[2];
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
