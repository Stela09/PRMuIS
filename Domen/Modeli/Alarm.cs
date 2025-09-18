using System;
using Domen.Enumeracije;

namespace Domen.Modeli
{
    [Serializable]
    public class Alarm
    {
        public TipAlarma Tip { get; set; }
        public double Vrednost { get; set; }
        public string Uzrok { get; set; } = string.Empty;
        public DateTime Vreme { get; set; }

    }
}
