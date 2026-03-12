using System;
using System.Net;
using System.Net.Sockets;
using Wintun;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace pandoraClient
{
    class Program
    {
        private static string ServerIP = "DEINE_SERVER_IP"; // Hier die IP deines Windows Servers eintragen
        private const int VpnPort = 1194;
        private static byte[] SessionKey = System.Text.Encoding.UTF8.GetBytes("PANDORA_SECRET_KEY_32_CHARS_LONG"); // Muss mit Server übereinstimmen

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== pandoraVPN Client v1.0 ===");

            if (args.Length > 0) ServerIP = args[0];

            try 
            {
                // 1. Wintun Adapter für den Client erstellen
                using var adapter = WintunAdapter.Create("pandoraClient", "Wintun");
                adapter.SetIP("10.8.0.2", "255.255.255.0");
                Console.WriteLine("[NET] Tunnel-Interface 10.8.0.2 aktiv.");

                // 2. UDP Verbindung zum Server
                using var clientSocket = new UdpClient();
                var serverEP = new IPEndPoint(IPAddress.Parse(ServerIP), VpnPort);

                Console.WriteLine($"[CONN] Verbinde zu pandoraVPN Server: {ServerIP}:{VpnPort}");

                // 3. Empfangs-Loop (Vom Server zurück zum PC)
                _ = Task.Run(async () => {
                    while (true) {
                        var received = await clientSocket.ReceiveAsync();
                        byte[] decrypted = Decrypt(received.Buffer, SessionKey);
                        if (decrypted.Length > 0) adapter.SendPacket(decrypted);
                    }
                });

                // 4. Sende-Loop (Vom PC zum Server)
                while (true)
                {
                    byte[] packet = adapter.ReceivePacket(); // Liest Pakete vom Windows-System
                    if (packet != null)
                    {
                        byte[] encrypted = Encrypt(packet, SessionKey);
                        await clientSocket.SendAsync(encrypted, encrypted.Length, serverEP);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Fehler: {ex.Message}");
                Console.WriteLine("Stellen Sie sicher, dass die wintun.dll im Ordner liegt und Sie Admin-Rechte haben.");
            }
        }

        private static byte[] Encrypt(byte[] data, byte[] key)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, new byte[12]);
            cipher.Init(true, parameters);
            byte[] output = new byte[cipher.GetOutputSize(data.Length)];
            int len = cipher.ProcessBytes(data, 0, data.Length, output, 0);
            cipher.DoFinal(output, len);
            return output;
        }

        private static byte[] Decrypt(byte[] data, byte[] key)
        {
            try {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, new byte[12]);
                cipher.Init(false, parameters);
                byte[] output = new byte[cipher.GetOutputSize(data.Length)];
                int len = cipher.ProcessBytes(data, 0, data.Length, output, 0);
                cipher.DoFinal(output, len);
                return output;
            } catch { return Array.Empty<byte>(); }
        }
    }
}
