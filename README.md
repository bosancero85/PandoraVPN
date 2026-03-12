# 🛡️ pandoraVPN 🛡️

Ein hochperformantes, Blockchain-freies VPN für Windows Server, basierend auf dem Wintun-Treiber und AES-256-GCM Verschlüsselung.

## Features
- **Blockchain-free:** Keine Token, keine Miner, reine Punkt-zu-Punkt Verbindung.
- **Wintun Interface:** Nutzt den modernsten Layer-3-Treiber für maximale Geschwindigkeit unter Windows.
- **RSA & AES-GCM:** Hybride Verschlüsselung (Zertifikatsbasierter Handshake + schneller Datentunnel).
- **IP-Rotation:** Automatische Rotation der internen VPN-IPs zur Erhöhung der Anonymität.
- **Windows Server Native:** Optimiert für Windows Routing & Remote Access (RRAS).

## Voraussetzungen
- Windows Server 2019/2022/2025
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Wintun.dll](https://www.wintun.net/) (muss im Ausführungsverzeichnis liegen)

## Installation
1. Repository klonen.
2. `dotnet build -c Release`
3. `wintun.dll` in den `/bin/Release/net8.0-windows/` Ordner kopieren.
4. `setup_routing.ps1` als Administrator ausführen.
5. Server starten: `pandoraVPN.exe`

## Sicherheitshinweis
Dieses Projekt dient Bildungszwecken. Stellen Sie sicher, dass Sie die Firewall-Regeln (UDP 1194) korrekt konfiguriert haben.
