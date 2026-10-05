# 📱 PaddyWise Mobile Client ("Kumburu")

The cross-platform mobile client for the **PaddyWise-AI** ecosystem, built with Flutter 3 and Dart. Designed for on-the-ground field operations by Sri Lankan paddy farmers and agricultural field officers.

---

## 🛠️ Tech Stack & Dependencies

- **Framework**: [Flutter 3](https://flutter.dev/) (Dart SDK `^3.9.2`)
- **UI Design**: Material 3 theming aligned with Kumburu brand colors (`AppTheme`)
- **Local Persistence**: `shared_preferences` for session management and cached tokens
- **Icons**: `cupertino_icons` and Material Design Icons

---

## 🚀 Getting Started

### Prerequisites

- [Flutter SDK (v3.x / Dart 3.9+)](https://docs.flutter.dev/get-started/install)
- Android Studio / Android SDK (configured for API level 34+) or Xcode for iOS development

### Running Locally

```bash
# Navigate to mobile project directory
cd paddywise_mobile

# Fetch packages
flutter pub get

# Run static analysis
flutter analyze

# Run widget tests
flutter test

# Run app on connected device or emulator
flutter run
```

### Building Release Artifacts

To generate an Android release APK or App Bundle:

```bash
# Build standalone release APK
flutter build apk --release

# Build Google Play App Bundle
flutter build appbundle --release
```

Automated Android release builds and APK uploads are managed through GitHub Actions in `.github/workflows/android-release.yml`.

---

## 📁 Project Architecture

```text
lib/
├── main.dart               # App entry point, session restoration, root widget
├── screens/
│   ├── login_screen.dart       # User authentication
│   ├── register_screen.dart    # Registration with role selection
│   └── dashboard_screen.dart   # Role-aware dashboard with quick actions
├── services/
│   └── auth_service.dart       # Authentication client & shared preferences storage
└── theme/
    └── app_theme.dart          # Earth-toned Kumburu brand theme palette
```

---

## 👥 Supported Roles in Mobile

The mobile application dynamically customizes dashboard actions and navigation depending on the authenticated user role:

- **Farmer**: Fast access to active cultivation cycles, activity logging, and symptom reporting with camera capture.
- **Agricultural / Field Officer**: Field verification tools, quick cycle review, and farmer contact registry.
