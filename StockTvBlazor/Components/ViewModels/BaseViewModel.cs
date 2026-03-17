using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels;

public abstract class BaseViewModel(SettingsService settingsService, NavigationManager navigationManager) 
{
	protected readonly SettingsService _settingsService = settingsService;
	private readonly NavigationManager _navigationManager = navigationManager;

	private int _inputValue;
	private int _specialCounter;
	private readonly Models.Match _match = new(settingsService);

	public event Action? OnViewModelChanged;
	protected Models.Match Match => _match; 
	protected int InputValue => _inputValue;
	

	private async Task AddToGreenAsync()
	{

		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, true);

		_match.AddTurn(turn);
		await _match.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;

	}
	private async Task AddToRedAsync()
	{
		if (_inputValue == -1)
			return;

		var turn = Turn.Create(_inputValue, _settingsService.CurrentSettings.Richtung, true);

		_match.AddTurn(turn);
		await _match.SaveTurnsToLocalSettingsAsync();

		_inputValue = -1;
	}
	private async Task ResetAsync(bool force = false)
	{
		_match.Reset(force);
		await _match.SaveTurnsToLocalSettingsAsync();
		_inputValue = -1;
	}

	private async Task DeleteLastTurnAsync()
	{
		if (_inputValue > 0)
		{
			_inputValue = -1;
			return;
		}

		_match.DeleteLastTurn();
		await _match.SaveTurnsToLocalSettingsAsync();
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
			case "Enter":				ShowSpecialPage();				break;
			case "*":					await AddToGreenAsync();		break;
			case "-":					await DeleteLastTurnAsync();	break;
			case "/" or "Backspace":	await AddToRedAsync();			break;
			case "+":					await ResetAsync();				break;

			default:
				int? input = value switch
				{
					"1" or "End" =>			1,
					"2" or "ArrowDown" =>	2,
					"3" or "PageDown" =>	3,
					"4" or "ArrowLeft" =>	4,
					"5" or "Clear" =>		5,
					"6" or "ArrowRight" =>	6,
					"7" or "Home" =>		7,
					"8" or "ArrowUp" =>		8,
					"9" or "PageUp" =>		9,
					"0" or "Insert" =>		0,
					_ => null
				};

				if (input.HasValue) 
					AddInput(input.Value);

				break;
		}

		OnViewModelChanged?.Invoke();

		//todo: Send after each key press a network notification
		//if (Settings.IsBroadcasting)
		//{
		//	if (Settings.MessageVersion == 0)
		//		BroadcastService.SendData(_match.Serialize());
		//	else if (Settings.MessageVersion == 1)
		//		BroadcastService.SendData(_match.SerializeJson());
		//}
	}
}
