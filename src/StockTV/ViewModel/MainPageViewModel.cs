﻿using NetMQ;
using StockTV.Classes;
using StockTV.Classes.NetMQUtil;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace StockTV.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// UWP ViewModel for MainPage
        /// </summary>

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


        #region private Fields

        private sbyte _inputValue;

        private readonly Match Match;

        private byte settingsCounter;

        private DispatcherTimer timer;

        #endregion


        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MainPageViewModel()
        {
            Match = new Match();
            Match.TurnsChanged += Match_TurnsChanged;
            _inputValue = -1;
           
            RespServer.RespServerDataReceived += RespServer_RespServerDataReceived;
        }



        private void Match_TurnsChanged(object sender, EventArgs e)
        {
            Match m = sender as Match;
            var data = m.Serialize(false);
            Debug.WriteLine($"{m.Games.First().Turns.Sum(t => t.PointsLeft)} : {m.Games.First().Turns.Sum(t => t.PointsRight)}");
            Settings.Instance.SendGameResults(data);
        }

        #endregion


        private void RespServer_RespServerDataReceived(NetMQ.NetMQSocket socket, MqServerDataReceivedEventArgs e)
        {
            if (!((Window.Current.Content as Frame).Content is Pages.MainPage)) return;

            if (e.IsGameModus && e.GameModus == GameSettings.GameModis.Ziel)
            {
                _ = socket.TrySignalOK();
                RespServer.RespServerDataReceived -= RespServer_RespServerDataReceived;
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Pages.ZielPage));
            }

            if (e.IsGameModus && e.GameModus != GameSettings.GameModis.Ziel)
            {
                Settings.Instance.GameSettings.SetModus(e.GameModus);
            }

            if (e.IsPointsPerTurn)
            {
                Settings.Instance.GameSettings.PointsPerTurn = e.PointsPerTurn;
            }

            if (e.IsTurnsPerGame)
            {
                Settings.Instance.GameSettings.TurnsPerGame = e.TurnsPerGame;
            }

            if (e.IsColorScheme)
            {
                Settings.Instance.ColorScheme.Scheme = e.ColorScheme;
            }

            if (e.IsReset)
            {
                if (Settings.Instance.GameSettings.GameModus != GameSettings.GameModis.Ziel)
                    this.Match.Reset(true);
            }

            if (e.IsSetBegegnungen)
            {
                Match.Begegnungen.Clear();
                foreach (var item in e.Begegnungen)
                {
                    Match.Begegnungen.Add(item);
                }
            }

            if (e.IsGetResult)
            {
                _ = socket.TrySendFrame(Match.Serialize(false), false);
            }

            if (socket.HasOut)
            {
                _ = socket.TrySignalOK();
            }

            RaiseAllPropertysChanged();
        }


       



        #region Public Function

        public void GetScanCode(uint ScanCode)
        {
            //Settings
            if (_inputValue == 0 && ScanCode == 28)
            {
                settingsCounter++;
            }
            else
            {
                settingsCounter = 0;
            }

            //Debouncing 
            if (!(ScanCode == 74 && _inputValue == 0))
            {
                if (!Debounce.IsDebounceOk(this, ScanCode))
                {
                    return;
                }
            }


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
             * ScanCode: 53 /                   --> ROT
             * ScanCode: 55 *                   --> GRÜN
             * ScanCode: 74 -                   --> BLAU
             * ScanCode: 78 +                   --> GELB
             * ScanCode: 28 Enter
             * ScanCode: 83 ,
             * ScanCode: 14 BackSpace           --> ROT
             *
             */

            switch (ScanCode)
            {
                case 69:    // NumLock
                case 83:    // ,
                    break;

                case 28:    // Enter
                    ShowSettingsPage();
                    break;

                case 55:    // *                    --> GRÜN
                    AddToGreen();
                    break;

                case 74:    // -                    --> BLAU
                    DeleteLastTurn();
                    break;

                case 53:    // /                    --> ROT
                case 14:    // BackSpace
                    AddToRed();
                    break;

                case 78:    // +                    --> GELB
                    Reset();
                    break;


                #region Numbers 1 to 0

                case 79:
                    //MyWertung.InputText = "1";
                    AddInput(1);
                    break;
                case 80:
                    //MyWertung.InputText = "2";
                    AddInput(2);
                    break;
                case 81:
                    //MyWertung.InputText = "3";
                    AddInput(3);
                    break;
                case 75:
                    //MyWertung.InputText = "4";
                    AddInput(4);
                    break;
                case 76:
                    //MyWertung.InputText = "5";
                    AddInput(5);
                    break;
                case 77:
                    //MyWertung.InputText = "6";
                    AddInput(6);
                    break;
                case 71:
                    //MyWertung.InputText = "7";
                    AddInput(7);
                    break;
                case 72:
                    //MyWertung.InputText = "8";
                    AddInput(8);
                    break;
                case 73:
                    //MyWertung.InputText = "9";
                    AddInput(9);
                    break;
                case 82:
                    //MyWertung.InputText = "0";
                    AddInput(0);
                    break;

                    #endregion
            }

            RaiseAllPropertysChanged();

            // Send after each key press a network notification
            if (Settings.Instance.IsBroadcasting)
            {
                BroadcastService.SendData(Match.Serialize(true));
            }
        }

        #endregion


        #region Public READONLY Properties

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

        /// <summary>
        /// HeaderText
        /// </summary>
        public string HeaderText
        {
            get
            {
                if (Match.CurrentGame.GameNumber == 1 &&
                    Match.CurrentGame.Turns.Count == 0)
                {
                    return $"Bahn: {Settings.Instance.CourtNumber}";
                }

                switch (Settings.Instance.GameSettings.GameModus)
                {
                    case GameSettings.GameModis.Training:
                        return $"Bahn: {Settings.Instance.CourtNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
                    case GameSettings.GameModis.BestOf:
                        return $"Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
                    case GameSettings.GameModis.Turnier:
                        return $"Bahn: {Settings.Instance.CourtNumber}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
                    default:
                        return "unknown Status";
                }
            }
        }

        /// <summary>
        /// Sum of the LEFT Points
        /// </summary>
        public string LeftPointsSum
        {
            get
            {
                return Match.CurrentGame.Turns.Sum(t => t.PointsLeft).ToString();
            }
        }

        /// <summary>
        /// Sum of the Left MatchPoints
        /// </summary>
        public string LeftMatchPoints
        {
            get
            {
                return Match.MatchPointsLeft.ToString();
            }
        }

        /// <summary>
        /// Sum of the RIGHT Points
        /// </summary>
        public string RightPointsSum
        {
            get
            {
                return Match.CurrentGame.Turns.Sum(t => t.PointsRight).ToString();
            }
        }

        /// <summary>
        /// Sum of the Right MatchPoints
        /// </summary>
        public string RightMatchPoints
        {
            get
            {
                return Match.MatchPointsRight.ToString();
            }
        }

        /// <summary>
        /// Turn-Details of the Left 
        /// </summary>
        public string LeftPoints
        {
            get
            {
                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsLeft}";
                }

                if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier &&
                    Match.Begegnungen.Count > 0 &&
                    string.IsNullOrEmpty(temp))
                {
                    temp = Match.Begegnungen
                        .Where(b => b.Spielnummer == Match.CurrentGame.GameNumber)
                        .FirstOrDefault()?.TeamNameLeft(Settings.ColorScheme.RightToLeft) ?? string.Empty;
                }

                return temp;
            }
        }

        /// <summary>
        /// Turn-Details of the Right
        /// </summary>
        public string RightPoints
        {
            get
            {
                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsRight}";
                }

                if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier &&
                    Match.Begegnungen.Count > 0 &&
                    string.IsNullOrEmpty(temp))
                {
                    temp = Match.Begegnungen
                        .Where(b => b.Spielnummer == Match.CurrentGame.GameNumber)
                        .FirstOrDefault()?.TeamNameRight(Settings.ColorScheme.RightToLeft) ?? string.Empty;
                }

                return temp;
            }
        }

        /// <summary>
        /// InputValue to Display
        /// </summary>
        public string InputValue
        {
            get
            {
                return _inputValue == -1 ? string.Empty : _inputValue.ToString();
            }
        }

        #endregion


        #region Private Functions

        private void Reset(bool force = false)
        {
            Match.Reset(force);
            _inputValue = -1;
        }

        private void DeleteLastTurn()
        {
            if (_inputValue > 0)
            {
                _inputValue = -1;
                return;
            }

            Match.DeleteLastTurn();
        }

        private void AddToRed()
        {
            if (_inputValue == -1)
                return;

            var turn = new Turn();

            if (!Settings.Instance.ColorScheme.RightToLeft)
            {
                turn.PointsRight = Convert.ToByte(_inputValue);
            }
            else
            {
                turn.PointsLeft = Convert.ToByte(_inputValue);
            }

            this.Match.AddTurn(turn);

            _inputValue = -1;
        }

        private void AddToGreen()
        {
            if (_inputValue == -1)
                return;

            var turn = new Turn();

            if (Settings.Instance.ColorScheme.RightToLeft)
            {
                turn.PointsRight = Convert.ToByte(_inputValue);
            }
            else
            {
                turn.PointsLeft = Convert.ToByte(_inputValue);
            }

            this.Match.AddTurn(turn);


            _inputValue = -1;
        }

        private void AddInput(sbyte value)
        {
            if (_inputValue < 0)
            {
                _inputValue = value;
            }
            else if ((_inputValue * 10) + value < Settings.Instance.GameSettings.PointsPerTurn)
            {
                _inputValue = Convert.ToSByte((_inputValue * 10) + value);
            }
            else
            {
                _inputValue = value;
            }
        }

        private void ShowSettingsPage()
        {
            if (settingsCounter >= 5)
            {
                settingsCounter = 0;
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Pages.SettingsPage));
            }
        }

        #endregion


        public bool ShowBegegnung
        {
            get
            {
                return true;
            }
        }

    }
}
