using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Windows.Storage;

namespace StockTV.Classes
{
    /// <summary>
    /// Singleton-Class for the Settings from the app
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
            var turnStringComplete = localSettings.Values["Turns"] as string;
            if (turnStringComplete == null)
                return _turns;

            var turnStrings = turnStringComplete?.Split(';') ;
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
                    var (address, mask, broadcast) = NetworkService.GetIPAddresses();
                    BroadcastAddress = broadcast;
                    IPAddress = address;

                    if(address == null || broadcast == null)
                    {
                        isBroadcasting = false;
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast IP-Address
        /// </summary>
        public IPAddress BroadcastAddress { get; private set; }
        public IPAddress IPAddress { get; private set; }

        const int broadcastPort = 4711;

        /// <summary>
        /// Broadcast Port
        /// </summary>
        public int BroadcastPort
        {
            get
            {
                return broadcastPort;
            }
        }

        #endregion
    }
}
