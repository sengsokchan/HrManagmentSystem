@echo off
net session >nul 2>&1
if %errorLevel% neq 0 (
  echo Run this file as Administrator.
  pause
  exit /b 1
)

netsh advfirewall firewall delete rule name="HR Management API 5088" >nul 2>&1
netsh advfirewall firewall add rule name="HR Management API 5088" dir=in action=allow protocol=TCP localport=5088
echo Firewall rule added for TCP 5088.
pause
