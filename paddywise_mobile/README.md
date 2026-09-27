# PaddyWise Mobile

Flutter mobile client for the PaddyWise smart paddy farming management system.

## Backend API Configuration

The mobile app connects to the ASP.NET Core backend API (`http://<host>:5164/api`).

The API Base URL is configurable at build and run time via `--dart-define=API_BASE_URL=...` (defined in [`lib/core/api/api_constants.dart`](lib/core/api/api_constants.dart)).

### 1. Running on Android Emulator
The Android emulator routes `10.0.2.2` to the host computer's loopback interface (`localhost`). Running with no flags uses this default:

```bash
flutter run
# OR on Windows:
scripts\run_emulator.bat
```

### 2. Running on a Physical Phone (Wi-Fi)
When testing on a physical phone connected to the same Wi-Fi as your development machine, pass your machine's local LAN IP:

```bash
flutter run --dart-define=API_BASE_URL=http://192.168.8.140:5164/api
# OR on Windows:
scripts\run_real_device.bat
```

> **Note:** `192.168.8.140` is your laptop's local LAN IP on your current Wi-Fi network. If your network changes or you reconnect to a different Wi-Fi, run `ipconfig` (Windows) or `ifconfig` / `ip a` (Mac/Linux) to get your new IP address and update the parameter:
> ```bash
> scripts\run_real_device.bat <NEW_IP_ADDRESS>
> ```
