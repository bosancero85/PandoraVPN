using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Wintun;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace pandoraVPN
{
    class Program
    {
        private const int VpnPort = 1194;
        private const string CertPath = "pandora_auth.pfx";
        private static byte[] SessionKey = new byte[32]; // In Produktion via Handshake tauschen
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== pandoraVPN v1.0 - Windows Server Edition ===");

            // 1. Zertifikat-Initialisierung
            CheckAndCreateCertificate();
            var serverCert = new X509Certificate2(CertPath, "pandora123");
            Console.WriteLine($"[AUTH] Zertifikat geladen: {serverCert.Subject}");

            // 2. Wintun Adapter Setup
            using var adapter = WintunAdapter.Create("pandoraVPN", "Wintun");
            adapter.SetIP("10.8.0.1", "255.255.255.0");
            Console.WriteLine("[NET] Virtuelles Interface 10.8.0.1 ist UP.");

            // 3. UDP Socket
            using var serverSocket = new UdpClient(VpnPort);
            
            // 4. IP-Rotation Task (Alle 5 Min)
            _ = Task.Run(() => RotateInternalIP(adapter));

            Console.WriteLine($"[READY] Server lauscht auf UDP:{VpnPort}. Drücken Sie Strg+C zum Beenden.");

            // Data Loop
            while (true)
            {
                try
                {
                    var result = await serverSocket.ReceiveAsync();
                    byte[] decrypted = DecryptPacket(result.Buffer, SessionKey);
                    
                    if (decrypted.Length > 0)
                    {
                        adapter.SendPacket(decrypted);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERR] Paket-Fehler: {ex.Message}");
                }
            }
        }

        private static void RotateInternalIP(WintunAdapter adapter)
        {
            var rand = new Random();
            while (true)
            {
                Thread.Sleep(300000); // 5 Minuten
                string nextIp = $"10.8.0.{rand.Next(2, 254)}";
                adapter.SetIP(nextIp, "255.255.255.0");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[ROTATE] Interne IP rotiert auf: {nextIp}");
                Console.ResetColor();
            }
        }

        private static byte[] DecryptPacket(byte[] encryptedData, byte[] key)
        {
            try
            {
                // AES-GCM 256 Entschlüsselung
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, new byte[12]); // Statischer IV für Demo
                cipher.Init(false, parameters);
                
                byte[] output = new byte[cipher.GetOutputSize(encryptedData.Length)];
                int len = cipher.ProcessBytes(encryptedData, 0, encryptedData.Length, output, 0);
                cipher.DoFinal(output, len);
                return output;
            }
            catch { return Array.Empty<byte>(); }
        }

        private static void CheckAndCreateCertificate()
        {
            if (!File.Exists(CertPath))
            {
                Console.WriteLine("[AUTH] Erstelle neues RSA-Zertifikat...");
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest("cn=pandoraVPN-Server", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
                File.WriteAllBytes(CertPath, cert.Export(X509ContentType.Pfx, "pandora123"));
            }
        }
    }
}
