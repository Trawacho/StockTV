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

            var groupnr = localSettings.Values[nameof(GroupNumber)] as string;
            this.GroupNumber = byte.Parse(groupnr ?? "0");

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
       

        #endregion

        #region GroupNumber

        /// <summary>
        /// Changes the GroupNumber up or down
        /// </summary>
        /// <param name="up"></param>
        public void GroupNumberChange(bool up = true)
        {
            if (up)
                GroupNumber++;
            else
                GroupNumber--;
        }

        private byte groupNumber;
        /// <summary>
        /// Number of the group (CourtNumber Group)
        /// </summary>
        public byte GroupNumber
        {
            internal get => groupNumber;
            set
            {
                if (groupNumber == value ||
                    value < 0 ||
                    value > 10)
                    return;

                groupNumber = value;

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[nameof(GroupNumber)] = value.ToString();
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
        public string GroupNumberLetter
        {
            get
            {
                switch (groupNumber)
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
            var turnStringComplete = localSettings.Values["Turns"] as string;
            if (turnStringComplete == null)
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
            var zielStringComplete = localSettings.Values["Ziel"] as string;
            if (zielStringComplete == null)
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
                    var (address, _, broadcast) = NetworkService.GetIPAddresses();
                    BroadcastAddress = broadcast;
                    IPAddress = address;

                    if (address == null || broadcast == null)
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

        /// <summary>
        ///  returns a byte array with ten bytes containing the settings, starting with courtnumber, groupnumber, modus, direction,.....
        /// </summary>
        /// <returns></returns>
        public byte[] GetDataHeader()
        {
            List<byte> data = new List<byte>
            {
                CourtNumber,                                        //Bahnnummer
                GroupNumber,                                        //SpielGruppe    
                Convert.ToByte((int)GameSettings.GameModus),        //Modus
                Convert.ToByte(ColorScheme.RightToLeft),            //Spielrichtung
                0,
                0,
                0,
                0,
                0,
                0
            };
            return data.ToArray();
        }
        #endregion
    }
}
