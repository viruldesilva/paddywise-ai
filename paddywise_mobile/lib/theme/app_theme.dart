import 'package:flutter/material.dart';

class AppColors {
  static const Color cream = Color(0xFFF6F1E3);
  static const Color creamDeep = Color(0xFFEDE5CF);
  static const Color forest = Color(0xFF22392A);
  static const Color forestDeep = Color(0xFF182A1E);
  static const Color shoot = Color(0xFF7FA66C);
  static const Color shootLight = Color(0xFFB4CB9C);
  static const Color gold = Color(0xFFE1A63B);
  static const Color goldDeep = Color(0xFFC48A28);
  static const Color clay = Color(0xFFA6693F);
  static const Color ink = Color(0xFF212D1E);
  static const Color inkSoft = Color(0xFF4B5645);
  static const Color line = Color(0x24212D1E); // rgba(33,45,30,.14)
  
  static const Color errorBg = Color(0xFFFEE2E2);
  static const Color errorText = Color(0xFF991B1B);
  static const Color successBg = Color(0xFFDCFCE7);
  static const Color successText = Color(0xFF166534);

  // Role Badges
  static const Color badgeFarmerBg = Color(0x267FA66C);
  static const Color badgeFarmerText = Color(0xFF3B6B28);

  static const Color badgeOfficerBg = Color(0x2622392A);
  static const Color badgeOfficerText = Color(0xFF22392A);

  static const Color badgeBuyerBg = Color(0x26E1A63B);
  static const Color badgeBuyerText = Color(0xFF9C6C19);

  static const Color badgeAdminBg = Color(0x26A6693F);
  static const Color badgeAdminText = Color(0xFF7A3C18);
}

class AppTheme {
  static ThemeData get theme {
    return ThemeData(
      useMaterial3: true,
      scaffoldBackgroundColor: AppColors.cream,
      colorScheme: const ColorScheme.light(
        primary: AppColors.gold,
        secondary: AppColors.forest,
        surface: AppColors.cream,
        onPrimary: AppColors.forestDeep,
        onSurface: AppColors.ink,
      ),
      fontFamily: 'serif',
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.forest,
        foregroundColor: AppColors.cream,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AppColors.cream,
          fontSize: 20,
          fontWeight: FontWeight.bold,
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.gold,
          foregroundColor: AppColors.forestDeep,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(9999),
          ),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.ink,
          side: const BorderSide(color: AppColors.line),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(9999),
          ),
          textStyle: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.shoot, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.errorText),
        ),
        prefixIconColor: AppColors.inkSoft.withAlpha(160),
        hintStyle: TextStyle(
          color: AppColors.inkSoft.withAlpha(140),
          fontSize: 14,
        ),
        labelStyle: const TextStyle(
          color: AppColors.ink,
          fontSize: 14,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }
}
