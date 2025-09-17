using System;

namespace Domen.Modeli
{
    [Serializable]
    public class Koordinate
    {
        public double Sirina { get; }
        public double Duzina { get; }

        public Koordinate(double sirina, double duzina)
        {
            Sirina = sirina;
            Duzina = duzina;
        }
    }
}
