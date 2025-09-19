using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Domen;
using Domen.Modeli;

namespace Server
{
    internal class CentralServerNonBlocking
    {
        private const int LISTEN_PORT = 10000;
        private static Socket _listenerSocket;
        private static List<Socket> _povezaneStanice = new List<Socket>();
        private static Dictionary<Socket, Stanica> _mapaStanica = new Dictionary<Socket, Stanica>();
        private static readonly Random _rand = new Random();

        static void Main(string[] args)
        {
            InicijalizujServer();
            PokreniServer();
        }

        private static void InicijalizujServer()
        {
            try
            {
                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, LISTEN_PORT));
                _listenerSocket.Listen(10);
                _listenerSocket.Blocking = false;

                Console.WriteLine($"Centralni server pokrenut na portu {LISTEN_PORT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška prilikom inicijalizacije servera: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void PokreniServer()
        {
            string[] gradovi = { "Beograd", "Novi Sad", "Niš", "Kragujevac", "Subotica" };

            while (true)
            {
                // Prihvatanje novih stanica
                if (_listenerSocket.Poll(500 * 1000, SelectMode.SelectRead))
                {
                    try
                    {
                        Socket s = _listenerSocket.Accept();
                        s.Blocking = false;

                        var stanica = new Stanica
                        {
                            Naziv = gradovi[_povezaneStanice.Count % gradovi.Length],
                            Koordinate = new Koordinate(
                                42.0 + _rand.NextDouble() * 3.0,
                                19.0 + _rand.NextDouble() * 5.0
                            ),
                            BrojStanovnika = _rand.Next(50000, 2000000),
                            BrojUredjaja = _rand.Next(3, 10)
                        };

                        _povezaneStanice.Add(s);
                        _mapaStanica[s] = stanica;

                        // Pošalji početne podatke stanice preko NetworkHelper
                        NetworkHelper.SendMessage(s, stanica);

                        Console.WriteLine($"Nova stanica povezana: {stanica.Naziv}");
                    }
                    catch (SocketException)
                    {
                        // Nema novih konekcija
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška prilikom prihvata stanice: {ex.Message}");
                    }
                }

                // Primanje podataka sa stanica
                foreach (var s in _povezaneStanice.ToList())
                {
                    try
                    {
                        if (s.Poll(500 * 1000, SelectMode.SelectRead))
                        {
                            // Koristimo NetworkHelper da primimo kompletan objekat Stanica
                            Stanica azuriranaStanica = NetworkHelper.ReceiveMessage<Stanica>(s);

                            // Dodaj nova merenja i alarme u postojeću stanicu
                            const int MAX_MERENJA = 3;

                            var merenja = _mapaStanica[s].Merenja;
                            merenja.AddRange(azuriranaStanica.Merenja);

                            // Ograniči listu na poslednjih MAX_MERENJA
                            if (merenja.Count > MAX_MERENJA)
                            {
                                int removeCount = merenja.Count - MAX_MERENJA;
                                merenja.RemoveRange(0, removeCount);
                            }
                            _mapaStanica[s].AktivniAlarmi.AddRange(azuriranaStanica.AktivniAlarmi);
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Stanica {_mapaStanica[s].Naziv} se odjavila.");
                        _mapaStanica.Remove(s);
                        _povezaneStanice.Remove(s);
                        s.Close();
                    }
                }

                // Prikaz statusa mreže
                PrikaziStatusMreze();

                Thread.Sleep(500); // osvežavanje svakog sekunda
            }
        }

        private static void PrikaziStatusMreze()
        {
            Console.Clear();
            Console.WriteLine("\n=== STATUS METEOROLOŠKE MREŽE ===\n");
            Console.WriteLine($"Aktivne stanice: {_mapaStanica.Count}\n");

            foreach (var stanica in _mapaStanica.Values)
            {
                Console.WriteLine($"=== {stanica.Naziv} ===");
                Console.WriteLine($"Lokacija: {stanica.Koordinate.Sirina:F2}°N, {stanica.Koordinate.Duzina:F2}°E");
                Console.WriteLine($"Broj stanovnika: {stanica.BrojStanovnika:N0}");
                Console.WriteLine($"Broj uređaja: {stanica.BrojUredjaja}");
                Console.WriteLine("\nNedavna merenja:");
                Console.WriteLine("Uređaj\tTip\tVrednost\tJedinica\tVreme");

                foreach (var m in stanica.Merenja)
                {
                    Console.WriteLine($"{m.IdUredjaja}\t{m.Tip}\t{m.Vrednost:F1}\t{m.Jedinica}\t\t{m.Vreme:HH:mm:ss}");
                }

                var poslednjiAlarm = stanica.AktivniAlarmi.LastOrDefault();
                if (poslednjiAlarm != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n!!! POSLEDNJI AKTIVNI ALARM !!!");
                    Console.WriteLine($"[{poslednjiAlarm.Vreme:HH:mm:ss}] {poslednjiAlarm.Uzrok} ({poslednjiAlarm.Vrednost})");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
        }
    }
}
