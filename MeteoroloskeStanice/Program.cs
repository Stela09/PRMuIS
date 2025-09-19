using Domen.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading;
using Domen;

namespace MeteoroloskeStanice
{
    internal class Program
    {
        private const int PORT_SERVERA = 10000;
        private const string IP_SERVERA = "127.0.0.1";

        private static Socket tcpVeza;
        private static Socket udpListener;
        private static Stanica mojaStanica;
        private static List<Merenje> stiglaMerenja = new List<Merenje>();
        private static List<Alarm> aktivniAlarmi = new List<Alarm>();

        static void Main(string[] args)
        {
            int udpPort = UnesiPortStanice();
            PokreniInicijalizaciju(udpPort);
            PokreniStanicaLoop();
        }

        // unosi i validira port stanice
        private static int UnesiPortStanice()
        {
            Console.WriteLine("Unesite port stanice (15000-15002):");
            int port;
            while (!int.TryParse(Console.ReadLine(), out port) || port < 15000 || port > 15002)
            {
                Console.WriteLine("Nevažeći port. Probajte ponovo (15000-15002):");
            }
            return port;
        }

        // pokreće TCP i UDP veze, preuzima podatke stanice
        private static void PokreniInicijalizaciju(int udpPort)
        {
            try
            {
                // TCP veza sa serverom
                tcpVeza = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpVeza.Connect(new IPEndPoint(IPAddress.Parse(IP_SERVERA), PORT_SERVERA));
                Console.WriteLine("Povezano na server.");

                // UDP listener za mjerne uređaje
                udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpListener.Bind(new IPEndPoint(IPAddress.Any, udpPort));
                Console.WriteLine($"UDP listener pokrenut na portu {udpPort}.");

                // Preuzimanje osnovnih podataka stanice sa servera
                mojaStanica = NetworkHelper.ReceiveMessage<Stanica>(tcpVeza);
                Console.WriteLine($"Preuzeti podaci stanice: {mojaStanica.Naziv}");

                Console.WriteLine($"Stanica '{mojaStanica.Naziv}' inicijalizovana.");
                Console.WriteLine($"Čekanje podataka uređaja na UDP portu {udpPort}...");
            }
            catch (Exception e)
            {
                
                Console.WriteLine($"Greška prilikom inicijalizacije: {e.Message}");
            }
        }

        // glavna petlja stanice koja pokreće osluškivanje i slanje podataka
        private static void PokreniStanicaLoop()
        {
            var threadUDP = new Thread(OsluskjujUredjaje);
            threadUDP.Start();

            var threadTCP = new Thread(SlanjePodatakaServeru);
            threadTCP.Start();

            Console.WriteLine("Pritisnite Enter za izlaz...");
            Console.ReadLine();

            tcpVeza?.Close();
            udpListener?.Close();
        }

        // osluškuje UDP poruke od mernih uređaja i obrađuje ih
        private static void OsluskjujUredjaje()
        {
            var buffer = new byte[1024];
            EndPoint udaljeniEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    int bytes = udpListener.ReceiveFrom(buffer, ref udaljeniEP);
                    var data = buffer.Take(bytes).ToArray();

                    // pokuša prvo kao merenje
                    try
                    {
                        var merenje = NetworkHelper.Deserialize<Merenje>(data);
                        lock (stiglaMerenja)
                        {
                            stiglaMerenja.Add(merenje);
                        }
                        Console.WriteLine($"Primljeno merenje od {merenje.IdUredjaja}: {merenje.Vrednost}{merenje.Jedinica}");
                    }
                    catch
                    {
                        // ako nije merenje, pokušaj kao alarm
                        try
                        {
                            var alarm = NetworkHelper.Deserialize<Alarm>(data);
                            lock (aktivniAlarmi)
                            {
                                aktivniAlarmi.Add(alarm);
                            }
                            Console.WriteLine($"Primljen alarm: {alarm.Uzrok}");
                        }
                        catch
                        {
                            // ako nije ni alarm, možda je poruka tipa "NOVI_UREDJAJ"
                            string poruka = System.Text.Encoding.UTF8.GetString(data);
                            var delovi = poruka.Split(';');
                            if (delovi[0] == "NOVI_UREDJAJ")
                            {
                                int idUredjaja = int.Parse(delovi[1]);
                                string tip = delovi[2];

                                mojaStanica.DodajUredjaj();
                                Console.WriteLine($"Novi uređaj ({tip}) dodat. Ukupno uređaja: {mojaStanica.BrojUredjaja}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška u UDP osluškivanju: {ex.Message}");
                }
            }
        }

        // šalje prikupljene podatke serveru na svakih sekund
        private static void SlanjePodatakaServeru()
        {
            while (true)
            {
                try
                {
                    lock (stiglaMerenja)
                    {
                        mojaStanica.Merenja = new List<Merenje>(stiglaMerenja);
                        stiglaMerenja.Clear();
                    }

                    lock (aktivniAlarmi)
                    {
                        mojaStanica.AktivniAlarmi = new List<Alarm>(aktivniAlarmi);
                        aktivniAlarmi.Clear();
                    }

                    NetworkHelper.SendMessage(tcpVeza, mojaStanica);
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Greška pri slanju podataka serveru: {e.Message}");
                    break;
                }
            }
        }
    }
}
