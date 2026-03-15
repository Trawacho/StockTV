using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockTvBlazor.Components.ViewModels
{
	public abstract class BaseViewModel(Models.Settings settings, NavigationManager navigationManager) : INotifyPropertyChanged
	{
		private int _inputValue;
		private int _specialCounter;
		private readonly Models.Match _match = new(settings);
		protected readonly Models.Settings _settings = settings;
		private readonly NavigationManager _navigationManager = navigationManager;

		public event PropertyChangedEventHandler? PropertyChanged;
		internal void RaisePropertyChange([CallerMemberName] string? propertyname = null)
		{
			var handler = PropertyChanged;
			handler?.Invoke(this, new PropertyChangedEventArgs(propertyname));
		}

		internal void RaiseAllPropertysChanged()
		{
			foreach (var prop in this.GetType().GetProperties())
			{
				RaisePropertyChange(prop.Name);
			}
		}
		protected Models.Match Match { get { return _match; } }
		protected int InputValue => _inputValue;
		protected void SpecialCounterIncrease() => _specialCounter++;
		protected void SpecialCounterReset() => _specialCounter = 0;

		private void AddToGreen()
		{

			if (_inputValue == -1)
				return;

			var turn = new Turn();

			if (_settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left)
			{
				turn.PointsRight = Convert.ToByte(_inputValue);
			}
			else
			{
				turn.PointsLeft = Convert.ToByte(_inputValue);
			}

			this._match.AddTurn(turn);


			_inputValue = -1;

		}
		private void AddToRed()
		{
			if (_inputValue == -1)
				return;

			var turn = new Turn();

			if (_settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Right)
			{
				turn.PointsRight = Convert.ToByte(_inputValue);
			}
			else
			{
				turn.PointsLeft = Convert.ToByte(_inputValue);
			}

			this._match.AddTurn(turn);

			_inputValue = -1;
		}
		private void Reset(bool force = false)
		{
			_match.Reset(force);
			_inputValue = -1;
		}

		private void DeleteLastTurn()
		{
			if (_inputValue > 0)
			{
				_inputValue = -1;
				return;
			}

			_match.DeleteLastTurn();
		}

		private protected void ShowSpecialPage()
		{
			if (_specialCounter < 5) return;
			_specialCounter = 0;

			if (_inputValue == 0)
			{
				_navigationManager.NavigateTo("/config");

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
			int maxPoints = _settings.GameSettings.PointsPerTurn;

			if (newValue <= maxPoints)
			{
				_inputValue = newValue;
			}
			else
			{
				_inputValue = (value <= maxPoints) ? value : -1;
			}
		}

		public void ProcessKey(string value)
		{

			//Settings Or Marekting SpecialCounter
			if ((_inputValue == 0 || _inputValue == 10)
				&& value == "Enter"
				&& !_settings.BlockLocalChanges)
			{
				SpecialCounterIncrease();
			}
			else
			{
				SpecialCounterReset();
			}


			//Debouncing 
			if (!(value == "-" && _inputValue == 0 && !_settings.BlockLocalChanges)) //Blaue Taste und 0 sowie kein BlockLocalChanges übergeht die Debounce-Funktion
			{
				if (!Debounce.IsDebounceOk(value))
				{
					return;
				}
			}

			switch (value)
			{
				case "Enter":				ShowSpecialPage();	break;
				case "*":					AddToGreen();		break;
				case "-":					DeleteLastTurn();	break;
				case "/" or "Backspace":	AddToRed();			break;
				case "+":					Reset();			break;

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

					if (input.HasValue) AddInput(input.Value);
					break;
			}

			RaiseAllPropertysChanged();

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
}
