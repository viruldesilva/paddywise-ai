@echo off
REM ==============================================================================
REM Run PaddyWise Mobile on a physical device over local Wi-Fi.
REM
REM NOTE: 192.168.8.140 is your laptop's current local LAN IP.
REM If your Wi-Fi reconnects or changes networks, check your IP with 'ipconfig'
REM and update this script or pass the new IP as an argument:
REM   scripts\run_real_device.bat 192.168.8.150
REM ==============================================================================

set DEFAULT_IP=192.168.8.140
set IP=%1
if "%IP%"=="" set IP=%DEFAULT_IP%

echo Connecting PaddyWise Mobile to backend at http://%IP%:5164/api ...
flutter run --dart-define=API_BASE_URL=http://%IP%:5164/api
