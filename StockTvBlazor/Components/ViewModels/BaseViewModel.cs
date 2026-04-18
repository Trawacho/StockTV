using StockTvBlazor.Services;
using StockTvBlazor.Models;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel : IDisposable
{
	protected readonly SettingsService _settingsService;
	private readonly MatchService _matchService;
	private readonly NetMqPublisherService _publisher;
	private int _inputValue;
	private int _specialCounter;
	private readonly Debounce _debounce = new();

	public event Action? OnViewModelChanged;
	public event Action<string>? OnNavigationRequested;

	public BaseViewModel(SettingsService settingsService, MatchService matchService, NetMqPublisherService publisher)
	{
		_settingsService = settingsService;
		_matchService = matchService;
		_publisher = publisher;
		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		_matchService.CurrentMatch.OnMatchChanged += HandleMatchChanged;
	}

	private bool _disposed = false;
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_matchService.CurrentMatch.OnMatchChanged -= HandleMatchChanged;
	}

	private void HandleMatchChanged()
	{
		OnViewModelChanged?.Invoke();
	}
	private void HandleSettingsChanged()
	{
		OnViewModelChanged?.Invoke();
	}


	protected Match CurrentMatch => _matchService.CurrentMatch;

	protected string HeaderTextBasis
	{
		get
		{
			var s = _settingsService.CurrentSettings;
			var prefix = s.BlockLocalChanges ? "." : "";
			return s.SpielgruppeLetter == string.Empty
				? $"{prefix}Bahn: {s.BahnNummer}"
				: $"{prefix}Bahn: {s.SpielgruppeLetter}-{s.BahnNummer}";
		}
	}

	public string InputValue => _inputValue < 0 ? "" : _inputValue.ToString();

	public int LeftPointsSum => CurrentMatch.CurrentGame.LeftPointsSum;

	public int RightPointsSum => CurrentMatch.CurrentGame.RightPointsSum;

	public string LeftPoints => CurrentMatch.CurrentGame.LeftPoints;

	public string RightPoints => CurrentMatch.CurrentGame.RightPoints;

	public string GetShellGridStyle()
	{
		if (!TeamNamesAvailable)
			return "grid-template-columns: 100%;";

		var mid = _settingsService.CurrentSettings.MidColumnWidth;
		var side = (100 - mid) / 2.0;
		return @$"grid-template-columns: {side.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}% 
										  {mid.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}% 
										  {side.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}%;";
	}

	public bool TeamNamesAvailable => !string.IsNullOrEmpty(LeftTeamName);

	public string LeftTeamName
	{
		get
		{
			return CurrentMatch.Begegnungen.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber)
									?.TeamNameLeft(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.LINKS)
									?? string.Empty;
		}
	}

	public string RightTeamName
	{
		get
		{
			return CurrentMatch.Begegnungen.FirstOrDefault(b => b.Spielnummer == CurrentMatch.CurrentGame.GameNumber)
									?.TeamNameRight(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.LINKS)
									?? string.Empty;
		}
	}


	private async Task AddToGreenAsync()
	{

		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, true);

		CurrentMatch.AddTurn(turn);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;

	}
	private async Task AddToRedAsync()
	{
		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, false);

		CurrentMatch.AddTurn(turn);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;
	}
	private async Task ResetAsync(bool force = false)
	{
		CurrentMatch.Reset(force);
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();
		_inputValue = -1;
	}

	private async Task DeleteLastTurnAsync()
	{
		if (_inputValue > 0)
		{
			_inputValue = -1;
			return;
		}

		CurrentMatch.DeleteLastTurn();
		await CurrentMatch.SaveTurnsToLocalSettingsAsync();
	}

	private protected void ShowSpecialPage()
	{
		if (_specialCounter < 5) return;
		_specialCounter = 0;

		if (_inputValue == 0)
		{
			//_navigationManager.NavigateTo("/settings");
			OnNavigationRequested?.Invoke("/settings");

		}
		else if (_inputValue == 10)
		{
			//todo: implement marketing page navigation
			//NavigateTo(typeof(Pages.MarketingPage));
		}

	}

	public void AddInput(int value)
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
			case "*": await AddToGreenAsync(); break;
			case "-": await DeleteLastTurnAsync(); break;
			case "/" or "Backspace": await AddToRedAsync(); break;
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

		_publisher.Publish("GetResult", CurrentMatch.SerializeJson());

	}
}