using System;

namespace Domen.Models
{
    [Serializable]
    public class Koordinate
    {
        public double GeografskaSirina { get; }
        public double GeografskaDuzina { get; }

        public Koordinate(double geografskaSirina, double geografskaDuzina)
        {
            GeografskaSirina = geografskaSirina;
            GeografskaDuzina = geografskaDuzina;
        }
    }
}
