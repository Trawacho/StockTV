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
        /// <summary>
        /// Constructor
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();

            InputPane.GetForCurrentView().Showing +=
               (s, e) =>
               {
                   (s as InputPane).TryHide();
                   System.Diagnostics.Debug.WriteLine("InputPane TryHide");
               };

            //Register Event
            this.KeyDown += SettingsPage_KeyDown;

            //Set focus to MainPage after Loaded
            this.Loaded += (s, e) => (s as SettingsPage).Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Remove Event from KeyDown before Page is unloaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.KeyDown -= SettingsPage_KeyDown;
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Send ScnaCode to DataContext
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            ((SettingsPageViewModel)DataContext).SettingsPage_KeyDown(e.KeyStatus.ScanCode);
        }

        




    }
}
