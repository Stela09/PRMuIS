using Domen.Enums;
using System;

namespace Domen.Models
{
    [Serializable]
    public class Merenje
    {
        public string UredjajId { get; set; } = string.Empty;
        public TipMerenja Tip { get; set; }
        public double Vrednost { get; set; }
        public string Jedinica { get; set; } = string.Empty;
        public DateTime Vreme { get; set; }
    }
}
