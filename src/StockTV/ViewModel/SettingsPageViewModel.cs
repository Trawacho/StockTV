﻿using StockTV.Classes;
using StockTV.Pages;
using System;
using System.ComponentModel;
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

        #endregion


        #region Constructor

        public SettingsPageViewModel()
        {

        }

        #endregion


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
                case 79:
                    SwitchColorScheme();
                    break;
                case 80:
                    SwitchGameModus();
                    break;
                default:
                    break;
            }

        }

        public void SwitchColorScheme()
        {
            Settings.Instance.ColorScheme.SwitchColorScheme();
            RaisePropertyChange(nameof(IsColorSchemeNormal));
        }

        public void SwitchGameModus()
        {
            if (Settings.Instance.GameSettings.Modus == GameSettings.Modis.Normal)
            {
                Settings.Instance.GameSettings.SetModus(GameSettings.Modis.Lkms);
            }
            else
            {
                Settings.Instance.GameSettings.SetModus(GameSettings.Modis.Normal);
            }
            RaisePropertyChange(nameof(IsGameModusNormal));
        }

        #endregion


        #region Properties

        public bool IsColorSchemeNormal
        {
            get
            {
                return Settings.Instance.ColorScheme.Scheme == ColorScheme.Schemes.Normal;
            }
        }
        
        public bool IsGameModusNormal
        {
            get
            {
                return Settings.Instance.GameSettings.Modus == GameSettings.Modis.Normal;
            }
        }
        
        #endregion
    }
}
