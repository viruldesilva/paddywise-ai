import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../../../services/auth_service.dart';

class PlanStatusScreen extends StatelessWidget {
  const PlanStatusScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user = AuthService.getCurrentUser();

    return Scaffold(
      backgroundColor: AppColors.cream,
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'CULTIVATION ADVISORY',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                letterSpacing: 1.2,
                color: AppColors.shoot,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'Plan Status & AI Advice',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                color: AppColors.ink,
                fontFamily: 'serif',
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Real-time status of your seasonal plan approved by your Agricultural Extension Officer.',
              style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
            ),
            const SizedBox(height: 18),

            // Active Plan Card
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.line),
              ),
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Yala 2026 Season Plan',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                          fontFamily: 'serif',
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.successBg,
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: const Text(
                          'OFFICER APPROVED',
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: AppColors.successText,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Farmer: ${user?.fullName ?? 'Bandara Wanninayake'} • Plot 04 (3.5 Acres)',
                    style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Paddy Variety: Bg 352 (Nadu 3.5 Months)',
                    style: TextStyle(fontSize: 12, color: AppColors.inkSoft),
                  ),
                  const SizedBox(height: 14),

                  // Timeline / Stage
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.creamDeep.withAlpha(100),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text(
                          'Current Stage: Tillering Phase (Day 45 / 105)',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: AppColors.forest,
                          ),
                        ),
                        SizedBox(height: 6),
                        LinearProgressIndicator(
                          value: 0.43,
                          backgroundColor: Colors.white,
                          valueColor:
                              AlwaysStoppedAnimation<Color>(AppColors.shoot),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),

                  // Officer Feedback Box
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.badgeOfficerBg,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                          color: AppColors.badgeOfficerText.withAlpha(60)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Row(
                          children: [
                            Icon(Icons.verified,
                                size: 16, color: AppColors.forest),
                            SizedBox(width: 6),
                            Text(
                              'Extension Officer Note:',
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: AppColors.forest,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 4),
                        Text(
                          '"Recommended water level at 5cm for next 10 days. Apply top dressing urea dosage before next Monday."',
                          style: TextStyle(
                            fontSize: 12,
                            fontStyle: FontStyle.italic,
                            color: AppColors.ink,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
