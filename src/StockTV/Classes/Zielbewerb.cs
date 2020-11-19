using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    public class Zielbewerb
    {
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

        #endregion

        public Zielbewerb()
        {
            this.MassenVorne = new ConcurrentStack<byte>();
            this.MassenHinten = new ConcurrentStack<byte>();
            this.Schüsse = new ConcurrentStack<byte>();
            this.Kombinieren = new ConcurrentStack<byte>();

            this.LoadTurnsFromLocalSettings();
        }



        /// <summary>
        /// Checks if Value is for actual Block of attempts allowed and insertes the value if true
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool AddValueToVersuche(sbyte value)
        {
            if (CountOfVersuche() < 24)
            {
                if (MassenVorne.Count() < 6)
                {
                    if (IsMassValue(value))
                        MassenVorne.Push(Convert.ToByte(value));
                }
                else if (Schüsse.Count() < 6)
                {
                    if (IsSchussValue(value))
                        Schüsse.Push(Convert.ToByte(value));
                }
                else if (MassenHinten.Count() < 6)
                {
                    if (IsMassValue(value))
                        MassenHinten.Push(Convert.ToByte(value));
                }
                else if (Kombinieren.Count() < 6)
                {
                    if (IsMassValue(value))
                        Kombinieren.Push(Convert.ToByte(value));
                }

                SaveTurnsToLocalSettings();
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
        }

        internal int LastValue()
        {
            return ListOfAllValues.LastOrDefault();
        }


        /// <summary>
        /// Save all turns as long as <see cref="GameSettings.GameModus"/> isn´t <see cref="GameSettings.GameModis.Training"/>
        /// </summary>
        private void SaveTurnsToLocalSettings()
        {
            Settings.Instance.SaveZielValues(ListOfAllValues);
        }

        private void LoadTurnsFromLocalSettings()
        {
            foreach (var t in Settings.Instance.LoadZielValues())
            {
                this.AddValueToVersuche(Convert.ToSByte(t));
            }

        }

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
        public byte[] Serialize(bool compressed = false, byte courtNumber = 0)
        {
            /* 
             *  the byte[] should have as first byte the courtnumber, follow by the values of the attempts
             *  
             *  e.g.
             *  01 04 08 00 10 04 08 05
             *  Court 1
             *  Attempt 1: 4    (Massen Vorne)
             *  Attempt 2: 8    (Massen Vorne)
             *  Attempt 3: 0    (Massen Vorne)
             *  Attempt 4: 10   (Massen Vorne)
             *  Attempt 5: 4    (Massen Vorne)
             *  Attempt 6: 8    (Massen Vorne)
             *  Attempt 7: 5    (Schiessen)
             *  
             */


            var values = new List<byte>();

            //First byte is CourtNumber
            if (courtNumber == 0)
                courtNumber = Settings.Instance.CourtNumber;

            values.Add(courtNumber);

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
