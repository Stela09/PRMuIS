using Domen.Enumeracije;
using Domen.Modeli;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace MerniUredjaj
{
    internal class Program
    {
        private const int OSNOVNI_PORT_STANICE = 15000;
        private static Socket _udpSoket;
        private static string _idUredjaja;
        private static IPEndPoint _krajnaStanica;
        private static TipMerenja _tipMerenja;

        public static void Main(string[] args)
        {
            KonfigurisanjeUredjaja();
            PokreniUredjaj();
        }

        // prikuplja id uredjaja, tip merenja i stanicu na koju se povezuje
        private static void KonfigurisanjeUredjaja()
        {
            Console.WriteLine("Unesite ID uređaja (npr. TEMP_001):");
            _idUredjaja = Console.ReadLine() ?? "TEMP_001";

            Console.WriteLine("Dostupne stanice:");
            Console.WriteLine("1. Stanica 1 (Port 15000)");
            Console.WriteLine("2. Stanica 2 (Port 15001)");
            Console.WriteLine("3. Stanica 3 (Port 15002)");
            Console.Write("Izaberite stanicu (1-3): ");

            int izborStanice;
            while (!int.TryParse(Console.ReadLine(), out izborStanice) || izborStanice < 1 || izborStanice > 3)
            {
                Console.Write("Nevažeći izbor. Izaberite 1-3: ");
            }

            int portStanice = OSNOVNI_PORT_STANICE + (izborStanice - 1);
            _krajnaStanica = new IPEndPoint(IPAddress.Parse("127.0.0.1"), portStanice);

            Console.WriteLine("Dostupni tipovi merenja:");
            Console.WriteLine("1. Temperatura");
            Console.WriteLine("2. Vlažnost");
            Console.WriteLine("3. Brzina vetra");
            Console.WriteLine("4. Smer vetra");
            Console.WriteLine("5. Pritisak");
            Console.WriteLine("6. Padavine");
            Console.WriteLine("7. Hemijski sastav");
            Console.WriteLine("8. Oblačnost");
            Console.Write("Izaberite tip merenja (1-8): ");

            int izborMerenja;
            while (!int.TryParse(Console.ReadLine(), out izborMerenja) || izborMerenja < 1 || izborMerenja > 8)
            {
                Console.Write("Nevažeći izbor. Izaberite 1-8: ");
            }

            _tipMerenja = (TipMerenja)(izborMerenja - 1);

            Console.WriteLine($"Uređaj {_idUredjaja} će se povezati na stanicu na portu {portStanice} i meriti {_tipMerenja}");
        }

        //glavna logika uredjaja 
        private static void PokreniUredjaj()
        {
            try
            {
                _udpSoket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp
                );

                Console.WriteLine($"Uređaj {_idUredjaja} je pokrenut. Šalje merenja tipa {_tipMerenja} na {_krajnaStanica}");

                var poruka = $"NOVI_UREDJAJ;{_idUredjaja};{_tipMerenja}";
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);
                _udpSoket.SendTo(podaci, _krajnaStanica);

                while (true)
                {
                    //generisanje i slanje merenja
                    var merenje = GeneratorMerenja.KreirajMerenje(_tipMerenja, _idUredjaja);
                    PosaljiMerenje(merenje);

                    // provera i slanje alarma ako je potrebno
                    ProveriISaljiAlarme(merenje);

                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška uređaja: {ex.Message}");
                _udpSoket?.Close();
            }
        }

        // serijalizacija i slanje merenja
        private static void PosaljiMerenje(Merenje merenje)
        {
            try
            {
                byte[] podaci;
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, merenje);
                    podaci = ms.ToArray();
                }

                _udpSoket.SendTo(podaci, _krajnaStanica);
                Console.WriteLine($"Poslato merenje: {merenje.Vrednost} {merenje.Jedinica} u {merenje.Vreme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri slanju merenja: {ex.Message}");
            }
        }

        // provera da li je merenje van granica i slanje alarma ako jeste
        private static void ProveriISaljiAlarme(Merenje merenje)
        {
            var alarmi = GeneratorMerenja.ProveriAlarme(merenje);

            foreach (var alarm in alarmi)
            {
                try
                {
                    byte[] podaciAlarma;
                    using (var ms = new MemoryStream())
                    {
                        var bf = new BinaryFormatter();
                        bf.Serialize(ms, alarm);
                        podaciAlarma = ms.ToArray();
                    }

                    _udpSoket.SendTo(podaciAlarma, _krajnaStanica);
                    Console.WriteLine($"ALARM poslat: {alarm.Uzrok}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška pri slanju alarma: {ex.Message}");
                }
            }
        }
    }
}
