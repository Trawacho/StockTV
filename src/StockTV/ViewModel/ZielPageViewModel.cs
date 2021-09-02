using StockTV.Classes;
using StockTV.Classes.NetMQUtil;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static StockTV.Classes.GameSettings;

namespace StockTV.ViewModel
{
    public class ZielPageViewModel : BaseViewModel
    {
        /// <summary>
        /// UWP ViewModel for ZielPage
        /// </summary>

        #region BaseClass Functions
        internal override byte[] GetSerializedResult()
        {
            return _zielbewerb.Serialize(false);
        }
        internal override void SetMatchReset()
        {
            _zielbewerb.Reset();
        }
        internal override void SetBegegnungen(IEnumerable<Begegnung> begegnungen)
        {
            return;
        }

        internal override void SwitchToOtherPage(GameModis gameModus)
        {
            if (gameModus == GameModis.Ziel) return;
            RespServer.RespServerDataReceived -= RespServer_RespServerDataReceived;
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Pages.MainPage));
        }
        #endregion

        #region Private Fields

        private sbyte _inputValue;
        private readonly Zielbewerb _zielbewerb;
        private readonly DispatcherTimer _isInvalidTimer = new DispatcherTimer();
        
        #endregion

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

        public string LastValue
        {
            get
            {
                return _inputValue == -1
                        ? _zielbewerb.CountOfVersuche() > 0
                            ? _zielbewerb.LastValue().ToString()
                            : string.Empty
                        : string.Empty;
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
            get { return _zielbewerb.GesamtSumme.ToString(); }
        }

        /// <summary>
        /// Anzahl Versuche to Display
        /// </summary>
        public string VersucheText
        {
            get
            {
                return $"{_zielbewerb.CountOfVersuche()}/24";
            }
        }

        /// <summary>
        /// AnzeigeText für die vier Einzelsummen
        /// </summary>
        public string VersuchsWerte
        {
            get
            {
                return $"{_zielbewerb.MassenVorneSumme} - {_zielbewerb.SchüsseSumme} - {_zielbewerb.MassenHintenSumme} - {_zielbewerb.KombinierenSumme}";
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default Konstruktur
        /// </summary>
        public ZielPageViewModel() : base()
        {
            _inputValue = -1;

            _zielbewerb = new Zielbewerb();
            _zielbewerb.ValuesChanged += Bewerb_ValuesChanged;

            _isInvalidTimer.Tick += IsInvalidTimer_Tick;
            _isInvalidTimer.Interval = TimeSpan.FromMilliseconds(500);
        }



        #endregion


        private void Bewerb_ValuesChanged(object sender, EventArgs e)
        {
            Zielbewerb m = sender as Zielbewerb;
            Settings.Instance.SendGameResults(m.Serialize(false));
        }


        #region Public Function

        public void GetScanCode(uint ScanCode)
        {
            //Settings
            if (_inputValue == 0 && ScanCode == 28)
            {
                SettingsCounterIncrease();
            }
            else
            {
                SettingsCounterReset();
            }


            //Debouncing
            if (!(ScanCode == 74 && _inputValue == 0))
            {
                if (!Debounce.IsDebounceOk(this, ScanCode))
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
                    AddInput(1);
                    break;
                case 80:
                    AddInput(2);
                    break;
                case 81:
                    AddInput(3);
                    break;
                case 75:
                    AddInput(4);
                    break;
                case 76:
                    AddInput(5);
                    break;
                case 77:
                    AddInput(6);
                    break;
                case 71:
                    AddInput(7);
                    break;
                case 72:
                    AddInput(8);
                    break;
                case 73:
                    AddInput(9);
                    break;
                case 82:
                    AddInput(0);
                    break;

                    #endregion
            }

            RaiseAllPropertysChanged();

            // Send after each key press a network notification
            if (Settings.Instance.IsBroadcasting)
            {
                BroadcastService.SendData(_zielbewerb.Serialize(true));
            }
        }

        #endregion

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
                _inputValue = value;
            }
        }

        /// <summary>
        /// Den Wert von _inputValue den zu den Versuchen schreiben
        /// </summary>
        private void AddValueToVersuche()
        {
            if (_inputValue == -1)
                return;

            if (_zielbewerb.AddValueToVersuche(_inputValue))
            {
                _inputValue = -1;
            }
            else
            {
                _isInvalidTimer.Start();
                IsInvalidTimer_Tick(null, null);
            }
        }

        /// <summary>
        /// Denn letzten Versuche wieder löschen
        /// </summary>
        private void DeleteLastValue()
        {
            if (_inputValue > 0)
            {
                _inputValue = -1;
                return;
            }

            _zielbewerb.DeleteLastValue();
        }

        /// <summary>
        /// Alle Werte zurücksetzen
        /// </summary>
        private void Reset()
        {
            if (_zielbewerb.CountOfVersuche() < 24)
                return;

            _zielbewerb.Reset();
            _inputValue = -1;
        }

        #endregion



        #region IsInvalidInput Implementation

        private bool isInvalidInput;
        /// <summary>
        /// TRUE, wenn auf der Anzeige "ungültig" angezeigt werden soll
        /// </summary>
        public bool IsInvalidInput
        {
            get
            {
                return isInvalidInput;
            }
            set
            {
                if (isInvalidInput != value)
                {
                    isInvalidInput = value;
                    RaisePropertyChange();
                }
            }
        }


        /// <summary>
        /// Setzt <see cref="IsInvalidInput"/> auf TRUE oder auf FALSE und beendet dem Timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsInvalidTimer_Tick(object sender, object e)
        {
            if (!IsInvalidInput)
            {
                IsInvalidInput = true;
                return;
            }
            IsInvalidInput = false;
            _isInvalidTimer.Stop();
        }

        #endregion

    }
}
