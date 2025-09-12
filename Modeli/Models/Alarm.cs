using Domen.Enums;
using System;

namespace Domen.Models
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
