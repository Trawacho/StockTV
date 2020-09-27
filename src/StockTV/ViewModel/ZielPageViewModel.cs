using StockTV.Classes;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace StockTV.ViewModel
{
    public class ZielPageViewModel : INotifyPropertyChanged
    {

        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public void RaiseAllPropertysChanged()
        {
            foreach (var prop in this.GetType().GetProperties())
            {
                RaisePropertyChange(prop.Name);
            }
        }

        #endregion

        private sbyte _inputValue;
        private readonly ConcurrentStack<byte> MassenVorne;
        private readonly ConcurrentStack<byte> MassenHinten;
        private readonly ConcurrentStack<byte> Schüsse;
        private readonly ConcurrentStack<byte> Kombinieren;

        #region Public READONLY Properties to display in View

        /// <summary>
        /// InputValue to Display
        /// </summary>
        public string InputValueText
        {
            get
            {
                return _inputValue == -1 ? string.Empty : _inputValue.ToString();
            }
        }

        /// <summary>
        /// Label to Display for GesamtPunkteText
        /// </summary>
        public string GesamtText
        {
            get
            {
                return "Gesamt";
            }
        }

        /// <summary>
        /// GesamtPunkte to Display
        /// </summary>
        public string GesamtPunkteText
        {
            get { return (SummeVon(MassenVorne) + SummeVon(Schüsse) + SummeVon(MassenHinten) + SummeVon(Kombinieren)).ToString(); }
        }

        /// <summary>
        /// Anzahl Versuche to Display
        /// </summary>
        public string VersucheText
        {
            get
            {
                return $"{CountOfVersuche()}/24";
            }
        }

        /// <summary>
        /// AnzeigeText für die vier Einzelsummen
        /// </summary>
        public string VersuchsWerte
        {
            get
            {
                return $"{SummeVon(MassenVorne)} - {SummeVon(Schüsse)} - {SummeVon(MassenHinten)} - {SummeVon(Kombinieren)}";
            }
        }

        #endregion

        /// <summary>
        /// Settings
        /// </summary>
        public Settings Settings
        {
            get
            {
                return Settings.Instance;
            }
        }


        /// <summary>
        /// Default Konstruktur
        /// </summary>
        public ZielPageViewModel()
        {
            this._inputValue = -1;

            this.MassenVorne = new ConcurrentStack<byte>();
            this.MassenHinten = new ConcurrentStack<byte>();
            this.Schüsse = new ConcurrentStack<byte>();
            this.Kombinieren = new ConcurrentStack<byte>();
        }

        #region Private Functions

        /// <summary>
        /// Werte von der Tastatur der internen Variable _inputValue zuweisen
        /// </summary>
        /// <param name="value"></param>
        private void AddInput(sbyte value)
        {
            if (_inputValue < 0)
            {
                _inputValue = value;
            }
            else if ((_inputValue * 10) + value < Settings.Instance.GameSettings.PointsPerTurn)
            {
                _inputValue = Convert.ToSByte((_inputValue * 10) + value);
            }
            else
            {
                _inputValue = -1;
            }
        }


        /// <summary>
        /// Den Wert von _inputValue den zu den Versuchen schreiben
        /// </summary>
        private void AddValueToVersuche()
        {
            if (_inputValue == -1)
                return;

            if (MassenVorne.Count() < 6)
            {
                MassenVorne.Push(Convert.ToByte(_inputValue));
            }
            else if (Schüsse.Count() < 6)
            {
                Schüsse.Push(Convert.ToByte(_inputValue));
            }
            else if (MassenHinten.Count() < 6)
            {
                MassenHinten.Push(Convert.ToByte(_inputValue));
            }
            else if (Kombinieren.Count() < 6)
            {
                Kombinieren.Push(Convert.ToByte(_inputValue));
            }

            _inputValue = -1;
        }

        /// <summary>
        /// Anzahl aller Versuche die bereits hinterlegt sind
        /// </summary>
        /// <returns></returns>
        private int CountOfVersuche()
        {
            return MassenVorne.Count + Schüsse.Count + MassenHinten.Count + Kombinieren.Count;
        }

        private int SummeVon(ConcurrentStack<byte> stack)
        {
            int value = 0;
            foreach (var v in stack)
            {
                value += Convert.ToInt32(v);
            }
            return value;
        }

        /// <summary>
        /// Denn letzten Versuche wieder löschen
        /// </summary>
        private void DeleteLastValue()
        {
            if (Kombinieren.Count > 0)
            {
                Kombinieren.TryPop(out _);
            }
            else if (MassenHinten.Count > 0)
            {
                MassenHinten.TryPop(out _);
            }
            else if (Schüsse.Count > 0)
            {
                Schüsse.TryPop(out _);
            }
            else if (MassenVorne.Count > 0)
            {
                MassenVorne.TryPop(out _);
            }

            _inputValue = -1;
        }

        /// <summary>
        /// Alle Werte zurücksetzen
        /// </summary>
        private void Reset()
        {
            MassenVorne.Clear();
            MassenHinten.Clear();
            Schüsse.Clear();
            Kombinieren.Clear();

            _inputValue = -1;
        }

        /// <summary>
        /// Switch to SettingsPage if 3 Values with sum of 22 are inserted
        /// </summary>
        private void ShowSettingsPage()
        {
            if (MassenVorne.Count() == 3 &&
                SummeVon(MassenVorne) == 22)
            {
                Reset();
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Pages.SettingsPage));
            }
        }

        #endregion

        #region Public Function

        public void GetScanCode(uint ScanCode)
        {

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

            switch (ScanCode)
            {
                case 69:    // NumLock
                case 83:    // ,
                    break;

                case 28:    // Enter
                    ShowSettingsPage();
                    break;

                case 74:    // -                    --> BLAU
                    DeleteLastValue();
                    break;

                case 55:    // *                    --> GRÜN
                case 53:    // /                    --> ROT
                case 14:    // BackSpace
                    AddValueToVersuche();
                    break;

                case 78:    // +                    --> GELB
                    Reset();
                    break;


                #region Numbers 1 to 0

                case 79:
                    //MyWertung.InputText = "1";
                    AddInput(1);
                    break;
                case 80:
                    //MyWertung.InputText = "2";
                    AddInput(2);
                    break;
                case 81:
                    //MyWertung.InputText = "3";
                    AddInput(3);
                    break;
                case 75:
                    //MyWertung.InputText = "4";
                    AddInput(4);
                    break;
                case 76:
                    //MyWertung.InputText = "5";
                    AddInput(5);
                    break;
                case 77:
                    //MyWertung.InputText = "6";
                    AddInput(6);
                    break;
                case 71:
                    //MyWertung.InputText = "7";
                    AddInput(7);
                    break;
                case 72:
                    //MyWertung.InputText = "8";
                    AddInput(8);
                    break;
                case 73:
                    //MyWertung.InputText = "9";
                    AddInput(9);
                    break;
                case 82:
                    //MyWertung.InputText = "0";
                    AddInput(0);
                    break;

                    #endregion
            }

            RaiseAllPropertysChanged();

            // Send after each key press a network notification
            if (Settings.Instance.IsBroadcasting)
            {
                // NetworkService.SendData(Match.Serialize(true));
            }
        }

        #endregion


    }
}
