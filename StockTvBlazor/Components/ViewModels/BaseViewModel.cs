using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;
using System.Runtime.InteropServices.Marshalling;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel : IDisposable
{
	protected readonly SettingsService _settingsService;// = settingsService;
	private readonly MatchService _matchService;// = matchService;
	private readonly NavigationManager _navigationManager;// = navigationManager;
	private readonly NetMqPublisherService _publisher;// = publisher;
	private int _inputValue;
	private int _specialCounter;

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
	protected int InputValue => _inputValue;


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
			if (!Debounce.IsDebounceOk(value))
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

	public void Dispose()
	{
		// Hier können Ressourcen freigegeben werden, z.B. Event-Handler abmelden
		_settingsService.OnSettingsChanged -= HandleSettingsChanged;
		_matchService.CurrentMatch.OnMatchChanged -= HandleMatchChanged;
	}
}
