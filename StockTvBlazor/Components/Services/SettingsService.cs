using StockTvBlazor.Components.Models;
using System.Text.Json;

namespace StockTvBlazor.Components.Services
{
	public class SettingsService
	{
		public SettingsService()
		{
			_settings = LoadSettings();
		}

		private readonly Settings _settings;
		private readonly string _settingsFilePath = "stocktv.config.json";

		public event Action? OnConfigurationChanged;

		public Settings CurrentSettings => _settings;

		public void ChangeModus(bool forward)
		{
			var values = Enum.GetValues<Settings.MODUS>();
			int currentIndex = Array.IndexOf(values, _settings.Modus);


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
				_settings.MaxKehrenProSpiel = 30;
				_settings.MaxPunkteProKehre = 15;
			}
			else
			{
				_settings.MaxKehrenProSpiel = 6;
				_settings.MaxPunkteProKehre = 9;
			}

			_settings.Modus = newModus;
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeTheme(bool forward)
		{
			var values = Enum.GetValues<Settings.THEME>();
			int currentIndex = Array.IndexOf(values, _settings.Theme);
			if (forward)
			{
				currentIndex = (currentIndex + 1) % values.Length;
			}
			else
			{
				currentIndex = (currentIndex - 1 + values.Length) % values.Length;
			}
			_settings.Theme = values[currentIndex];
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeRichtung(bool forward)
		{
			var values = Enum.GetValues<Settings.RICHTUNG>();
			int currentIndex = Array.IndexOf(values, _settings.Richtung);
			if (forward)
			{
				currentIndex = (currentIndex + 1) % values.Length;
			}
			else
			{
				currentIndex = (currentIndex - 1 + values.Length) % values.Length;
			}
			_settings.Richtung = values[currentIndex];
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeBlockLocalChanges()
		{
			_settings.BlockLocalChanges = !_settings.BlockLocalChanges;
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeSpielgruppe(bool forward)
		{
			if (forward)
			{
				if (_settings.Spielgruppe < 10)
				{
					_settings.Spielgruppe++;
				}
			}
			else
			{
				if (_settings.Spielgruppe > 0)
				{
					_settings.Spielgruppe--;
				}
			}

			OnConfigurationChanged?.Invoke();
		}

		public void ChangeBahnNummer(bool forward)
		{
			if (forward)
			{
				if (_settings.BahnNummer < 30) // Assuming 99 is the maximum value
					_settings.BahnNummer++;
			}
			else
			{
				if (_settings.BahnNummer > 1) // Assuming 1 is the minimum value
					_settings.BahnNummer--;
			}
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeMaxMaxKehrenProSpiel(bool forward)
		{
			if (forward)
			{
				if (_settings.MaxKehrenProSpiel < 30) // Assuming 30 is the maximum value
					_settings.MaxKehrenProSpiel++;
			}
			else
			{
				if (_settings.MaxKehrenProSpiel > 4) // Assuming 4 is the minimum value
					_settings.MaxKehrenProSpiel--;
			}
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeMaxPunkteProKehre(bool forward)
		{
			if (forward)
			{
				if (_settings.MaxPunkteProKehre < 15) // Assuming 15 is the maximum value
					_settings.MaxPunkteProKehre++;
			}
			else
			{
				if (_settings.MaxPunkteProKehre > 0) // Assuming 0 is the minimum value
					_settings.MaxPunkteProKehre--;
			}
			OnConfigurationChanged?.Invoke();
		}

		public void ChangeNetworking()
		{
			//todo: implement network connection logic when enabling networking
			_settings.Networking = !_settings.Networking;
			OnConfigurationChanged?.Invoke();
		}

		#region Load and Save
		private Settings LoadSettings()
		{
			if (File.Exists(_settingsFilePath))
			{
				try
				{
					var json = File.ReadAllText(_settingsFilePath);
					return JsonSerializer.Deserialize<Settings>(json) ?? new();
				}
				catch
				{
					/* Log error or fallback to default */
					return new Settings();
				}
			}
			return new Settings();

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
				OnConfigurationChanged?.Invoke();
			}
			catch (Exception ex)
			{
				// Hier ggf. Logger injizieren und Fehler loggen
				Console.WriteLine($"Fehler beim Speichern der Config: {ex.Message}");
			}
		}

		internal async Task SaveTurns(List<Turn> turns)
		{
			if (_settings.Modus == Settings.MODUS.TRAINING)
			{
				// Im Trainingsmodus werden die Kehren nicht gespeichert, da sie nur temporär sind
				return;
			}
			_settings.Kehren.Clear();
			_settings.Kehren.AddRange(turns);
			await SaveSettingsAsync();
		}

		internal List<ITurn> LoadTurns()
		{
			// Im Trainingsmodus werden die Kehren nicht geladen, da sie nur temporär sind
			if (_settings.Modus == Settings.MODUS.TRAINING)
				return new List<ITurn>();

			LoadSettings();
			return _settings.Kehren;
		}
		#endregion

	}
}
