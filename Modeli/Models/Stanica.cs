using Domen.Models;
using System;
using System.Collections.Generic;

namespace Domen.Models
{
    [Serializable]
    public class Stanica
    {
        public string Naziv { get; set; } = string.Empty;
        public Koordinate Koordinate { get; set; }
        public int BrojStanovnika { get; set; }
        public int BrojUredjaja { get; set; }
        public List<Merenje> Merenja { get; set; } = new List<Merenje>();
        public List<Alarm> AktivniAlarmi { get; set; } = new List<Alarm>();
    }
}
