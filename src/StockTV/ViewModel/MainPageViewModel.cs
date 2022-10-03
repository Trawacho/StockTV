using StockTV.Classes;
using System;
using System.Linq;
using Windows.UI.Xaml;

namespace StockTV.ViewModel
{
    /// <summary>
    /// UWP ViewModel for MainPAge
    /// </summary>
    public class MainPageViewModel : BaseViewModel
    {
        #region BaseClass Functions

        internal override byte[] GetSerializedResult()
        {
            return Settings.MessageVersion == 0
                ? Match.Serialize()
                : Match.SerializeJson();
        }

        internal override void SetMatchReset()
        {
            this.Match.Reset(true);
        }

        internal override void SetTeamNames(string begegnungen)
        {
            Match.Begegnungen.Clear();

            var parts = begegnungen.TrimEnd(';').Split(';');
            foreach (var b in parts)
            {
                var c = b.Split(':');
                if (byte.TryParse(c[0], out byte _spielNummer))
                {
                    Match.Begegnungen.Add(new Begegnung(_spielNummer, c[1], c[2]));
                }
            }
        }

        internal override void SetSettings(byte[] settings)
        {
            Settings.SetSettings(settings);

            if (Settings.GameSettings.GameModus == GameSettings.GameModis.Ziel)
            {
                Match.TurnsChanged -= Match_TurnsChanged;
                base.NavigateTo(typeof(Pages.ZielPage));
            }

        }


        #endregion

        #region private Fields

        private sbyte _inputValue;
        private readonly Match Match;

        #endregion

        #region Public READONLY Properties


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
                    if (Settings.Instance.SpielgruppeLetter == string.Empty)
                        return $"Bahn: {Settings.Instance.CourtNumber}";
                    else
                        return $"Bahn: {Settings.Instance.SpielgruppeLetter}-{Settings.Instance.CourtNumber}";
                }

                switch (Settings.Instance.GameSettings.GameModus)
                {
                    case GameSettings.GameModis.Training:
                        return $"Bahn: {Settings.Instance.CourtNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
                    case GameSettings.GameModis.BestOf:
                        return $"Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
                    case GameSettings.GameModis.Turnier:
                        if (Settings.Instance.SpielgruppeLetter == string.Empty)
                            return $"Bahn: {Settings.Instance.CourtNumber}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
                        else
                            return $"Bahn: {Settings.Instance.SpielgruppeLetter}-{Settings.Instance.CourtNumber}   Spiel: {Match.CurrentGame.GameNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
                    default:
                        return "unknown Status";
                }
            }
        }

        /// <summary>
        /// Sum of the LEFT Points
        /// </summary>
        public string LeftPointsSum => Match.CurrentGame.Turns.Sum(t => t.PointsLeft).ToString();

        /// <summary>
        /// Sum of the Left MatchPoints
        /// </summary>
        public string LeftMatchPoints => Match.MatchPointsLeft.ToString();

        /// <summary>
        /// Sum of the RIGHT Points
        /// </summary>
        public string RightPointsSum => Match.CurrentGame.Turns.Sum(t => t.PointsRight).ToString();

        /// <summary>
        /// Sum of the Right MatchPoints
        /// </summary>
        public string RightMatchPoints => Match.MatchPointsRight.ToString();

        /// <summary>
        /// Turn-Details of the Left 
        /// </summary>
        public string LeftPoints
        {
            get
            {
                if (Settings.GameSettings.GameModus == GameSettings.GameModis.BestOf
                    && Match.CurrentGame.Turns.Count == 0
                    && Match.CurrentGame.GameNumber > 1)
                    return Match.Games.Sum(s => s.Turns.Sum(t => t.PointsLeft)).ToString();

                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsLeft}";
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
                if (Settings.GameSettings.GameModus == GameSettings.GameModis.BestOf
                    && Match.CurrentGame.Turns.Count == 0
                    && Match.CurrentGame.GameNumber > 1)
                    return Match.Games.Sum(s => s.Turns.Sum(t => t.PointsRight)).ToString();

                string temp = string.Empty;
                foreach (var item in Match.CurrentGame.Turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += $"{item.PointsRight}";
                }

                return temp;
            }
        }

        #region TeamNames

        public bool IsTeamNameAvailable => !string.IsNullOrEmpty(LeftTeamName);

        public string LeftTeamName
        {
            get
            {
                if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier &&
                    Match.Begegnungen.Count > 0)
                {
                    return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
                                            ?.TeamNameLeft(Settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left)
                                            ?? string.Empty;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string RightTeamName
        {
            get
            {
                if (Settings.Instance.GameSettings.GameModus == GameSettings.GameModis.Turnier &&
                    Match.Begegnungen.Count > 0)
                {
                    return Match.Begegnungen.FirstOrDefault(b => b.Spielnummer == Match.CurrentGame.GameNumber)
                                            ?.TeamNameRight(Settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left)
                                            ?? string.Empty;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        #endregion

        public GridLength MidColumnLength
        {
            get => new GridLength(Settings.MidColumnLength, GridUnitType.Star);
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

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MainPageViewModel() : base()
        {
            _inputValue = -1;

            Match = new Match();
            Match.TurnsChanged += Match_TurnsChanged;
        }

        #endregion

        #region Public Function

        public void GetScanCode(uint ScanCode)
        {
            //Settings Or Marekting SpecialCounter
            if ((_inputValue == 0 || _inputValue == 10)
                && ScanCode == 28)
            {
                base.SpecialCounterIncrease();
            }
            else
            {
                base.SpecialCounterReset();
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
                    ShowSpecialPage(_inputValue);
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
            if (Settings.IsBroadcasting)
            {
                if (Settings.MessageVersion == 0)
                    BroadcastService.SendData(Match.Serialize());
                else if (Settings.MessageVersion == 1)
                    BroadcastService.SendData(Match.SerializeJson());
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
            if (_inputValue != 0)
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

            if (Settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Right)
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

            if (Settings.ColorScheme.NextBahnModus == ColorScheme.NextBahnModis.Left)
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
                if (value <= Settings.Instance.GameSettings.PointsPerTurn)
                    _inputValue = value;
            }
            else if ((_inputValue * 10) + value <= Settings.GameSettings.PointsPerTurn)
            {
                _inputValue = Convert.ToSByte((_inputValue * 10) + value);
            }
            else
            {
                if (value <= Settings.Instance.GameSettings.PointsPerTurn)
                    _inputValue = value;
                else
                    _inputValue = -1;
            }
        }

        /// <summary>
        /// Send match-result via Pub-Server after a change in the Match values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Match_TurnsChanged(object sender, EventArgs e)
        {
            if (sender is Match m)
            {
                if (Settings.Instance.MessageVersion == 0)
                {
                    Settings.Instance.PublishGameResult(m.Serialize());
                }
                else if (Settings.Instance.MessageVersion == 1)
                {
                    Settings.Instance.PublishGameResult(m.SerializeJson());
                }
            }
        }

        #endregion

    }
}
