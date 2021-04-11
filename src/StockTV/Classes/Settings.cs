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

            var broadcasting = localSettings.Values[nameof(IsBroadcasting)] as string;
            this.IsBroadcasting = bool.Parse(broadcasting ?? "false");

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

        public override string ToString()
        {
            return $"Bahn={CourtNumber};ColorScheme={ColorScheme.Scheme};GameModus={GameSettings.GameModus};PointsPerTurn={GameSettings.PointsPerTurn};TurnsPerGame={GameSettings.TurnsPerGame};NextLeft={ColorScheme.RightToLeft}";
        }

        public void SendGameResults(byte[] message)
        {
            PServer.SendDataMessage("SendingResultInfo", message);
        }

    }
}
