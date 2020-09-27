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
        public int MassenVorneSumme { get { return SummeVon(MassenVorne); } }
        public int MassenHintenSumme { get { return SummeVon(MassenHinten); } }
        public int SchüsseSumme { get { return SummeVon(Schüsse); } }
        public int KombinierenSumme { get { return SummeVon(Kombinieren); } }

        public int GesamtSumme
        {
            get
            {
                return MassenVorneSumme + MassenHintenSumme + SchüsseSumme + KombinierenSumme;
            }
        }

        public Zielbewerb()
        {
            this.MassenVorne = new ConcurrentStack<byte>();
            this.MassenHinten = new ConcurrentStack<byte>();
            this.Schüsse = new ConcurrentStack<byte>();
            this.Kombinieren = new ConcurrentStack<byte>();
        }

        private readonly ConcurrentStack<byte> MassenVorne;
        private readonly ConcurrentStack<byte> MassenHinten;
        private readonly ConcurrentStack<byte> Schüsse;
        private readonly ConcurrentStack<byte> Kombinieren;


        internal bool IsSettingsInput()
        {
            return MassenVorne.Count() == 3 &&
                   MassenVorneSumme == 22;
        }

        internal void AddValueToVersuche(sbyte value)
        {
            if (MassenVorne.Count() < 6)
            {
                MassenVorne.Push(Convert.ToByte(value));
            }
            else if (Schüsse.Count() < 6)
            {
                Schüsse.Push(Convert.ToByte(value));
            }
            else if (MassenHinten.Count() < 6)
            {
                MassenHinten.Push(Convert.ToByte(value));
            }
            else if (Kombinieren.Count() < 6)
            {
                Kombinieren.Push(Convert.ToByte(value));
            }
        }

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
        }

        internal void Reset()
        {
            MassenVorne.Clear();
            MassenHinten.Clear();
            Schüsse.Clear();
            Kombinieren.Clear();
        }


        /// <summary>
        /// Anzahl aller Versuche die bereits hinterlegt sind
        /// </summary>
        /// <returns></returns>
        internal int CountOfVersuche()
        {
            return MassenVorne.Count + Schüsse.Count + MassenHinten.Count + Kombinieren.Count;
        }

        private int SummeVon(ConcurrentStack<byte> stack)
        {
            int value = 0;
            foreach (var v in stack)
            {
                value += Convert.ToInt32(v);
            }
            return value;
        }

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
            foreach(var a in MassenVorne)
            {
                values.Add(a);
            }
            foreach(var a in Schüsse)
            {
                values.Add(a);
            }
            foreach(var a in MassenHinten)
            {
                values.Add(a);
            }
            foreach(var a in Kombinieren)
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
