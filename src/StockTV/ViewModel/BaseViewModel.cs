using NetMQ;
using StockTV.Classes;
using StockTV.Classes.NetMQUtil;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static StockTV.Classes.GameSettings;

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

        internal abstract void SwitchToOtherPage(GameModis gameModus);
        internal abstract void SetBegegnungen(IEnumerable<Begegnung> begegnungen);
        internal abstract void SetMatchReset();
        internal abstract byte[] GetSerializedResult();

        private byte _settingsCounter;
        internal void SettingsCounterIncrease() => _settingsCounter++;
        internal void SettingsCounterReset() => _settingsCounter = 0;

        /// <summary>
        /// Settings
        /// </summary>
        public Settings Settings
        {
            get
            {
                return Settings.Instance;
            }
        }

        private protected void ShowSettingsPage()
        {
            if (_settingsCounter < 5) return;

            _settingsCounter = 0;
            RespServer.RespServerDataReceived -= RespServer_RespServerDataReceived;
            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Pages.SettingsPage));
        }

        private protected void RespServer_RespServerDataReceived(MqServerDataReceivedEventArgs e)
        {
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher
                .RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                   // if (!((Window.Current.Content as Frame).Content is Pages.MainPage)) return;

                    if (e.IsGameModus)
                    {
                        Settings.Instance.GameSettings.SetModus(e.GameModus);
                        SwitchToOtherPage(e.GameModus);
                    }

                    if (e.IsPointsPerTurn)
                    {
                        Settings.Instance.GameSettings.PointsPerTurn = e.PointsPerTurn;
                    }

                    if (e.IsBahnNummer)
                    {
                        Settings.Instance.CourtNumber = e.BahnNummer;
                    }

                    if (e.IsGroupNumber)
                    {
                        Settings.Instance.Spielgruppe = e.GroupNumber;
                    }

                    if (e.IsTurnsPerGame)
                    {
                        Settings.Instance.GameSettings.TurnsPerGame = e.TurnsPerGame;
                    }

                    if (e.IsColorModus)
                    {
                        Settings.Instance.ColorScheme.ColorModus = e.ColorModus;
                    }

                    if (e.IsNextBahn)
                    {
                        Settings.Instance.ColorScheme.NextBahnModus = e.NextBahn;
                    }

                    if (e.IsReset)
                    {
                        SetMatchReset();
                    }

                    if (e.IsSetBegegnungen)
                    {
                        SetBegegnungen(e.Begegnungen);
                    }

                    if (e.IsGetResult)
                    {
                        NetMQMessage back = new NetMQMessage(2);
                        back.Append("Result");
                        back.Append(GetSerializedResult());
                        RespServer.AddOutbound(back);
                    }

                    if (e.IsGetSettings)
                    {
                        NetMQMessage back = new NetMQMessage(2);
                        back.Append("Settings");
                        back.Append(Settings.Instance.ToString());
                        RespServer.AddOutbound(back);
                    }

                    RaiseAllPropertysChanged();
                });
        }
    }
}
