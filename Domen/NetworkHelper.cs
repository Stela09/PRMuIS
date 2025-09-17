using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domen
{
    public static class NetworkHelper
    {
        // Šalje objekat preko TCP soketa koristeći binarnu serijalizaciju.
        public static void SendMessage(Socket socket, object obj)
        {
            try
            {
                byte[] data = Serialize(obj);

                // Pošalji prvo dužinu poruke
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                socket.Send(lengthPrefix);

                // Zatim pošalji stvarne podatke
                socket.Send(data);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Slanje poruke nije uspelo: {ex.Message}", ex);
            }
        }

        /// Prima objekat tipa T sa TCP soketa koristeći binarnu deserializaciju.
        public static T ReceiveMessage<T>(Socket socket)
        {
            try
            {
                int messageLength = ReceiveMessageLength(socket);
                byte[] buffer = ReceiveExactBytes(socket, messageLength);

                return Deserialize<T>(buffer);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Prijem poruke nije uspeo: {ex.Message}", ex);
            }
        }

        #region PrivatneMetode

        private static byte[] Serialize(object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }

        private static int ReceiveMessageLength(Socket socket)
        {
            byte[] lengthBytes = ReceiveExactBytes(socket, 4);
            return BitConverter.ToInt32(lengthBytes, 0);
        }

        private static byte[] ReceiveExactBytes(Socket socket, int length)
        {
            byte[] buffer = new byte[length];
            int totalReceived = 0;

            while (totalReceived < length)
            {
                int received = socket.Receive(buffer, totalReceived, length - totalReceived, SocketFlags.None);
                if (received == 0)
                    throw new Exception("Veza je prekinuta sa hostom");

                totalReceived += received;
            }

            return buffer;
        }

        #endregion
    }
}
