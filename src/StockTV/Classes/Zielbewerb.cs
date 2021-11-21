using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace StockTV.Classes
{
    public class Zielbewerb
    {
        #region EventHandler for ValuesChanged

        public event EventHandler ValuesChanged;
        protected void RaiseValuesChanged()
        {
            var handler = ValuesChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Properties of SUMs

        /// <summary>
        /// Sum of Values about the attempts of Massen Vorne
        /// </summary>
        public int MassenVorneSumme { get { return SummeVon(MassenVorne); } }

        /// <summary>
        /// Sum of Values about the attempts of Schüsse
        /// </summary>
        public int SchüsseSumme { get { return SummeVon(Schüsse); } }
        /// <summary>
        /// Sum of Values about the attempts of Massen Hinten
        /// </summary>
        public int MassenHintenSumme { get { return SummeVon(MassenHinten); } }

        /// <summary>
        /// Sum of Values about the attempts of Kombinieren
        /// </summary>
        public int KombinierenSumme { get { return SummeVon(Kombinieren); } }

        /// <summary>
        /// Sum of Values over all attempts
        /// </summary>
        public int GesamtSumme
        {
            get
            {
                return MassenVorneSumme + MassenHintenSumme + SchüsseSumme + KombinierenSumme;
            }
        }

        #endregion

        #region private Fields

        private readonly ConcurrentStack<byte> MassenVorne;
        private readonly ConcurrentStack<byte> Schüsse;
        private readonly ConcurrentStack<byte> MassenHinten;
        private readonly ConcurrentStack<byte> Kombinieren;


        /// <summary>
        /// List of all Values (MassenVorne, Schüsse, MassenHinten, Kombinieren)
        /// </summary>
        private List<byte> ListOfAllValues
        {
            get
            {
                var values = new List<byte>();

                values.AddRange(MassenVorne.Reverse().ToList());
                values.AddRange(Schüsse.Reverse().ToList());
                values.AddRange(MassenHinten.Reverse().ToList());
                values.AddRange(Kombinieren.Reverse().ToList());

                return values;
            }
        }


        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public Zielbewerb()
        {
            this.MassenVorne = new ConcurrentStack<byte>();
            this.MassenHinten = new ConcurrentStack<byte>();
            this.Schüsse = new ConcurrentStack<byte>();
            this.Kombinieren = new ConcurrentStack<byte>();

            this.LoadTurnsFromLocalSettings();
        }
        
        #endregion


        /// <summary>
        /// Checks if Value is for actual Block of attempts allowed and insertes the value if true
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool AddValueToVersuche(sbyte value)
        {
            if (CountOfVersuche() < 24)
            {
                if (MassenVorne.Count() < 6 && IsMassValue(value))
                {
                    MassenVorne.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= 6 && Schüsse.Count() < 6 && IsSchussValue(value))
                {
                    Schüsse.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= 12 && MassenHinten.Count() < 6 && IsMassValue(value))
                {
                    MassenHinten.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= 18 && Kombinieren.Count() < 6 && IsMassValue(value))
                {
                    Kombinieren.Push(Convert.ToByte(value));
                }
                else
                {
                    return false;
                }
                SaveTurnsToLocalSettings();
                RaiseValuesChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Delete the last inserted value
        /// </summary>
        internal void DeleteLastValue()
        {
            if (Kombinieren.Count > 0)
            {
                Kombinieren.TryPop(out _);
            }
            else if (MassenHinten.Count > 0)
            {
                MassenHinten.TryPop(out _);
            }
            else if (Schüsse.Count > 0)
            {
                Schüsse.TryPop(out _);
            }
            else if (MassenVorne.Count > 0)
            {
                MassenVorne.TryPop(out _);
            }

            SaveTurnsToLocalSettings();
            RaiseValuesChanged();
        }

        /// <summary>
        /// Delete all Values in all Blocks of Attempts
        /// </summary>
        internal void Reset()
        {
            MassenVorne.Clear();
            MassenHinten.Clear();
            Schüsse.Clear();
            Kombinieren.Clear();
            SaveTurnsToLocalSettings();
            RaiseValuesChanged();
        }

        /// <summary>
        /// Last value of all Values
        /// </summary>
        /// <returns></returns>
        internal int LastValue()
        {
            return ListOfAllValues.LastOrDefault();
        }


        /// <summary>
        /// Save all Values
        /// </summary>
        private void SaveTurnsToLocalSettings()
        {
            Settings.Instance.SaveZielValues(ListOfAllValues);
        }

        /// <summary>
        /// Load all Values from Settings
        /// </summary>
        private void LoadTurnsFromLocalSettings()
        {
            foreach (var t in Settings.Instance.LoadZielValues())
            {
                this.AddValueToVersuche(Convert.ToSByte(t));
            }
        }


        /// <summary>
        /// Anzahl aller Versuche die bereits hinterlegt sind
        /// </summary>
        /// <returns></returns>
        internal int CountOfVersuche()
        {
            return MassenVorne.Count + Schüsse.Count + MassenHinten.Count + Kombinieren.Count;
        }

        /// <summary>
        /// Summe aller Werte von stack
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        private int SummeVon(ConcurrentStack<byte> stack)
        {
            int value = 0;
            foreach (var v in stack)
            {
                value += Convert.ToInt32(v);
            }
            return value;
        }

        /// <summary>
        /// Ist der Wert bei einem Mass-Versuch gültig
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsMassValue(sbyte value)
        {
            switch (value)
            {
                case 0:
                case 2:
                case 4:
                case 6:
                case 8:
                case 10:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Ist der Wert bei einem Schuss-Versuch gültig
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsSchussValue(sbyte value)
        {
            switch (value)
            {
                case 0:
                case 2:
                case 5:
                case 10:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Erzeugt ein byte-Array mit allen Werten. Im Ersten Byte ist die Bahnnummer. In den Weiteren der Wert der Versuche
        /// </summary>
        /// <param name="compressed"></param>
        /// <param name="courtNumber"></param>
        /// <returns></returns>
        public byte[] Serialize(bool compressed = false)
        {
            /* 
             *   the byte array starts with ten bytes, containing the settings, starting with courtnumber, groupnumber, modus, direction,.....
             *  
             *  e.g.
             *  01 00 00 00 00 00 00 00 00 00 04 08 00 10 04 08 05
             *  Court 1
             *     Group 0
             *        Modus 0
             *           direction 0
             *           .....
             *                                 Attempt 1: 4    (Massen Vorne)
             *                                    Attempt 2: 8    (Massen Vorne)
             *                                       Attempt 3: 0    (Massen Vorne)
             *                                          Attempt 4: 10   (Massen Vorne)
             *                                             Attempt 5: 4    (Massen Vorne)
             *                                                Attempt 6: 8    (Massen Vorne)
             *                                                   Attempt 7: 5    (Schiessen)
             *  
             */


            var values = new List<byte>();
            values.AddRange(Settings.Instance.GetSettings());


            //Add for each attempt the value 
            foreach (var a in ListOfAllValues)
            {
                values.Add(a);
            }


            //Convert the list of values to an array
            var data = values.ToArray();

            if (!compressed)
                return data;

            var output = new MemoryStream();
            using (var datastream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                datastream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }
}
