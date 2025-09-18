using System;
using System.Collections.Generic;
using Domen.Modeli;
using Domen.Enumeracije;

namespace MerniUredjaj
{
    public static class GeneratorMerenja
    {
        private static readonly Random _slucajni = new Random();

        private class GranicaAlarma
        {
            public double? Donja { get; set; }
            public double? Gornja { get; set; }
            public TipAlarma TipDonja { get; set; }
            public TipAlarma TipGornja { get; set; }
            public string Jedinica { get; set; } = "";
            public string Poruka { get; set; } = "";
        }

        private static readonly Dictionary<TipMerenja, GranicaAlarma> konfiguracijaAlarma =
            new Dictionary<TipMerenja, GranicaAlarma>
            {
                { TipMerenja.Temperatura, new GranicaAlarma { Donja = -5, Gornja = 35, TipDonja = TipAlarma.NiskaTemperatura, TipGornja = TipAlarma.VisokaTemperatura, Jedinica="°C", Poruka="temperatura" } },
                { TipMerenja.Vlaznost, new GranicaAlarma { Donja = 10, Gornja = 90, TipDonja = TipAlarma.NiskaVlaznost, TipGornja = TipAlarma.VisokaVlaznost, Jedinica="%", Poruka="vlažnost" } },
                { TipMerenja.BrzinaVetra, new GranicaAlarma { Gornja = 80, TipGornja = TipAlarma.VisokaBrzinaVetra, Jedinica="km/h", Poruka="brzina vetra" } },
                { TipMerenja.Pritisak, new GranicaAlarma { Donja = 960, Gornja = 1040, TipDonja = TipAlarma.NizakPritisak, TipGornja = TipAlarma.VisokPritisak, Jedinica="hPa", Poruka="pritisak" } },
                { TipMerenja.Padavine, new GranicaAlarma { Gornja = 30, TipGornja = TipAlarma.PrekomernePadavine, Jedinica="mm/h", Poruka="padavine" } },
                { TipMerenja.HemijskiSastav, new GranicaAlarma { Donja = 5, Gornja = 80, TipDonja = TipAlarma.NiskaKoncentracijaHemikalija, TipGornja = TipAlarma.VisokaKoncentracijaHemikalija, Jedinica="ppm", Poruka="hemijska koncentracija" } },
                { TipMerenja.Oblacnost, new GranicaAlarma { Gornja = 95, TipGornja = TipAlarma.VisokaOblacnost, Jedinica="%", Poruka="oblačnost" } },
            };

        public static Merenje KreirajMerenje(TipMerenja tip, string idUredjaja)
        {
            int vrednost;

            switch (tip)
            {
                case TipMerenja.Temperatura:
                    vrednost = _slucajni.Next(-10, 40);
                    break;
                case TipMerenja.Vlaznost:
                    vrednost = _slucajni.Next(0, 101);
                    break;
                case TipMerenja.BrzinaVetra:
                    vrednost = _slucajni.Next(0, 120);
                    break;
                case TipMerenja.PravacVetra:
                    vrednost = _slucajni.Next(0, 360);
                    break;
                case TipMerenja.Pritisak:
                    vrednost = _slucajni.Next(950, 1051);
                    break;
                case TipMerenja.Padavine:
                    vrednost = _slucajni.Next(0, 51);
                    break;
                case TipMerenja.HemijskiSastav:
                    vrednost = _slucajni.Next(0, 101);
                    break;
                case TipMerenja.Oblacnost:
                    vrednost = _slucajni.Next(0, 101);
                    break;
                default:
                    throw new ArgumentException($"Nepodržani tip merenja: {tip}");
            }


            string jedinica = konfiguracijaAlarma.ContainsKey(tip) ? konfiguracijaAlarma[tip].Jedinica : tip == TipMerenja.PravacVetra ? "°" : "";

            return new Merenje
            {
                IdUredjaja = idUredjaja,
                Tip = tip,
                Vrednost = vrednost,
                Jedinica = jedinica,
                Vreme = DateTime.Now
            };
        }

        public static List<Alarm> ProveriAlarme(Merenje merenje)
        {
            var alarmi = new List<Alarm>();
            if (!konfiguracijaAlarma.TryGetValue(merenje.Tip, out var granica))
                return alarmi;

            if (granica.Gornja.HasValue && merenje.Vrednost > granica.Gornja.Value)
            {
                alarmi.Add(new Alarm
                {
                    Tip = granica.TipGornja,
                    Vrednost = merenje.Vrednost,
                    Uzrok = $"Visoka {granica.Poruka}: {merenje.Vrednost}{granica.Jedinica}",
                    Vreme = DateTime.Now
                });
            }
            if (granica.Donja.HasValue && merenje.Vrednost < granica.Donja.Value)
            {
                alarmi.Add(new Alarm
                {
                    Tip = granica.TipDonja,
                    Vrednost = merenje.Vrednost,
                    Uzrok = $"Niska {granica.Poruka}: {merenje.Vrednost}{granica.Jedinica}",
                    Vreme = DateTime.Now
                });
            }

            return alarmi;
        }
    }
}
