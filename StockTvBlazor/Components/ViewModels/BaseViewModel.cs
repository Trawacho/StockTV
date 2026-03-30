using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel : IDisposable
{
	protected readonly SettingsService _settingsService;
	private readonly MatchService _matchService;
	private readonly NavigationManager _navigationManager;
	private readonly NetMqPublisherService _publisher;
	private int _inputValue;
	private int _specialCounter;
	private readonly Debounce _debounce = new();

	public event Action? OnViewModelChanged;

	public BaseViewModel(SettingsService settingsService, MatchService matchService, NavigationManager navigationManager, NetMqPublisherService publisher)
	{
		_settingsService = settingsService;
		_matchService = matchService;
		_navigationManager = navigationManager;
		_publisher = publisher;
		_settingsService.OnSettingsChanged += HandleSettingsChanged;
		_settingsService.OnModusChanged += HandleModusChanged;
		_matchService.CurrentMatch.OnMatchChanged += HandleMatchChanged;
	}

	public void Dispose()
	{
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_settingsService.OnModusChanged -= HandleModusChanged;
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
	private void HandleModusChanged()
	{
		switch (_settingsService.CurrentSettings.Modus)
		{
			case Settings.MODUS.BESTOF:
				_navigationManager.NavigateTo("/bestof");
				break;
			case Settings.MODUS.TRAINING:
				_navigationManager.NavigateTo("/training");
				return;
			case Settings.MODUS.TURNIER:
				_navigationManager.NavigateTo("/turnier");
				return;
		}
	}

	protected Models.Match Match => _matchService.CurrentMatch;
	

	public string InputValue => _inputValue < 0 ? "" : _inputValue.ToString();

	public int LeftPointsSum => Match.CurrentGame.LeftPointsSum;

	public int RightPointsSum => Match.CurrentGame.RightPointsSum;

	public string LeftPoints => Match.CurrentGame.LeftPoints;

	public string RightPoints => Match.CurrentGame.RightPoints;

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
			return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
									?.TeamNameLeft(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.LINKS)
									?? string.Empty;
		}
	}

	public string RightTeamName
	{
		get
		{
			return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
									?.TeamNameRight(_settingsService.CurrentSettings.Richtung == Settings.RICHTUNG.LINKS)
									?? string.Empty;
		}
	}


	private async Task AddToGreenAsync()
	{

		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, true);

		Match.AddTurn(turn);
		await Match.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;

	}
	private async Task AddToRedAsync()
	{
		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, false);

		Match.AddTurn(turn);
		await Match.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;
	}
	private async Task ResetAsync(bool force = false)
	{
		Match.Reset(force);
		await Match.SaveTurnsToLocalSettingsAsync();
		_inputValue = -1;
	}

	private async Task DeleteLastTurnAsync()
	{
		if (_inputValue > 0)
		{
			_inputValue = -1;
			return;
		}

		Match.DeleteLastTurn();
		await Match.SaveTurnsToLocalSettingsAsync();
	}

	private protected void ShowSpecialPage()
	{
		if (_specialCounter < 5) return;
		_specialCounter = 0;

		if (_inputValue == 0)
		{
			_navigationManager.NavigateTo("/settings");

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

		_publisher.Publish("GetResult", Match.SerializeJson());

	}
}