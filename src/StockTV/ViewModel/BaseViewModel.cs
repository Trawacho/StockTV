using NetMQ;
using StockTV.Classes;
using StockTV.Classes.NetMQUtil;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace StockTV.ViewModel
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public void RaiseAllPropertysChanged()
        {
            foreach (var prop in this.GetType().GetProperties())
            {
                RaisePropertyChange(prop.Name);
            }
        }

        #endregion
        public BaseViewModel()
        {
            RespServer.RespServerDataReceived += RespServer_RespServerDataReceived;
        }

        /// <summary>
        /// Settings from Singleton Instance
        /// </summary>
        public Settings Settings => Settings.Instance;


        private byte _specialCounter;
        internal protected void SpecialCounterIncrease() => _specialCounter++;
        internal protected void SpecialCounterReset() => _specialCounter = 0;


        /// <summary>
        /// If <see cref="_specialCounter"/> is eq to 5, Navigate to the SettingsPage or MarketingPage 
        /// </summary>
        /// <param name="inputValue"></param>
        private protected void ShowSpecialPage(sbyte inputValue)
        {
            if (_specialCounter < 5) return;
            _specialCounter = 0;

            if (inputValue == 0)
            {
                NavigateTo(typeof(Pages.SettingsPage));
            }
            else if (inputValue == 10)
            {
                NavigateTo(typeof(Pages.MarketingPage));
            }


        }



        internal abstract void SetSettings(byte[] settings);
        internal abstract void SetTeamNames(string begegnungen);
        internal abstract void SetMatchReset();
        internal abstract byte[] GetSerializedResult();
      

        /// <summary>
        /// Send NetMQ Message to sender with Settings
        /// </summary>
        /// <param name="senderFrame"></param>
        private protected void SendSettings(NetMQFrame senderFrame)
        {
            NetMQMessage back = new NetMQMessage(4);
            back.Append(senderFrame);
            back.AppendEmptyFrame();
            back.Append(MessageTopic.GetSettings.ToString());
            back.Append(Settings.GetSettings());
            RespServer.AddOutbound(back);
        }

        /// <summary>
        /// Send NetMQ Message to sender with the Result as Value part<br></br>
        /// </summary>
        /// <param name="senderFrame"></param>
        private protected void SendResult(NetMQFrame senderFrame)
        {
            NetMQMessage back = new NetMQMessage(4);
            back.Append(senderFrame);
            back.AppendEmptyFrame();
            back.Append(MessageTopic.GetResult.ToString());
            back.Append(GetSerializedResult());
            RespServer.AddOutbound(back);
        }

        /// <summary>
        /// Save Byte-Array to Settings<para></para>
        /// Creates a copy of value and creates a BitmapImage. This is saved in Settings
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fileName"></param>
        private protected void SetImage(byte[] value, string fileName)
        {
            var dataSource = new byte[value.Length];
            System.Array.Copy(value, dataSource, value.Length);

            using (var randomAccess = new InMemoryRandomAccessStream())
            using (var writer = new DataWriter(randomAccess.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(dataSource);
                writer.StoreAsync().GetResults();
                BitmapImage bm = new BitmapImage();
                bm.SetSource(randomAccess);

                Settings.MarketingImage = bm;
            }

            NavigateTo(typeof(Pages.MarketingPage), true);

        }

        /// <summary>
        /// The <see cref="Settings.MarketingImage"/> is set to null
        /// </summary>
        private protected void ClearImage()
        {
            Settings.MarketingImage = null;
        }

        private protected void GoToImage()
        {
            NavigateTo(typeof(Pages.MarketingPage));
        }


        /// <summary>
        /// Navigate to the page from given Type if other than the actual one<br></br>
        /// De-register from ResponseServer<para></para>
        /// if stayOnPage is set to TRUE, no change if still on MarketingPage
        /// </summary>
        /// <param name="pageTypeToNavigate"></param>
        internal void NavigateTo(System.Type pageTypeToNavigate, bool stayOnPage = false)
        {
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame.Content.GetType() != pageTypeToNavigate)
            {
                RespServer.RespServerDataReceived -= RespServer_RespServerDataReceived;
                rootFrame.Navigate(pageTypeToNavigate);
            }
            else if (!stayOnPage &&
                     rootFrame.Content.GetType() == typeof(Pages.MarketingPage) &&
                     typeof(Pages.MarketingPage) == pageTypeToNavigate)
            {
                if (Settings.GameSettings.GameModus == GameSettings.GameModis.Ziel)
                    NavigateTo(typeof(Pages.ZielPage));
                else
                    NavigateTo(typeof(Pages.MainPage));
            }
        }




        /// <summary>
        /// Depending on the args the appropiate function get called
        /// </summary>
        /// <param name="args"></param>
        private protected void RespServer_RespServerDataReceived(MqServerDataReceivedEventArgs args)
        {
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher
                .RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    switch (args.Topic)
                    {
                        case MessageTopic.SetResult:
                            break;
                        case MessageTopic.GetResult:
                            SendResult(args.SenderID);
                            break;
                        case MessageTopic.SetSettings:
                            SetSettings(args.Value);
                            break;
                        case MessageTopic.GetSettings:
                            SendSettings(args.SenderID);
                            break;
                        case MessageTopic.ResetResult:
                            SetMatchReset();
                            break;
                        case MessageTopic.SetTeamNames:
                            SetTeamNames(System.Text.Encoding.UTF8.GetString(args.Value));
                            break;
                        case MessageTopic.SetImage:
                            SetImage(
                                args.Value,
                                System.Text.Encoding.UTF8.GetString(args.GetAdditionals()));
                            break;
                        case MessageTopic.GoToImage:
                            GoToImage();
                            break;
                        case MessageTopic.ClearImage:
                            ClearImage();
                            break;
                        default:
                            break;
                    }

                    RaiseAllPropertysChanged();
                });
        }
    }
}
