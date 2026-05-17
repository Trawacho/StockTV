using StockTvBlazor.Services;
using System.Text;
using System.Text.Json;

namespace StockTvBlazor.Models;

public class ZielBewerb
{
	public event Action? OnZielBewerbChanged;

	private readonly SettingsService _settingsService;

	public ZielBewerb(SettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public int MassenVorneSumme => _massenVorne.Sum();
	public int SchiessenSumme => _schiessen.Sum();
	public int MassenSeiteSumme => _massenSeite.Sum();
	public int KombinierenSumme => _kombinieren.Sum();

	public int GesamtSumme =>
		MassenVorneSumme + SchiessenSumme + MassenSeiteSumme + KombinierenSumme;

	private readonly List<int> _massenVorne = [];
	private readonly List<int> _schiessen = [];
	private readonly List<int> _massenSeite = [];
	private readonly List<int> _kombinieren = [];

	private string? _spielerName;

	public int MaxKehrenProSpiel => _settingsService.CurrentSettings.Game.MaxKehrenProSpiel;
	public int MaxVersucheGesamt => MaxKehrenProSpiel * 4;

	public bool IsMassValue(int value)
		=> value is 0 or 2 or 4 or 6 or 8 or 10;

	public bool IsSchussValue(int value)
		=> value is 0 or 2 or 5 or 10;

	public int AnzahlVersuche()
		=> _massenVorne.Count
		 + _schiessen.Count
		 + _massenSeite.Count
		 + _kombinieren.Count;

	public List<int> AlleVersuche()
		=> _massenVorne
			.Concat(_schiessen)
			.Concat(_massenSeite)
			.Concat(_kombinieren)
			.ToList();

	public int LetzterVersuch()
		=> AlleVersuche().LastOrDefault();

	public string Spielername => _spielerName ?? string.Empty;

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

	public bool AddVersuch(int value)
	{
		if (AnzahlVersuche() >= MaxVersucheGesamt)
			return false;

		if (_massenVorne.Count < MaxKehrenProSpiel && IsMassValue(value))
		{
			_massenVorne.Add(value);
		}
		else if (_massenVorne.Count >= MaxKehrenProSpiel &&
				 _schiessen.Count < MaxKehrenProSpiel &&
				 IsSchussValue(value))
		{
			_schiessen.Add(value);
		}
		else if (_schiessen.Count >= MaxKehrenProSpiel &&
				 _massenSeite.Count < MaxKehrenProSpiel &&
				 IsMassValue(value))
		{
			_massenSeite.Add(value);
		}
		else if (_massenSeite.Count >= MaxKehrenProSpiel &&
				 _kombinieren.Count < MaxKehrenProSpiel &&
				 IsMassValue(value))
		{
			_kombinieren.Add(value);
		}
		else
		{
			return false;
		}

		OnZielBewerbChanged?.Invoke();
		return true;
	}

	public void Reset()
	{
		_massenVorne.Clear();
		_schiessen.Clear();
		_massenSeite.Clear();
		_kombinieren.Clear();
		_spielerName = string.Empty;

		OnZielBewerbChanged?.Invoke();
	}

	public void AddSpielerName(string spielerName)
	{
		_spielerName = spielerName;
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

		var json = JsonSerializer.Serialize(daten);
		values.AddRange(Encoding.UTF8.GetBytes(json));

		return [.. values];
	}

	private class SpielModus
	{
		public int Name { get; set; }
		public List<int> Versuche { get; set; } = [];
	}
}
