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

	public int GesamtSumme
	{
		get
		{
			var aktuelleRunde = MassenVorneSumme + SchiessenSumme + MassenSeiteSumme + KombinierenSumme;
			return IsZiel2Mode ? _runde1Summe + aktuelleRunde : aktuelleRunde;
		}
	}

	private bool IsZiel2Mode => _settingsService.CurrentSettings.Game.CurrentModus == Settings.GameSettings.Modus.Ziel2;

	private readonly List<int> _massenVorne = [];

	private readonly List<int> _schiessen = [];

	private readonly List<int> _massenSeite = [];

	private readonly List<int> _kombinieren = [];

	private string? _spielerName;

	private int _runde1Summe = 0;

	private int _aktuellerDurchgang = 1;

	public int MaxKehrenProSpiel => _settingsService.CurrentSettings.Game.MaxKehrenProSpiel;

	public int MaxVersucheGesamt => MaxKehrenProSpiel * 4;
	public int MaxVersucheDisplay => IsZiel2Mode ? MaxVersucheGesamt * 2 : MaxVersucheGesamt;
	public int AnzahlVersucheDisplay => _aktuellerDurchgang == 1 ? AnzahlVersuche() : AnzahlVersuche() + MaxVersucheGesamt;

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

	private bool TryAddVersuchInternal(int value)
	{
		var phases = new (List<int> Phase, Func<int, bool> Validator)[]
		{
			(_massenVorne, IsMassValue),
			(_schiessen, IsSchussValue),
			(_massenSeite, IsMassValue),
			(_kombinieren, IsMassValue),
		};

		for (int i = 0; i < phases.Length; i++)
		{
			var (phase, validator) = phases[i];

			if (i > 0 && phases[i - 1].Phase.Count < MaxKehrenProSpiel)
				continue;

			if (phase.Count < MaxKehrenProSpiel && validator(value))
			{
				phase.Add(value);
				return true;
			}
		}

		return false;
	}

	public bool AddVersuch(int value)
	{
		bool success = false;

		if (AnzahlVersuche() >= MaxVersucheGesamt)
		{
			if (IsZiel2Mode && _aktuellerDurchgang == 1)
			{
				_runde1Summe += MassenVorneSumme + SchiessenSumme + MassenSeiteSumme + KombinierenSumme;
				_massenVorne.Clear();
				_schiessen.Clear();
				_massenSeite.Clear();
				_kombinieren.Clear();
				_aktuellerDurchgang = 2;
				success = TryAddVersuchInternal(value);
			}
		}
		else
		{
			success = TryAddVersuchInternal(value);
		}

		if (success)
			OnZielBewerbChanged?.Invoke();

		return success;
	}

	public void Reset()
	{
		_massenVorne.Clear();
		_schiessen.Clear();
		_massenSeite.Clear();
		_kombinieren.Clear();
		_spielerName = string.Empty;
		_runde1Summe = 0;
		_aktuellerDurchgang = 1;

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
