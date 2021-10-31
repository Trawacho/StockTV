using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Windows.Storage;
using StockTV.Classes.NetMQUtil;

namespace StockTV.Classes
{
    /// <summary>
    /// Singleton-Class for the App-Settings
    /// </summary>
    public sealed class Settings
    {
        private static readonly Lazy<Settings> settings = new Lazy<Settings>(() => new Settings());

        private void Load()
        {
            ColorScheme = ColorScheme.Load();
            GameSettings = GameSettings.Load();

            var localSettings = ApplicationData.Current.LocalSettings;

            var coursenr = localSettings.Values[nameof(CourtNumber)] as string;
            this.CourtNumber = byte.Parse(coursenr ?? "1");

            var groupnr = localSettings.Values[nameof(Spielgruppe)] as string;
            this.Spielgruppe = byte.Parse(groupnr ?? "0");

            var broadcasting = localSettings.Values[nameof(IsBroadcasting)] as string;
            this.IsBroadcasting = bool.Parse(broadcasting ?? "false");

            var midcolLen = localSettings.Values[nameof(MidColumnLength)] as string;
            this.MidColumnLength = byte.Parse(midcolLen ?? "10");

            Debug.WriteLine("Settings loaded...");
        }


        /// <summary>
        /// Instanz of the settings
        /// </summary>
        public static Settings Instance { get { return settings.Value; } }

        /// <summary>
        /// Defualt private constructor
        /// </summary>
        private Settings()
        {
            Load();
        }

        /// <summary>
        /// Holds the current ColorScheme
        /// </summary>
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Holds the current GameSettings
        /// </summary>
        public GameSettings GameSettings { get; set; }

        #region CourtNumber
        /// <summary>
        /// Changes the CourtNumber up or down
        /// </summary>
        /// <param name="up">true = increase, false = decrease</param>
        public void CourtNumberChange(bool up = true)
        {
            if (up)
            {
                CourtNumber++;
            }
            else
            {
                CourtNumber--;
            }
        }

        private byte courtNumber;
        /// <summary>
        /// Number of the Course
        /// </summary>
        public byte CourtNumber
        {
            get { return courtNumber; }
            set
            {

                if (courtNumber == value ||
                          value < 1 ||
                          value > 99)
                    return;

                courtNumber = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(CourtNumber)] = value.ToString();
            }
        }


        #endregion

        #region GroupNumber

        /// <summary>
        /// Changes the GroupNumber up or down
        /// </summary>
        /// <param name="up"></param>
        public void SpielgruppeChange(bool up = true)
        {
            if (up)
                Spielgruppe++;
            else
                Spielgruppe--;
        }

        private byte spielgruppe;
        /// <summary>
        /// Number of the group (CourtNumber Group)
        /// </summary>
        public byte Spielgruppe
        {
            internal get => spielgruppe;
            set
            {
                if (spielgruppe == value ||
                    value < 0 ||
                    value > 10)
                    return;

                spielgruppe = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(Spielgruppe)] = value.ToString();
            }
        }

        /// <summary>
        /// GroupNumber as Letter
        /// <br>0 =></br>
        /// <br> 1 => A</br>
        /// <br> 2 => B</br>
        /// <br> .</br>
        /// <br> .</br>
        /// <br> .</br>
        /// <br>10 => J</br>
        /// </summary>
        public string SpielgruppeLetter
        {
            get
            {
                switch (spielgruppe)
                {
                    case 1:
                        return "A";
                    case 2:
                        return "B";
                    case 3:
                        return "C";
                    case 4:
                        return "D";
                    case 5:
                        return "E";
                    case 6:
                        return "F";
                    case 7:
                        return "G";
                    case 8:
                        return "H";
                    case 9:
                        return "I";
                    case 10:
                        return "J";
                    case 0:
                    default:
                        return string.Empty;
                }
            }
        }

        #endregion

        #region Turns of a Game

        /// <summary>
        /// Save List of Turns to localSettings
        /// </summary>
        /// <param name="turns"></param>
        internal void SaveTurns(List<Turn> turns)
        {
            string turnString = "";
            foreach (var turn in turns)
            {
                turnString += $"{turn.PointsLeft}:{turn.PointsRight};";
            }
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Turns"] = turnString;
        }

        /// <summary>
        /// Load List of Turns from localSettings
        /// </summary>
        /// <returns></returns>
        internal List<Turn> LoadTurns()
        {
            var _turns = new List<Turn>();

            _turns.Clear();

            var localSettings = ApplicationData.Current.LocalSettings;
            if (!(localSettings.Values["Turns"] is string turnStringComplete))
                return _turns;

            var turnStrings = turnStringComplete?.Split(';');
            foreach (var turnString in turnStrings.Where(t => t.Contains(':')))
            {
                var x = turnString.Split(':');
                _turns.Add(new Turn()
                {
                    PointsLeft = Convert.ToByte(x[0]),
                    PointsRight = Convert.ToByte(x[1])
                });
            }

            return _turns;
        }

        #endregion

        #region Zielschiessen

        /// <summary>
        /// Save list of Values from Zielschiessen to localSettings
        /// </summary>
        /// <param name="values"></param>
        internal void SaveZielValues(List<byte> values)
        {
            string zielString = "";
            foreach (var v in values)
            {
                zielString += $"{v};";
            }
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Ziel"] = zielString;
        }

        /// <summary>
        /// Load list of values from Zielschiessen from localSettings
        /// </summary>
        /// <returns></returns>
        internal List<byte> LoadZielValues()
        {
            var _vals = new List<byte>();
            _vals.Clear();
            var localSettings = ApplicationData.Current.LocalSettings;
            if (!(localSettings.Values["Ziel"] is string zielStringComplete))
                return _vals;

            var zielStrings = zielStringComplete?.Split(';');
            foreach (var zielString in zielStrings)
            {
                if (!string.IsNullOrEmpty(zielString))
                    _vals.Add(Convert.ToByte(zielString));
            }

            return _vals;
        }

        #endregion


        #region MidColumnFaktor

        private byte midColumnLength;

        /// <summary>
        /// Breite der mittleren Spalte<br></br>
        /// Nur relevant, wenn TeamNamen angezeigt werden<br></br>
        /// Wert kann nur zwischen 1 und 20 liegen (default bei 10)
        /// </summary>
        public byte MidColumnLength
        {
            get => midColumnLength;
            set
            {
                if (midColumnLength == value ||
                                    value < 1 ||
                                    value > 20)
                    return;

                midColumnLength = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(MidColumnLength)] = value.ToString();
            }
        }

        #endregion


        #region Broadcasting / Networking

        /// <summary>
        /// Switch between true or false (on or off)
        /// </summary>
        public void IsBroadcastingChange()
        {
            IsBroadcasting = !IsBroadcasting;
        }

        private bool isBroadcasting;

        /// <summary>
        /// Broadcasting every Result or not
        /// </summary>
        public bool IsBroadcasting
        {
            get { return isBroadcasting; }
            set
            {
                if (isBroadcasting == value)
                    return;

                isBroadcasting = value;
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(IsBroadcasting)] = value.ToString();

                if (value)
                {
                    var (address, _, broadcast) = BroadcastService.GetIPAddresses();
                    BroadcastAddress = broadcast;
                    IPAddress = address;

                    if (address == null || broadcast == null)
                    {
                        isBroadcasting = false;
                    }
                    else
                    {
                        MdnsService.Advertise();
                        RespServer.Start();
                        if (PServer == null)
                        {
                            PServer = new PubServer();
                        }
                        PServer.Start();
                    }
                }
                else
                {
                    //Dienste abschalten
                    RespServer.Cancel();
                    MdnsService.Unadvertise();
                }
            }
        }
        PubServer PServer;

        /// <summary>
        /// Broadcast IP-Address
        /// </summary>
        public IPAddress BroadcastAddress { get; private set; }
        public IPAddress IPAddress { get; private set; }


        /// <summary>
        /// Broadcast Port
        /// </summary>
        public int BroadcastPort
        {
            get
            {
                return 4711;
            }
        }

        #endregion


        /// <summary>
        /// returns a byte array with ten bytes containing the settings
        /// <para>
        /// 0   Bahnnummer<br></br>  
        /// 1   SpielGruppe<br></br>
        /// 2   Modus<br></br>
        /// 3   Spielrichtung<br></br>
        /// 4   Farbmodus<br></br>
        /// 5   Anzahl max. Punkte pro Kehre<br></br>
        /// 6   Anzahl der Kehren pro Spiel<br></br>
        /// 7   reserve<br></br>
        /// 8   reserve<br></br>
        /// 9   reserve<br></br>
        /// </para>
        /// </summary>
        /// <returns></returns>
        public byte[] GetSettings()
        {
            List<byte> data = new List<byte>
            {
                CourtNumber,                                        //Bahnnummer
                Spielgruppe,                                        //SpielGruppe    
                Convert.ToByte((int)GameSettings.GameModus),        //Modus
                Convert.ToByte(ColorScheme.NextBahnModus),          //Spielrichtung
                Convert.ToByte(ColorScheme.ColorModus),             //FarbModus (hell,dunkel)
                GameSettings.PointsPerTurn,                         //Anzahl max. Punkte pro Kehre
                GameSettings.TurnsPerGame,                          //Anzahl der Kehren
                MidColumnLength,                                    //Breite der mittleren Spalte (nur bei der Anzeige von TeamNamen relevant)
                0,
                0
            };
            return data.ToArray();
        }


        /// <summary>
        /// Set settings as in array specified. Needs a byte array with ten bytes
        /// <para>
        /// 0   Bahnnummer<br></br>  
        /// 1   SpielGruppe<br></br>
        /// 2   Modus<br></br>
        /// 3   Spielrichtung<br></br>
        /// 4   Farbmodus<br></br>
        /// 5   Anzahl max. Punkte pro Kehre<br></br>
        /// 6   Anzahl der Kehren pro Spiel<br></br>
        /// 7   reserve<br></br>
        /// 8   reserve<br></br>
        /// 9   reserve<br></br>
        /// </para>
        /// </summary>
        /// <param name="settingsArray"></param>
        public void SetSettings(byte[] settingsArray)
        {
            this.CourtNumber = settingsArray[0];
            this.Spielgruppe = settingsArray[1];
            this.GameSettings.SetModus(settingsArray[2]);
            this.ColorScheme.SetNextBahnModus(settingsArray[3]);
            this.ColorScheme.SetColorModus(settingsArray[4]);
            this.GameSettings.PointsPerTurn = settingsArray[5];
            this.GameSettings.TurnsPerGame = settingsArray[6];
            this.MidColumnLength = settingsArray[7];
            _ = settingsArray[8];
            _ = settingsArray[9];
        }

        public override string ToString()
        {
            return $"Bahn={CourtNumber};Spielgruppe={Spielgruppe};ColorModus={ColorScheme.ColorModus};GameModus={GameSettings.GameModus};PointsPerTurn={GameSettings.PointsPerTurn};TurnsPerGame={GameSettings.TurnsPerGame};NextBahn={ColorScheme.NextBahnModus}";
        }

        public void SendGameResults(byte[] message)
        {
            System.Diagnostics.Debug.WriteLine($"{string.Join('-', message)}");
            PServer?.SendDataMessage("SendingResultInfo", message);
        }

    }
}
