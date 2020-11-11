using StockTV.ViewModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StockTV.Pages
{
    public sealed partial class ZielPage : Page
    {
        public ZielPage()
        {
            this.InitializeComponent();

            //Try Hide inputPane if showing
            InputPane.GetForCurrentView().Showing +=
                (s, e) => (s as InputPane).TryHide();

            //Set focus to MainPage after Loaded
            this.Loaded += (s, e) => (s as ZielPage).Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Register Event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.CoreWindow.KeyDown += ZielPage_KeyDown;
        }

        /// <summary>
        /// Remove Event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= ZielPage_KeyDown;
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Send ScanCode to DataContext
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZielPage_KeyDown(object sender, KeyEventArgs e)
        {
            ((ZielPageViewModel)DataContext).GetScanCode(e.KeyStatus.ScanCode);
        }
    }
}
