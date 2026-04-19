using StockTvBlazor.Models;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Services;

public class ZielService(SettingsService settingsService, ILogger<MatchService> logger, NetMqPublisherService publisherService)
{
	private readonly SettingsService _settingsService = settingsService;
	private readonly ILogger<MatchService> _logger = logger;
	private readonly NetMqPublisherService _publisherService = publisherService;
	private ZielBewerb? _currentZielBewerb;

	public void InitializeZiel()
	{
		_currentZielBewerb ??= new ZielBewerb(_settingsService);
		_logger.LogInformation("ZielService wurde initialisiert.");
	}

	public ZielBewerb CurrentZielBewerb => _currentZielBewerb
			?? throw new InvalidOperationException("ZielBewerb wurde nicht initialisiert. Prüfe Program.cs!");

	public void SetTeilnehmer(byte[] name)
	{
		var spielerName = System.Text.Encoding.UTF8.GetString(name);
		CurrentZielBewerb.AddSpielerName(spielerName);
	}



	private int _inputValue;
	public int InputValue => _inputValue;
	public int _specialCounter;
	private readonly Debounce _debounce = new();

	public event Action? OnGlobalRefresh;
	public event Action<string>? OnNavigationRequested;

	public void RequestGlobalRefresh() => OnGlobalRefresh?.Invoke();


	private bool _invalidInput;
	public bool InvalidInput
	{
		get => _invalidInput;
		set
		{
			if (_invalidInput != value)
			{
				_invalidInput = value;
				OnGlobalRefresh?.Invoke();
			}
		}
	}

	public async Task ProcessKeyAsync(string value)
	{
		var s = _settingsService.CurrentSettings;

		if ((_inputValue == 0 || _inputValue == 10)
			&& value == "Enter"
			&& !s.General.BlockLocalChanges)
		{
			_specialCounter++;
		}
		else
		{
			_specialCounter = 0;
		}

		if (!(value == "-" && _inputValue == 0 && !s.General.BlockLocalChanges))
		{
			if (!_debounce.IsDebounceOk(value))
				return;
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

		OnGlobalRefresh?.Invoke();
		_publisherService.Publish("GetResult", CurrentZielBewerb.SerializeJson());
	}

	private void AddInput(int value)
	{
		var s = _settingsService.CurrentSettings;

		int newValue = (_inputValue < 0) ? value : (_inputValue * 10) + value;
		int maxPoints = s.Game.MaxPunkteProKehre;

		if (newValue <= maxPoints)
			_inputValue = newValue;
		else
			_inputValue = (value <= maxPoints) ? value : -1;
	}

	private async Task ResetAsync()
	{
		var s = _settingsService.CurrentSettings;

		if (CurrentZielBewerb.AnzahlVersuche() < s.Game.MaxKehrenProSpiel * 4)
			return;

		CurrentZielBewerb.Reset();
		_inputValue = -1;
	}

	private async Task AddToZielBewerb()
	{
		if (_inputValue < 0) return;

		if (!CurrentZielBewerb.AddVersuch(_inputValue))
		{
			_inputValue = -1;

			InvalidInput = true;
			await Task.Delay(1500);
			InvalidInput = false;
			return;
		}

		_inputValue = -1;
	}

	private async Task DeleteLastValueAsync()
	{
		if (_inputValue >= 0)
			_inputValue = -1;
		else
			CurrentZielBewerb.DeleteLastVersuch();
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
			// TODO Marketing
		}
	}
}
