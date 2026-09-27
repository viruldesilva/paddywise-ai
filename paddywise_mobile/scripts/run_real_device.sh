#!/bin/bash
# ==============================================================================
# Run PaddyWise Mobile on a physical device over local Wi-Fi.
#
# NOTE: 192.168.8.140 is your laptop's current local LAN IP.
# If your Wi-Fi reconnects or changes networks, check your IP with 'ifconfig' or 'ip a'
# and update this script or pass the new IP as an argument:
#   ./scripts/run_real_device.sh 192.168.8.150
# ==============================================================================

DEFAULT_IP="192.168.8.140"
IP="${1:-$DEFAULT_IP}"

echo "Connecting PaddyWise Mobile to backend at http://${IP}:5164/api ..."
flutter run --dart-define=API_BASE_URL="http://${IP}:5164/api"
