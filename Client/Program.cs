using System;
using System.Diagnostics;
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
        private static string ServerIP = "DEINE_SERVER_IP"; 
        private const int VpnPort = 1194;
        private static byte[] SessionKey = System.Text.Encoding.UTF8.GetBytes("PANDORA_SECRET_KEY_32_CHARS_LONG");
        private static string OriginalGateway = "";

        static async Task Main(string[] args)
        {
            Console.Title = "pandoraVPN Client - Secure Full Tunnel";
            if (args.Length > 0) ServerIP = args[0];

            // 1. Original Gateway finden (um später alles zurückzusetzen)
            OriginalGateway = GetDefaultGateway();

            try 
            {
                using var adapter = WintunAdapter.Create("pandoraClient", "Wintun");
                adapter.SetIP("10.8.0.2", "255.255.255.0");
                
                // 2. Routing setzen: Alles über den VPN-Tunnel leiten
                SetupRouting(ServerIP);

                using var clientSocket = new UdpClient();
                var serverEP = new IPEndPoint(IPAddress.Parse(ServerIP), VpnPort);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[CONNECTED] Tunnel aktiv. Traffic wird über {ServerIP} geleitet.");
                Console.ResetColor();

                // Empfangs-Loop
                _ = Task.Run(async () => {
                    while (true) {
                        var received = await clientSocket.ReceiveAsync();
                        byte[] decrypted = Crypto(received.Buffer, SessionKey, false);
                        if (decrypted.Length > 0) adapter.SendPacket(decrypted);
                    }
                });

                // Sende-Loop
                while (true)
                {
                    byte[] packet = adapter.ReceivePacket();
                    if (packet != null)
                    {
                        byte[] encrypted = Crypto(packet, SessionKey, true);
                        await clientSocket.SendAsync(encrypted, encrypted.Length, serverEP);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                CleanupRouting();
            }
        }

        private static void SetupRouting(string vpnServerIp)
        {
            // A) Route zum VPN-Server selbst über das echte Internet behalten
            RunCmd($"route add {vpnServerIp} {OriginalGateway} metric 1");
            
            // B) Standard-Gateway auf das VPN-Interface umstellen (Full Tunnel)
            RunCmd($"route delete 0.0.0.0 mask 0.0.0.0 {OriginalGateway}");
            RunCmd($"route add 0.0.0.0 mask 0.0.0.0 10.8.0.1 metric 1");
            
            // C) DNS auf Google setzen (verhindert DNS-Leaks)
            RunCmd("netsh interface ip set dns name=\"pandoraClient\" static 8.8.8.8");
        }

        private static void CleanupRouting()
        {
            Console.WriteLine("[CLEANUP] Setze Netzwerk-Routen zurück...");
            RunCmd($"route delete 0.0.0.0");
            RunCmd($"route add 0.0.0.0 mask 0.0.0.0 {OriginalGateway} metric 20");
        }

        private static string GetDefaultGateway()
        {
            // Einfacher Weg, um das aktuelle Standard-Gateway zu finden
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c chcp 437 & netstat -rn | findstr \"0.0.0.0.*0.0.0.0\"";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            var parts = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 2 ? parts[2] : "";
        }

        private static void RunCmd(string args) => Process.Start(new ProcessStartInfo("cmd.exe", "/c " + args) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }).WaitForExit();

        private static byte[] Crypto(byte[] data, byte[] key, bool encrypt)
        {
            try {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, new byte[12]);
                cipher.Init(encrypt, parameters);
                byte[] output = new byte[cipher.GetOutputSize(data.Length)];
                int len = cipher.ProcessBytes(data, 0, data.Length, output, 0);
                cipher.DoFinal(output, len);
                return output;
            } catch { return Array.Empty<byte>(); }
        }
    }
}
