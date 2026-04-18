using StockTvBlazor.Services;
using StockTvBlazor.Models;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Components.ViewModels;

public class ZielViewModel : IDisposable
{

	public ZielViewModel(ZielService zielService, SettingsService settingsService, NetMqPublisherService publisherService)
	{
		_zielService = zielService;
		_settingsService = settingsService;
		_publisherService = publisherService;
		_currentBewerb = _zielService.CurrentZielBewerb;
		_currentBewerb.OnZielBewerbChanged += HandleZielBewerbChanged;
	}

	public event Action<string>? OnNavigationRequested;
	public event Action? OnViewModelChanged;

	private bool _disposed = false;
	private readonly ZielService _zielService;
	private readonly SettingsService _settingsService;
	private readonly NetMqPublisherService _publisherService;
	private readonly ZielBewerb _currentBewerb;

	public string GesamtText => "Gesamt:";          // steht oben rechts als fixer Text
	public string SpielerName =>                    // steht ganz oben und wird nur angezeigt, wenn auch vorhanden
		_currentBewerb.Spielername;
	private int _inputValue;                        // interne Variable, die den aktuellen Eingabewert speichert.
	public int _specialCounter;                     // zählt, wie oft hintereinander die Enter-Taste gedrückt wurde um zu special Page zu gelangen. Reset, sobald andere Taste gedrückt wird
	private readonly Debounce _debounce = new();    // Hilfsklasse, um schnelle aufeinanderfolgende Eingaben zu filtern (Debouncing)


	private void HandleZielBewerbChanged()
	{
		OnViewModelChanged?.Invoke();
	}

	/// <summary>
	/// True, wenn Spielername vorhanden ist.
	/// </summary>
	public bool IsSpielernameAvailable => !string.IsNullOrEmpty(SpielerName);

	/// <summary>
	/// Alle Versuche addiert, zur Anzeige mitte rechts
	/// </summary>
	public string GesamtPunkteText => _zielService.CurrentZielBewerb.GesamtSumme.ToString();

	/// <summary>
	/// Formatierter String mit den bereits gespielten Versuchen und der maximal möglichen Anzahl an Versuchen, zur Anzeige oben links. Beispiel: "4/24"
	/// </summary>
	public string AnzahlVersuche => $"{_zielService.CurrentZielBewerb.AnzahlVersuche()}/{_settingsService.CurrentSettings.MaxKehrenProSpiel * 4}";

	/// <summary>
	/// Formatierter String mit den Summen der 4 Disziplinen, zur Anzeige unten. Beispiel: "24 - 15 - 30 - 10"
	/// </summary>
	public string SummeDerVersuche => $"{_currentBewerb.MassenVorneSumme} - {_currentBewerb.SchiessenSumme} - {_currentBewerb.MassenSeiteSumme} - {_currentBewerb.KombinierenSumme}";

	/// <summary>
	/// Anzeige des Eingabewerts, wenn dieser Größer oder gleich 0 ist
	/// </summary>
	public string InputValue => _inputValue < 0 ? "" : _inputValue.ToString();

	/// <summary>
	/// Wert des letzten Versuchs der gültig eingegeben wurde, zur Anzeige mitte links
	/// </summary>
	public string LastValue => _inputValue == -1
		? _currentBewerb.AnzahlVersuche() > 0
		? _currentBewerb.LetzterVersuch().ToString()
			: string.Empty
		: string.Empty;

	private bool _invalidInput;
	public bool InvalidInput
	{
		get => _invalidInput;
		set
		{
			if (_invalidInput != value)
			{
				_invalidInput = value;
				OnViewModelChanged?.Invoke();
			}
		}
	}


	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_currentBewerb.OnZielBewerbChanged -= HandleZielBewerbChanged;
	}

	public async Task ProcessKeyAsync(string value)
	{
		//Settings Or Marekting SpecialCounter
		if ((_inputValue == 0 || _inputValue == 10)
			&& value == "Enter"
			&& !_settingsService.CurrentSettings.BlockLocalChanges)
		{
			_specialCounter++;
		}
		else
		{
			_specialCounter = 0;
		}


		//Debouncing 
		if (!(value == "-" && _inputValue == 0 && !_settingsService.CurrentSettings.BlockLocalChanges)) //Blaue Taste und 0 sowie kein BlockLocalChanges übergeht die Debounce-Funktion
		{
			if (!_debounce.IsDebounceOk(value))
			{
				return;
			}
		}

		switch (value)
		{
			case "Enter": ShowSpecialPage(); break;
			case "/" or "Backspace" or "*": await AddToZielBewerb(); break;
			case "-": await DeleteLastValueAsync(); break;
			case "+": await ResetAsync(); break;

			default:
				int? input = value switch
				{
					"1" or "End" => 1,
					"2" or "ArrowDown" => 2,
					"3" or "PageDown" => 3,
					"4" or "ArrowLeft" => 4,
					"5" or "Clear" => 5,
					"6" or "ArrowRight" => 6,
					"7" or "Home" => 7,
					"8" or "ArrowUp" => 8,
					"9" or "PageUp" => 9,
					"0" or "Insert" => 0,
					_ => null
				};

				if (input.HasValue)
					AddInput(input.Value);

				break;
		}

		OnViewModelChanged?.Invoke();
		_publisherService.Publish("GetResult", _currentBewerb.SerializeJson());
	}

	private void AddInput(int value)
	{
		int newValue = (_inputValue < 0) ? value : (_inputValue * 10) + value;
		int maxPoints = _settingsService.CurrentSettings.MaxPunkteProKehre;

		if (newValue <= maxPoints)
		{
			_inputValue = newValue;
		}
		else
		{
			_inputValue = (value <= maxPoints) ? value : -1;
		}
	}

	/// <summary>
	/// Wenn alle Versuche gemacht wurden, den Spielernamen löschen, alle Versuche zurücksetzen und die Anzeige des aktuellen Eingabewerts löschen.
	/// </summary>
	/// <returns></returns>
	private async Task ResetAsync()
	{
		if (_currentBewerb.AnzahlVersuche() < _settingsService.CurrentSettings.MaxKehrenProSpiel * 4) return;

		_currentBewerb.Reset();
		_inputValue = -1;
	}

	private async Task AddToZielBewerb()
	{
		if (_inputValue < 0) return;
		if (!_currentBewerb.AddVersuch(_inputValue))
		{
			_inputValue = -1;

			InvalidInput = true;
			await Task.Delay(1500);
			InvalidInput = false;
			return;
		}
		_inputValue = -1;
	}

	/// <summary>
	/// Letzte Eingabe aus inputvalue löschen, wenn dieser noch nicht  zum Bewerb hinzugefügt wurde.
	/// Ansonsten letzten Wert aus Bewerb löschen. In beiden Fällen wird inputvalue auf -1 zurückgesetzt, damit die Anzeige verschwindet.
	/// </summary>
	/// <returns></returns>
	private async Task DeleteLastValueAsync()
	{
		if (_inputValue >= 0)
			_inputValue = -1;
		else
			_currentBewerb.DeleteLastVersuch();
	}

	private protected void ShowSpecialPage()
	{
		if (_specialCounter < 5) return;
		_specialCounter = 0;

		if (_inputValue == 0)
		{
			OnNavigationRequested?.Invoke("/settings");
		}
		else if (_inputValue == 10)
		{
			//todo: implement marketing page navigation
			//NavigateTo(typeof(Pages.MarketingPage));
		}
	}
}