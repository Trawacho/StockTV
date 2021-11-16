using StockTV.ViewModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StockTV.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MarketingPage : Page
    {
        public MarketingPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            base.OnNavigatingFrom(e);
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            ((MarketingPageViewModel)DataContext).GetScanCode(args.KeyStatus.ScanCode);
        }
    }
}
