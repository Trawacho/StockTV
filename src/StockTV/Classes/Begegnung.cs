using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTV.Classes
{
    /// <summary>
    /// Class to display TeamNames
    /// </summary>
    public class Begegnung
    {
        public Begegnung(byte spielNummer, string TeamA, string TeamB)
        {
            Spielnummer = spielNummer;
            Mannschaft_A = TeamA;
            Mannschaft_B = TeamB;
        }

        public byte Spielnummer { get; set; }
        private readonly string Mannschaft_A;
        private readonly string Mannschaft_B;

        public string TeamNameLeft(bool colorSchemeRightToLeft)
        {
            return colorSchemeRightToLeft ? Mannschaft_B : Mannschaft_A;
        }

        public string TeamNameRight(bool colorSchemeRightToLeft)
        {
            return colorSchemeRightToLeft ? Mannschaft_A : Mannschaft_B;
        }
    }
}
