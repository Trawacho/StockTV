using StockTV.Classes;
using StockTV.Pages;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace StockTV.ViewModel
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {

        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        private void RaiseAllPropertiesChanged()
        {
            foreach (var p in this.GetType().GetProperties())
            {
                RaisePropertyChange(p.Name);
            }
        }

        #endregion


        #region Constructor

        public SettingsPageViewModel()
        {
            ActiveSetting = ActiveSettings.ColorScheme;
        }

        #endregion


        public enum ActiveSettings
        {
            ColorScheme = 0,
            GameMous = 1,
            MaxPointsPerTurn = 2,
            MaxCountOfTurnsPerGame = 3,

        }


        private ActiveSettings ActiveSetting;
        

        #region Functions

        public void SettingsPage_KeyDown(uint ScanCode)
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
             */

            switch (ScanCode)
            {
                case 28:
                    Frame rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(MainPage));
                    break;
                case 72:
                    GoToPreviousSettings();
                    break;
                case 80:
                    GoToNextSetting();
                    break;
                case 75:
                    DecreaseSetting();
                    break;
                case 77:
                    IncreaseSetting();
                    break;
                default:
                    break;
            }

            RaiseAllPropertiesChanged();

        }


        public void GoToNextSetting()
        {
            if (ActiveSetting  < Enum.GetValues(typeof(ActiveSettings)).Cast<ActiveSettings>().Max())
            {
                ActiveSetting += 1;
            }
        }

        public void GoToPreviousSettings()
        {

            if (ActiveSetting  > Enum.GetValues(typeof(ActiveSettings)).Cast<ActiveSettings>().Min())
            {
                ActiveSetting -= 1;
            }
        }

        public void IncreaseSetting()
        {
            switch (ActiveSetting)
            {
                case ActiveSettings.ColorScheme:
                    Settings.Instance.ColorScheme.ColorSchemeUp();
                    break;
                case ActiveSettings.GameMous:
                    Settings.Instance.GameSettings.ModusChange();
                    break;
                case ActiveSettings.MaxCountOfTurnsPerGame:
                    Settings.Instance.GameSettings.TurnsPerGameChange();
                    break;
                case ActiveSettings.MaxPointsPerTurn:
                    Settings.Instance.GameSettings.PointsPerTurnChange();
                    break;
                default:
                    break;
            }
        }

        public void DecreaseSetting()
        {
            switch (ActiveSetting)
            {
                case ActiveSettings.ColorScheme:
                    Settings.Instance.ColorScheme.ColorSchemeDown();
                    break;
                case ActiveSettings.GameMous:
                    Settings.Instance.GameSettings.ModusChange(false);
                    break;
                case ActiveSettings.MaxCountOfTurnsPerGame:
                    Settings.Instance.GameSettings.TurnsPerGameChange(false);
                    break;
                case ActiveSettings.MaxPointsPerTurn:
                    Settings.Instance.GameSettings.PointsPerTurnChange(false);
                    break;
                default:
                    break;
            }

        }

        #endregion

        #region Properties


        public bool IsColorSchemeActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.ColorScheme;
            }
        }

        public bool IsGameModusActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.GameMous;
            }
        }

        public bool IsPointsPerTurnActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.MaxPointsPerTurn;
            }
        }

        public bool IsTurnsPerGameActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.MaxCountOfTurnsPerGame;

            }
        }



        public string ColorSchemeValue
        {
            get
            {
                return Settings.Instance.ColorScheme.Scheme.ToString();
            }
        }

        public string GameModusValue
        {
            get
            {
                return Settings.Instance.GameSettings.Modus.ToString();
            }
        }

        public string PointsPerTurnValue
        {
            get
            {
                return Settings.Instance.GameSettings.PointsPerTurn.ToString();
            }
        }

        public string TurnsPerGameValue
        {
            get
            {
                return Settings.Instance.GameSettings.TurnsPerGame.ToString();
            }
        }

       public string xVal
        {
            get
            {
                return string.Empty;
            }
            set { }
        }
        
        #endregion
    }
}
