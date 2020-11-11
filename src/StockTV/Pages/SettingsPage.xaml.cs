using StockTV.ViewModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
               (s, e) => (s as InputPane).TryHide();

            //Set focus to MainPage after Loaded
            this.Loaded += (s, e) => (s as SettingsPage).Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Register Event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.CoreWindow.KeyDown += SettingsPage_KeyDown;
        }


        /// <summary>
        /// Remove Event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= SettingsPage_KeyDown;
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Send ScanCode to DataContext
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPage_KeyDown(object sender, KeyEventArgs e)
        {
            ((SettingsPageViewModel)DataContext).SettingsPage_KeyDown(e.KeyStatus.ScanCode);
        }






    }
}
