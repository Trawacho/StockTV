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
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyname));
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


        /// <summary>
        /// all different settings possible to activate for editing
        /// </summary>
        public enum ActiveSettings
        {
            ColorScheme = 0,
            ColorSchemeRightToLeft = 1,
            GameMous = 2,
            MaxPointsPerTurn = 3,
            MaxCountOfTurnsPerGame = 4,
            CourtNumber = 5,
            Networking = 6,
        }

        /// <summary>
        /// Actual Setting to edit
        /// </summary>
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
                case 78:    //+ (gelbe Taste)
                    ExitSettingPage();
                    break;
                case 72:    //8 (up)
                    GoToPreviousSettings();
                    break;
                case 80:    //2 (down)
                    GoToNextSetting();
                    break;
                case 75:    //4 (left)
                    DecreaseSetting();
                    break;
                case 77:    //6 (right)
                    IncreaseSetting();
                    break;
                default:
                    break;
            }

            RaiseAllPropertiesChanged();
        }

        private void ExitSettingPage()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Ziel)
                rootFrame.Navigate(typeof(ZielPage));
            else
                rootFrame.Navigate(typeof(MainPage));
            
        }

        public void GoToNextSetting()
        {
            if (ActiveSetting < Enum.GetValues(typeof(ActiveSettings)).Cast<ActiveSettings>().Max())
            {
                ActiveSetting += 1;
            }
        }

        public void GoToPreviousSettings()
        {

            if (ActiveSetting > Enum.GetValues(typeof(ActiveSettings)).Cast<ActiveSettings>().Min())
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
                case ActiveSettings.ColorSchemeRightToLeft:
                    Settings.Instance.ColorScheme.RightToLeftUp();
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
                case ActiveSettings.CourtNumber:
                    Settings.Instance.CourtNumberChange();
                    break;
                case ActiveSettings.Networking:
                    Settings.Instance.IsBroadcastingChange();
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
                case ActiveSettings.ColorSchemeRightToLeft:
                    Settings.Instance.ColorScheme.RightToLeftDown();
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
                case ActiveSettings.CourtNumber:
                    Settings.Instance.CourtNumberChange(false);
                    break;
                case ActiveSettings.Networking:
                    Settings.Instance.IsBroadcastingChange();
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

        public bool IsColorSchemeRightToLeftActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.ColorSchemeRightToLeft;
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

        public bool IsCourtNumberActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.CourtNumber;
            }
        }

        public bool IsNetworkingActive
        {
            get
            {
                return ActiveSetting == ActiveSettings.Networking;
            }
        }

        public string ColorSchemeValue
        {
            get
            {
                return Settings.Instance.ColorScheme.Scheme.ToString();
            }
        }

        public string RightToLeftValue
        {
            get
            {
                return Settings.Instance.ColorScheme.RightToLeft
                    ? "Links"
                    : "Rechts";
            }
        }

        public string GameModusValue
        {
            get
            {
                return Settings.Instance.GameSettings.GameModus.ToString();
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

        public string CourtNumberValue
        {
            get
            {
                return Settings.Instance.CourtNumber.ToString();
            }
        }

        public string NetworkingValue
        {
            get
            {
                return Settings.Instance.IsBroadcasting ? "On" : "Off";
            }
        }

        public string NetworkingDescription
        {
            get
            {
                return Settings.Instance.IsBroadcasting
                    ? $"IP: {Settings.Instance.IPAddress} --> {Settings.Instance.BroadcastAddress}:{Settings.Instance.BroadcastPort}"
                    : "On / Off";
            }
        }

        #endregion
    }
}
