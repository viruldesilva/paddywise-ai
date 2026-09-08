import 'package:flutter/material.dart';
import 'theme/app_theme.dart';
import 'services/auth_service.dart';
import 'screens/login_screen.dart';
import 'screens/dashboard_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await AuthService.init();
  final currentUser = AuthService.getCurrentUser();

  runApp(PaddyWiseApp(initialUser: currentUser));
}

class PaddyWiseApp extends StatelessWidget {
  final dynamic initialUser;

  const PaddyWiseApp({super.key, this.initialUser});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Kumburu - PaddyWise AI',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.theme,
      home: initialUser != null
          ? DashboardScreen(user: initialUser)
          : const LoginScreen(),
    );
  }
}
