using StockTvBlazor.Components.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockTvBlazor.Components.ViewModels
{
	public abstract class BaseViewModel : INotifyPropertyChanged
	{
		protected BaseViewModel(Models.Settings settings)
		{
			_settings = settings;
			_match = new Models.Match(settings);
		}

		private int _inputValue;
		private int _specialCounter;
		private readonly Models.Match _match;
		protected readonly Models.Settings _settings;

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
		protected void SpecialCounterIncrease()
		{
			_specialCounter++;
		}
		protected void SpecialCounterReset()
		{
			_specialCounter = 0;
		}

		private void AddInput(int value)
		{
			if (_inputValue < 0)
			{
				if (value <= _settings.GameSettings.PointsPerTurn)
					_inputValue = value;
			}
			else if ((_inputValue * 10) + value <= _settings.GameSettings.PointsPerTurn)
			{
				_inputValue = Convert.ToSByte((_inputValue * 10) + value);
			}
			else
			{
				if (value <= _settings.GameSettings.PointsPerTurn)
					_inputValue = value;
				else
					_inputValue = -1;
			}
		}

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

		public void GetScanCode(string value)
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


			/*
             * ScanCode of KeyPad
             * ScanCode: 69 NumberKeyLock
             * ScanCode: 82 0
             * ScanCode: 79 1
             * ScanCode: 80 2
             * ScanCode: 81 3
             * ScanCode: 75 4
             * ScanCode: 76 5
             * ScanCode: 77 6
             * ScanCode: 71 7
             * ScanCode: 72 8
             * ScanCode: 73 9
             * ScanCode: 53 /                   --> ROT
             * ScanCode: 55 *                   --> GRÜN
             * ScanCode: 74 -                   --> BLAU
             * ScanCode: 78 +                   --> GELB
             * ScanCode: 28 Enter
             * ScanCode: 83 ,
             * ScanCode: 14 BackSpace           --> ROT
             *
             */

			switch (value)
			{
				case "NumLock":    // NumLock
				case ",":    // ,
					break;

				case "Enter":    // Enter
								 // ShowSpecialPage(_inputValue);
					break;

				case "*":    // *                    --> GRÜN
					AddToGreen();
					break;

				case "-":    // -                    --> BLAU
					DeleteLastTurn();
					break;

				case "/":    // /                    --> ROT
				case "Backspace":    // BackSpace
					AddToRed();
					break;

				case "+":    // +                    --> GELB
					Reset();
					break;


				#region Numbers 1 to 0

				case "1":
					//MyWertung.InputText = "1";
					AddInput(1);
					break;
				case "2":
					//MyWertung.InputText = "2";
					AddInput(2);
					break;
				case "3":
					//MyWertung.InputText = "3";
					AddInput(3);
					break;
				case "4":
					//MyWertung.InputText = "4";
					AddInput(4);
					break;
				case "5":
					//MyWertung.InputText = "5";
					AddInput(5);
					break;
				case "6":
					//MyWertung.InputText = "6";
					AddInput(6);
					break;
				case "7":
					//MyWertung.InputText = "7";
					AddInput(7);
					break;
				case "8":
					//MyWertung.InputText = "8";
					AddInput(8);
					break;
				case "9":
					//MyWertung.InputText = "9";
					AddInput(9);
					break;
				case "0":
					//MyWertung.InputText = "0";
					AddInput(0);
					break;

				#endregion
			}

			RaiseAllPropertysChanged();

			// Send after each key press a network notification
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
