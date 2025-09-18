using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Domen.Modeli;
using Domen;
namespace Server
{
    internal class CentralServerApp
    {
        private const int LISTEN_PORT = 10000;
        private static Socket _listenerSocket;
        private static List<Socket> _povezaneStanice = new List<Socket>();
        private static Dictionary<Socket, Stanica> _mapaStanica = new Dictionary<Socket, Stanica>();
        private static readonly Random _rand = new Random();

        public static void Main(string[] args)
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
            // Pokreni thread za prihvat stanica
            var listenerThread = new Thread(ObradiDolazneStanice);
            listenerThread.Start();

            // Pokreni prikaz statusa mreže
            var displayThread = new Thread(PrikaziStatusMreze);
            displayThread.Start();

            Console.WriteLine("Pritisnite Enter za izlaz...");
            Console.ReadLine();

            foreach (var socket in _povezaneStanice)
            {
                socket?.Close();
            }
            _listenerSocket?.Close();
        }

        private static void ObradiDolazneStanice()
        {
            string[] gradovi = { "Beograd", "Novi Sad", "Niš", "Kragujevac", "Subotica" };

            while (true)
            {
                try
                {
                    Socket stanicaSocket = _listenerSocket.Accept();

                    // Generiši početne podatke stanice
                    var stanica = new Stanica
                    {
                        Naziv = gradovi[_povezaneStanice.Count % gradovi.Length],
                        Koordinate = new Koordinate(
                            42.0 + _rand.NextDouble() * 3.0,   // širina
                            19.0 + _rand.NextDouble() * 5.0    // dužina
                        ),
                        BrojStanovnika = _rand.Next(50000, 2000000),
                        BrojUredjaja = _rand.Next(3, 10)
                    };

                    // Pošalji početne podatke stanice
                    NetworkHelper.SendMessage(stanicaSocket, stanica);

                    lock (_mapaStanica)
                    {
                        _povezaneStanice.Add(stanicaSocket);
                        _mapaStanica.Add(stanicaSocket, stanica);
                    }

                    // Pokreni thread za primanje podataka sa stanice
                    var stanicaThread = new Thread(() => PrimiPodatkeStanice(stanicaSocket));
                    stanicaThread.Start();

                    Console.WriteLine($"Nova stanica povezana: {stanica.Naziv}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška prilikom prihvata stanice: {ex.Message}");
                }
            }
        }

        private static void PrimiPodatkeStanice(Socket stanicaSocket)
        {
            while (true)
            {
                try
                {
                    var azuriranaStanica = NetworkHelper.ReceiveMessage<Stanica>(stanicaSocket);

                    lock (_mapaStanica)
                    {
                        if (_mapaStanica.ContainsKey(stanicaSocket))
                        {
                            // Dodaj nova merenja u postojeću listu
                            _mapaStanica[stanicaSocket].Merenja.AddRange(azuriranaStanica.Merenja);
                            _mapaStanica[stanicaSocket].AktivniAlarmi.AddRange(azuriranaStanica.AktivniAlarmi);
                        }
                        else
                        {
                            // Nova stanica
                            _mapaStanica[stanicaSocket] = azuriranaStanica;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greška prilikom primanja podataka stanice: {ex.Message}");
                    break;
                }
            }

            // Čišćenje nakon odjave stanice
            lock (_mapaStanica)
            {
                _mapaStanica.Remove(stanicaSocket);
                _povezaneStanice.Remove(stanicaSocket);
            }
            stanicaSocket.Close();
        }

        private static void PrikaziStatusMreze()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n=== STATUS METEOROLOŠKE MREŽE ===\n");
                Console.WriteLine($"Aktivne stanice: {_mapaStanica.Count}\n");

                lock (_mapaStanica)
                {
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
                            Console.WriteLine($"{m.IdUredjaja}\t{m.Tip}\t{m.Vrednost:F1}\t{m.Jedinica}\t{m.Vreme:HH:mm:ss}");
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

                Thread.Sleep(1000); // osvežavanje svakog sekunda
            }
        }
    }
}
