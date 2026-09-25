import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final notifications = [
      {
        'title': 'Plan Review Update',
        'desc': 'Officer Dr. Nilmini Perera approved your Tillering stage plan.',
        'time': '2 hours ago',
        'icon': Icons.check_circle_outline,
        'color': AppColors.shoot,
        'unread': true,
      },
      {
        'title': 'Pest Warning: Brown Plant Hopper',
        'desc': 'Elevated hopper activity reported in Medirigiriya division.',
        'time': 'Yesterday',
        'icon': Icons.warning_amber_rounded,
        'color': AppColors.goldDeep,
        'unread': false,
      },
      {
        'title': 'Weather Advisory: Intermittent Showers',
        'desc': 'Heavy rain predicted over the weekend. Check paddy drainage bunds.',
        'time': '3 days ago',
        'icon': Icons.cloud_outlined,
        'color': AppColors.forest,
        'unread': false,
      },
    ];

    return Scaffold(
      backgroundColor: AppColors.cream,
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'ACTIVITY FEED',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                letterSpacing: 1.2,
                color: AppColors.shoot,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'Notifications',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                color: AppColors.ink,
                fontFamily: 'serif',
              ),
            ),
            const SizedBox(height: 16),
            ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: notifications.length,
              separatorBuilder: (ctx, i) => const SizedBox(height: 10),
              itemBuilder: (ctx, i) {
                final item = notifications[i];
                final isUnread = item['unread'] as bool;
                return Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: isUnread ? Colors.white : Colors.white.withAlpha(200),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isUnread ? AppColors.shoot : AppColors.line,
                      width: isUnread ? 1.5 : 1.0,
                    ),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      CircleAvatar(
                        radius: 18,
                        backgroundColor: (item['color'] as Color).withAlpha(30),
                        child: Icon(
                          item['icon'] as IconData,
                          size: 20,
                          color: item['color'] as Color,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Expanded(
                                  child: Text(
                                    item['title'] as String,
                                    style: const TextStyle(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 14,
                                      color: AppColors.ink,
                                    ),
                                  ),
                                ),
                                Text(
                                  item['time'] as String,
                                  style: const TextStyle(
                                    fontSize: 11,
                                    color: AppColors.inkSoft,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              item['desc'] as String,
                              style: const TextStyle(
                                fontSize: 13,
                                color: AppColors.inkSoft,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
