using StockTV.Classes;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace StockTV.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// UWP ViewModel
        /// </summary>

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


        #region private Fields

        private int _inputValue;

        private Match Match;

        #endregion


        #region Constructor

        public MainPageViewModel()
        {
            Match = new Match();
            _inputValue = -1;
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
             * ScanCode: 53 /
             * ScanCode: 55 *
             * ScanCode: 74 -
             * ScanCode: 78 +
             * ScanCode: 28 Enter
             * ScanCode: 83 ,
             * ScanCode: 14 BackSpace
             *
             */

            switch (ScanCode)
            {
                case 69:    // NumLock

                case 83:    // ,
                case 28:    // Enter
                    ShowSettingsPage();
                    break;

                case 55:    // *
                    AddRight();
                    break;

                case 74:    // -
                    DeleteLastTurn();
                    break;

                case 53:    // /
                case 14:    // BackSpace
                    AddLeft();
                    break;

                case 78:    // +
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

        }

        #endregion


        #region Public READONLY Properties

        public Settings Settings
        {
            get
            {
                return Settings.Instance;
            }
        }


        public string HeaderText
        {
            get
            {
                if (Match.CurrentGame.GameNumber == 1 && Match.CurrentGame.CountOfTurns == 0)
                {
                    return "Spielstand";
                }
                else if (Settings.Instance.GameSettings.Modus == GameSettings.Modis.Normal)
                {
                    return $"Spielstand nach {Match.CurrentGame.CountOfTurns} Kehren";
                }
                else if (Settings.Instance.GameSettings.Modus == GameSettings.Modis.Lkms)
                {
                    return $"Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.CountOfTurns}";
                }
                return "unknown Status";
            }
        }

        public string LeftPointsSum
        {
            get
            {
                return Match.CurrentGame.Turns.Sum(t => t.PointsLeft).ToString();
            }
        }

        public string Left2ndPoints
        {
            get
            {
                return Match.MatchPointsLeft.ToString();
            }
        }

        public string RightPointsSum
        {
            get
            {
                return Match.CurrentGame.Turns.Sum(t => t.PointsRight).ToString();
            }
        }

        public string Right2ndPoints
        {
            get
            {
                return Match.MatchPointsRight.ToString();
            }
        }

        public string LeftPoints
        {
            get
            {
                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsLeft}";
                }
                return temp;
            }
        }

        public string RightPoints
        {
            get
            {
                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsRight}";
                }
                return temp;
            }
        }

        public string InputValue
        {
            get
            {
                return  _inputValue == -1 ? string.Empty : _inputValue.ToString();
            }
        }

        #endregion


        #region Private Functions

        private void Reset()
        {
            Match.Reset();
            _inputValue = -1;
        }

        private void DeleteLastTurn()
        {
            Match.DeleteLastTurn();
            _inputValue = -1;
        }

        private void AddLeft()
        {
            if (_inputValue == -1)
                return;

            this.Match.AddTurn(new Turn()
            {
                PointsLeft = _inputValue
            });

            _inputValue = -1;
        }

        private void AddRight()
        {
            if (_inputValue == -1)
                return;


            this.Match.AddTurn(new Turn()
            {
                PointsRight = _inputValue
            });

            _inputValue = -1;
        }

        private void AddInput(int value)
        {
            _inputValue = _inputValue < 0 ? value : (_inputValue * 10) + value;

            if (_inputValue > Settings.Instance.GameSettings.MaxPointsPerTurn)
            {
                _inputValue = -1;
            }
        }

        private void ShowSettingsPage()
        {
            if (Match.CanSettingsShow)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Pages.SettingsPage));
            }
        }

        #endregion
       
    }
}
