using System;
using Domen.Enumeracije;

namespace Domen.Modeli
{
    [Serializable]
    public class Merenje
    {
        public string IdUredjaja { get; set; } = string.Empty;
        public TipMerenja Tip { get; set; }
        public double Vrednost { get; set; }
        public string Jedinica { get; set; } = string.Empty;
        public DateTime Vreme { get; set; }
    }
}
