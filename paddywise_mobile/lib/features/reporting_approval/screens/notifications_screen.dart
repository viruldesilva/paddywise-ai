import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../../../services/auth_service.dart';
import '../../../theme/app_theme.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  bool _isLoading = false;
  List<Map<String, dynamic>> _notifications = [];

  static final List<Map<String, dynamic>> _defaultFallbackNotifications = [
    {
      'id': 0,
      'title': 'Plan Review Update',
      'desc': 'Officer Dr. Nilmini Perera approved your Tillering stage plan.',
      'time': '2 hours ago',
      'icon': Icons.check_circle_outline,
      'color': AppColors.shoot,
      'unread': true,
    },
    {
      'id': -1,
      'title': 'Pest Warning: Brown Plant Hopper',
      'desc': 'Elevated hopper activity reported in Medirigiriya division.',
      'time': 'Yesterday',
      'icon': Icons.warning_amber_rounded,
      'color': AppColors.goldDeep,
      'unread': false,
    },
    {
      'id': -2,
      'title': 'Weather Advisory: Intermittent Showers',
      'desc': 'Heavy rain predicted over the weekend. Check paddy drainage bunds.',
      'time': '3 days ago',
      'icon': Icons.cloud_outlined,
      'color': AppColors.forest,
      'unread': false,
    },
  ];

  @override
  void initState() {
    super.initState();
    _fetchNotifications();
  }

  Future<void> _fetchNotifications() async {
    setState(() => _isLoading = true);

    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/notifications');

    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(const Duration(seconds: 4));

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body) as List<dynamic>;
        if (data.isNotEmpty) {
          final mapped = data.map<Map<String, dynamic>>((raw) {
            final item = raw as Map<String, dynamic>;
            final title = item['title'] as String? ?? 'Notification';
            final isPlan = title.toLowerCase().contains('plan');
            final isRead = item['isRead'] as bool? ?? false;
            final createdAt = DateTime.tryParse(item['createdAt'] as String? ?? '');

            return {
              'id': item['id'] as int? ?? 0,
              'title': title,
              'desc': item['message'] as String? ?? '',
              'time': _formatRelativeTime(createdAt),
              'icon': isPlan ? Icons.agriculture_rounded : Icons.bug_report_outlined,
              'color': isPlan ? AppColors.shoot : AppColors.goldDeep,
              'unread': !isRead,
            };
          }).toList();

          if (mounted) {
            setState(() {
              _notifications = mapped;
              _isLoading = false;
            });
            return;
          }
        }
      }
    } catch (_) {
      // Fall through to fallback
    }

    if (mounted) {
      setState(() {
        _notifications = _defaultFallbackNotifications;
        _isLoading = false;
      });
    }
  }

  String _formatRelativeTime(DateTime? date) {
    if (date == null) return 'Recent';
    final diff = DateTime.now().toUtc().difference(date.toUtc());
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    if (diff.inDays < 7) return '${diff.inDays}d ago';
    return '${date.day}/${date.month}/${date.year}';
  }

  Future<void> _markAsRead(int id, int index) async {
    if (id <= 0) {
      setState(() {
        _notifications[index]['unread'] = false;
      });
      return;
    }

    setState(() {
      _notifications[index]['unread'] = false;
    });

    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/notifications/$id/read');

    try {
      await http.patch(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(const Duration(seconds: 3));
    } catch (_) {}
  }

  void _showNotificationDetail(Map<String, dynamic> item, int index) {
    _markAsRead(item['id'] as int, index);

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundColor: (item['color'] as Color).withAlpha(30),
                  child: Icon(
                    item['icon'] as IconData,
                    size: 22,
                    color: item['color'] as Color,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item['title'] as String,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                        ),
                      ),
                      Text(
                        item['time'] as String,
                        style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Divider(),
            const SizedBox(height: 12),
            Text(
              item['desc'] as String,
              style: const TextStyle(
                fontSize: 14,
                height: 1.5,
                color: AppColors.ink,
              ),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Close'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      body: RefreshIndicator(
        onRefresh: _fetchNotifications,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
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
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Notifications',
                    style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                      fontFamily: 'serif',
                    ),
                  ),
                  if (_isLoading)
                    const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    ),
                ],
              ),
              const SizedBox(height: 16),
              if (_notifications.isEmpty && !_isLoading)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.symmetric(vertical: 40),
                    child: Text(
                      'No notifications yet.',
                      style: TextStyle(color: AppColors.inkSoft),
                    ),
                  ),
                )
              else
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _notifications.length,
                  separatorBuilder: (ctx, i) => const SizedBox(height: 10),
                  itemBuilder: (ctx, i) {
                    final item = _notifications[i];
                    final isUnread = item['unread'] as bool;
                    return InkWell(
                      borderRadius: BorderRadius.circular(10),
                      onTap: () => _showNotificationDetail(item, i),
                      child: Container(
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
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
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
                      ),
                    );
                  },
                ),
            ],
          ),
        ),
      ),
    );
  }
}
