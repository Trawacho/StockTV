using StockTV.ViewModel;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StockTV
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            //Try Hide inputPane if showing
            InputPane.GetForCurrentView().Showing +=
                (s, e) =>
                {
                    (s as InputPane).TryHide();
                    System.Diagnostics.Debug.WriteLine("InputPane TryHide");
                };

            //Send ScanCode from KeyDown to DataContext
            Window.Current.CoreWindow.KeyDown +=
                (s, e) => ((MainPageViewModel)DataContext).Do(e.KeyStatus.ScanCode);

            //Set focus to MainPage after Loaded
            this.Loaded += (s, e) => (s as MainPage).Focus(FocusState.Programmatic);
        }
    }
}
