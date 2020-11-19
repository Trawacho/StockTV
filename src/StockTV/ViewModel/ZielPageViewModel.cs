﻿using StockTV.Classes;
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
        private readonly Zielbewerb _zielbewerb;
        private byte settingsCounter;

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
            this._zielbewerb = new Zielbewerb();
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
                _inputValue = -1;
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

        /// <summary>
        /// Switch to SettingsPage
        /// </summary>
        private void ShowSettingsPage()
        {
            if (settingsCounter >= 5)
            {
                settingsCounter = 0;
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Pages.SettingsPage));
            }
        }

        #endregion

        #region Public Function

        public void GetScanCode(uint ScanCode)
        {
            //Settings
            if (_inputValue == 0 && ScanCode == 28)
            {
                settingsCounter++;
            }
            else
            {
                settingsCounter = 0;
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
                NetworkService.SendData(_zielbewerb.Serialize(true));
            }
        }

        #endregion


    }
}
