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
        public int MassenVorneSumme { get { return SummeVon(_massenVorne); } }

        /// <summary>
        /// Sum of Values about the attempts of Schüsse
        /// </summary>
        public int SchüsseSumme { get { return SummeVon(_schüsse); } }
        /// <summary>
        /// Sum of Values about the attempts of Massen Hinten
        /// </summary>
        public int MassenHintenSumme { get { return SummeVon(_massenHinten); } }

        /// <summary>
        /// Sum of Values about the attempts of Kombinieren
        /// </summary>
        public int KombinierenSumme { get { return SummeVon(_kombinieren); } }

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

        private readonly ConcurrentStack<byte> _massenVorne;
        private readonly ConcurrentStack<byte> _schüsse;
        private readonly ConcurrentStack<byte> _massenHinten;
        private readonly ConcurrentStack<byte> _kombinieren;


        /// <summary>
        /// List of all Values (MassenVorne, Schüsse, MassenHinten, Kombinieren)
        /// </summary>
        private List<byte> ListOfAllValues
        {
            get
            {
                var values = new List<byte>();

                values.AddRange(_massenVorne.Reverse().ToList());
                values.AddRange(_schüsse.Reverse().ToList());
                values.AddRange(_massenHinten.Reverse().ToList());
                values.AddRange(_kombinieren.Reverse().ToList());

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
            this._massenVorne = new ConcurrentStack<byte>();
            this._massenHinten = new ConcurrentStack<byte>();
            this._schüsse = new ConcurrentStack<byte>();
            this._kombinieren = new ConcurrentStack<byte>();

            this.LoadTurnsFromLocalSettings();
        }

        #endregion

        #region Save and Load Values to Settings

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

        #endregion

        #region Private helper Functions

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

        #endregion

        /// <summary>
        /// Checks if Value is for actual Block of attempts allowed and insertes the value if true
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool AddValueToVersuche(sbyte value)
        {
            if (CountOfVersuche() < Settings.Instance.GameSettings.TurnsPerGame * 4)
            {
                if (_massenVorne.Count() < Settings.Instance.GameSettings.TurnsPerGame 
                    && IsMassValue(value))
                {
                    _massenVorne.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= Settings.Instance.GameSettings.TurnsPerGame 
                    && _schüsse.Count() < Settings.Instance.GameSettings.TurnsPerGame 
                    && IsSchussValue(value))
                {
                    _schüsse.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= (2 * Settings.Instance.GameSettings.TurnsPerGame) 
                    && _massenHinten.Count() < Settings.Instance.GameSettings.TurnsPerGame 
                    && IsMassValue(value))
                {
                    _massenHinten.Push(Convert.ToByte(value));
                }
                else if (CountOfVersuche() >= (3 * Settings.Instance.GameSettings.TurnsPerGame )
                    && _kombinieren.Count() < Settings.Instance.GameSettings.TurnsPerGame 
                    && IsMassValue(value))
                {
                    _kombinieren.Push(Convert.ToByte(value));
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
            if (_kombinieren.Count > 0)
            {
                _kombinieren.TryPop(out _);
            }
            else if (_massenHinten.Count > 0)
            {
                _massenHinten.TryPop(out _);
            }
            else if (_schüsse.Count > 0)
            {
                _schüsse.TryPop(out _);
            }
            else if (_massenVorne.Count > 0)
            {
                _massenVorne.TryPop(out _);
            }

            SaveTurnsToLocalSettings();
            RaiseValuesChanged();
        }

        /// <summary>
        /// Delete all Values in all Blocks of Attempts
        /// </summary>
        internal void Reset()
        {
            _massenVorne.Clear();
            _massenHinten.Clear();
            _schüsse.Clear();
            _kombinieren.Clear();
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
        /// Anzahl aller Versuche die bereits hinterlegt sind
        /// </summary>
        /// <returns></returns>
        internal int CountOfVersuche()
        {
            return _massenVorne.Count + _schüsse.Count + _massenHinten.Count + _kombinieren.Count;
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
