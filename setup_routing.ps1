# pandoraVPN - Server Routing Setup
Write-Host "Konfiguriere Windows Networking für pandoraVPN..." -ForegroundColor Green

# IP Forwarding aktivieren
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" -Name "IPEnableRouter" -Value 1

# Firewall Regel
New-NetFirewallRule -DisplayName "pandoraVPN-Inbound" -Direction Inbound -LocalPort 1194 -Protocol UDP -Action Allow -ErrorAction SilentlyContinue

# NAT für das VPN Subnetz (10.8.0.0/24)
# Erfordert installierte 'RemoteAccess' Rolle
if (!(Get-NetNat -Name "pandoraNAT" -ErrorAction SilentlyContinue)) {
    New-NetNat -Name "pandoraNAT" -InternalIPInterfaceAddressPrefix "10.8.0.0/24"
    Write-Host "NAT-Netzwerk eingerichtet." -ForegroundColor Cyan
}

Write-Host "Setup abgeschlossen. Bitte Server neu starten!" -ForegroundColor Red
