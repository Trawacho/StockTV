using StockTvBlazor.Components.Services;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace StockTvBlazor.Components.Models;

public class ZielBewerb(SettingsService settingsService)
{

	public event Action? OnZielBewerbChanged;
	/// <summary>
	/// Summe der beim Massen Vorne erreichten Punkte
	/// </summary>
	public int MassenVorneSumme => _massenVorne.Sum();
	/// <summary>
	/// Summe der beim Schiess erreichten Punkte
	/// </summary>
	public int SchiessenSumme => _schiessen.Sum();
	/// <summary>
	/// Summe der beim Massen Seite erreichten Punkte
	/// </summary>
	public int MassenSeiteSumme => _massenSeite.Sum();
	/// <summary>
	/// Summe der beim Kombinieren erreichten Punkte
	/// </summary>
	public int KombinierenSumme => _kombinieren.Sum();
	/// <summary>
	/// Summe alle Punkte über alle Disziplinen
	/// </summary>
	public int GesamtSumme => MassenVorneSumme + SchiessenSumme + MassenSeiteSumme + KombinierenSumme;

	private readonly List<int> _massenVorne = [];
	private readonly List<int> _schiessen = [];
	private readonly List<int> _massenSeite = [];
	private readonly List<int> _kombinieren = [];
	private readonly SettingsService _settingsService = settingsService;
	private string? _spielerName;

	/// <summary>
	/// Maximale Anzahl an Versuchen pro Disziplin
	/// </summary>
	public int MaxKehrenProSpiel => _settingsService.CurrentSettings.MaxKehrenProSpiel;
	public int MaxVersucheGesamt => MaxKehrenProSpiel * 4;

	public bool IsMassValue(int value)
	{
		return value switch
		{
			0 or 2 or 4 or 6 or 8 or 10 => true,
			_ => false,
		};
	}
	public bool IsSchussValue(int value)
	{
		return value switch
		{
			0 or 2 or 5 or 10 => true,
			_ => false,
		};
	}

	/// <summary>
	/// Anzahl der bereits getätigten Versuche
	/// </summary>
	/// <returns></returns>
	public int AnzahlVersuche() => _massenVorne.Count + _schiessen.Count + _massenSeite.Count + _kombinieren.Count;

	/// <summary>
	/// Liste aller Versuche aller Disziplinen
	/// </summary>
	/// <returns></returns>
	public List<int> AlleVersuche() => _massenVorne.Concat(_schiessen).Concat(_massenSeite).Concat(_kombinieren).ToList();

	/// <summary>
	/// Wert des letzten Versuchs
	/// </summary>
	/// <returns></returns>
	public int LetzterVersuch() => AlleVersuche().LastOrDefault();

	public string Spielername => _spielerName ??= "";

	/// <summary>
	/// Lösche den letzen Versuch 
	/// </summary>
	public void DeleteLastVersuch()
	{
		if (_kombinieren.Count > 0)
			_kombinieren.RemoveAt(_kombinieren.Count - 1);
		else if (_massenSeite.Count > 0)
			_massenSeite.RemoveAt(_massenSeite.Count - 1);
		else if (_schiessen.Count > 0)
			_schiessen.RemoveAt(_schiessen.Count - 1);
		else if (_massenVorne.Count > 0)
			_massenVorne.RemoveAt(_massenVorne.Count - 1);
		OnZielBewerbChanged?.Invoke();

	}

	/// <summary>
	/// Versuch hinzufügen. Wenn Wert ungültig oder alle Dispziplinen voll sind, wird FALSE zurückgegeben
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool AddVersuch(int value)
	{
		if (AnzahlVersuche() >= MaxVersucheGesamt) return false; // Maximal 4 Disziplienen mit jeweils maxVersuche 

		if (_massenVorne.Count < MaxKehrenProSpiel && IsMassValue(value))
		{
			_massenVorne.Add(value);
		}
		else if (_schiessen.Count < MaxKehrenProSpiel && IsSchussValue(value))
		{
			_schiessen.Add(value);
		}
		else if (_massenSeite.Count < MaxKehrenProSpiel && IsMassValue(value))
		{
			_massenSeite.Add(value);
		}
		else if (_kombinieren.Count < MaxKehrenProSpiel && IsMassValue(value))
		{
			_kombinieren.Add(value);
		}
		else
		{
			return false; // Alle Disziplinen voll oder ungültiger Wert
		}
		OnZielBewerbChanged?.Invoke();
		return true;

	}

	/// <summary>
	/// Alle Werte aus allen Disziplinen löschen
	/// </summary>
	public void Reset()
	{
		_massenVorne.Clear();
		_schiessen.Clear();
		_massenSeite.Clear();
		_kombinieren.Clear();
		_spielerName = string.Empty;
		OnZielBewerbChanged?.Invoke();
	}

	public void AddSpielerName(string SpielerName)
	{
		_spielerName = SpielerName;
		OnZielBewerbChanged?.Invoke();
	}

	internal byte[] SerializeJson()
	{
		var values = new List<byte>();
		values.AddRange(_settingsService.GetSettings());

		var daten = new Dictionary<string, SpielModus>
		{
			["MassenVorne"] = new SpielModus { Name = 1, Versuche = _massenVorne },
			["Schiessen"] = new SpielModus { Name = 2, Versuche = _schiessen },
			["MassenSeite"] = new SpielModus { Name = 3, Versuche = _massenSeite },
			["Kombinieren"] = new SpielModus { Name = 4, Versuche = _kombinieren }
		};

		var jsonString = JsonSerializer.Serialize(daten);
		values.AddRange(Encoding.UTF8.GetBytes(jsonString));

		return [.. values];
	}

	private class SpielModus
	{
		public int Name { get; set; }
		public List<int> Versuche { get; set; } = [];
	}

}