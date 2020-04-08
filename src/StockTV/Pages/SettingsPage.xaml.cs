using StockTV.Classes;
using StockTV.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StockTV.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            InputPane.GetForCurrentView().Showing +=
               (s, e) =>
               {
                   (s as InputPane).TryHide();
                   System.Diagnostics.Debug.WriteLine("InputPane TryHide");
               };

            //Send ScanCode from KeyDown to DataContext
            //Window.Current.CoreWindow.KeyDown +=
            // (s, e) => ((SettingsPageViewModel)DataContext).SettingsPage_KeyDown(s,e);
            this.KeyDown += SettingsPage_KeyDown;

            //Set focus to MainPage after Loaded
            this.Loaded += (s, e) => (s as SettingsPage).Focus(FocusState.Programmatic);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.KeyDown -= SettingsPage_KeyDown;
            base.OnNavigatingFrom(e);
        }

        private void SettingsPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            ((SettingsPageViewModel)DataContext).SettingsPage_KeyDown(e.KeyStatus.ScanCode);
        }

        




    }
}
