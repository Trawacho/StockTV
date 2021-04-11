using NetMQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace StockTV.Classes.NetMQUtil
{
    internal delegate void MqServerDataReceivedEventHandler(MqServerDataReceivedEventArgs e);

    internal class MqServerDataReceivedEventArgs : EventArgs
    {
        private byte[] Data { get; set; }
        private NetMQMessage Message { get; set; }
        private Hashtable table = new Hashtable();
        public MqServerDataReceivedEventArgs(NetMQMessage message)
        {
            this.Message = message;
            this.Data = message.Last.ToByteArray();
            var l = Encoding.UTF8.GetString(Data).TrimEnd(';').Split(';');
            foreach (var item in l)
            {
                var t = item.Split('=');
                if (t.Length == 2)
                    table.Add(t[0], t[1]);
            }

        }

        #region GameModus

        public GameSettings.GameModis GameModus
        {
            get
            {
                try
                {
                    return (GameSettings.GameModis)Enum.Parse(typeof(GameSettings.GameModis), table["GameModus"].ToString());

                }
                catch (Exception)
                {
                    return GameSettings.GameModis.Training;
                }
            }
        }
        public bool IsGameModus
        {
            get
            {
                return table.ContainsKey("GameModus");
            }
        }

        #endregion

        #region PointsPerTurn
        public byte PointsPerTurn
        {
            get
            {
                try
                {
                    return Convert.ToByte(table[nameof(GameSettings.PointsPerTurn)]);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public bool IsPointsPerTurn
        {
            get
            {
                return table.ContainsKey(nameof(GameSettings.PointsPerTurn));
            }
        }

        #endregion

        #region TurnsPerGame

        public byte TurnsPerGame
        {
            get
            {
                try
                {
                    return Convert.ToByte(table[nameof(GameSettings.TurnsPerGame)]);
                }
                catch (Exception)
                {
                    return 6;
                }
            }
        }

        public bool IsTurnsPerGame
        {
            get
            {
                return table.ContainsKey(nameof(GameSettings.TurnsPerGame));
            }
        }

        #endregion

        #region ColorScheme

        public ColorScheme.Schemes ColorScheme
        {
            get
            {
                try
                {
                    return (Classes.ColorScheme.Schemes)Enum.Parse(typeof(Classes.ColorScheme.Schemes), table[nameof(Classes.ColorScheme)].ToString());
                }
                catch (Exception)
                {
                    return Classes.ColorScheme.Schemes.Normal;
                }
            }
        }

        public bool IsColorScheme
        {
            get
            {
                return table.ContainsKey(nameof(ColorScheme));
            }
        }
        #endregion

        #region Reset

        public bool Reset
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(table["Reset"]);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool IsReset
        {
            get
            {
                return table.ContainsKey("Reset");
            }
        }

        #endregion

        #region Begegnungen 

        public bool IsSetBegegnungen
        {
            get
            {
                return table.ContainsKey("SetBegegnungen");
            }
        }

        public IEnumerable<Begegnung> Begegnungen
        {
            get
            {
                var retVal = new List<Begegnung>();
                var s1 = Encoding.UTF8.GetString(Data).Split("=");
                if (!s1[0].Contains("SetBegegnungen")) return retVal;

                var a = s1[1].TrimEnd(';').Split(';');
                foreach (var b in a)
                {
                    var c = b.Split(':');
                    if (byte.TryParse(c[0], out byte _spielNummer))
                    {
                        retVal.Add(new Begegnung(_spielNummer, c[1], c[2]));
                    }
                }
                return retVal;
            }
        }

        #endregion

        #region Nächste Bahn Links/Rechts
        public bool IsNextCourtLeft => table.ContainsKey("NextLeft");
        public bool NextCourtLeft
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(table["NextLeft"]);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion

        #region GetSettings

        public bool IsGetSettings => table.ContainsKey("Get") && table.ContainsValue("Settings");
        #endregion

        #region GetResult
        public bool IsGetResult => table.ContainsKey("Get") && table.ContainsValue("Result");
        
        #endregion
    }
}
