import 'package:flutter/material.dart';

import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';

/// Eyebrow, serif title and subtitle — the header every Kumburu screen uses.
class FcPageHeader extends StatelessWidget {
  final String eyebrow;
  final String title;
  final String? subtitle;
  final Widget? trailing;

  const FcPageHeader({
    super.key,
    required this.eyebrow,
    required this.title,
    this.subtitle,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                eyebrow.toUpperCase(),
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.2,
                  color: AppColors.shoot,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
              if (subtitle != null) ...[
                const SizedBox(height: 6),
                Text(
                  subtitle!,
                  style: const TextStyle(fontSize: 13, color: AppColors.inkSoft),
                ),
              ],
            ],
          ),
        ),
        ?trailing,
      ],
    );
  }
}

/// White rounded card with the hairline border.
class FcCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Color color;
  final Color borderColor;

  const FcCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.onTap,
    this.color = Colors.white,
    this.borderColor = AppColors.line,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: color,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: borderColor),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(padding: padding, child: child),
      ),
    );
  }
}

/// Serif section heading with an optional action on the right.
class FcSectionTitle extends StatelessWidget {
  final String title;
  final Widget? action;

  const FcSectionTitle(this.title, {super.key, this.action});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 22, bottom: 10),
      child: Row(
        children: [
          Expanded(
            child: Text(
              title,
              style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.bold,
                color: AppColors.ink,
                fontFamily: 'serif',
              ),
            ),
          ),
          ?action,
        ],
      ),
    );
  }
}

/// Icon + label + value, used in detail cards.
class FcInfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const FcInfoRow({super.key, required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        children: [
          Icon(icon, size: 16, color: AppColors.shoot),
          const SizedBox(width: 8),
          Text(label, style: const TextStyle(fontSize: 13, color: AppColors.inkSoft)),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: AppColors.ink,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Full-area loading, error or empty state.
class FcStateView extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? message;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool isError;

  const FcStateView({
    super.key,
    required this.icon,
    required this.title,
    this.message,
    this.actionLabel,
    this.onAction,
    this.isError = false,
  });

  const FcStateView.error({
    super.key,
    required String this.message,
    required VoidCallback this.onAction,
  })  : icon = Icons.cloud_off_outlined,
        title = 'Could not load',
        actionLabel = 'Try again',
        isError = true;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 16),
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: isError ? AppColors.errorBg : AppColors.shootLight.withAlpha(90),
              shape: BoxShape.circle,
            ),
            child: Icon(icon,
                size: 28, color: isError ? AppColors.errorText : AppColors.forest),
          ),
          const SizedBox(height: 14),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: AppColors.ink,
              fontFamily: 'serif',
            ),
          ),
          if (message != null) ...[
            const SizedBox(height: 6),
            Text(
              message!,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
            ),
          ],
          if (actionLabel != null && onAction != null) ...[
            const SizedBox(height: 16),
            isError
                ? OutlinedButton(onPressed: onAction, child: Text(actionLabel!))
                : ElevatedButton(onPressed: onAction, child: Text(actionLabel!)),
          ],
        ],
      ),
    );
  }
}

/// Centered spinner used while a screen's first read is in flight.
class FcLoading extends StatelessWidget {
  final String? message;
  const FcLoading({super.key, this.message});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 60),
      child: Center(
        child: Column(
          children: [
            const CircularProgressIndicator(color: AppColors.gold),
            if (message != null) ...[
              const SizedBox(height: 14),
              Text(message!, style: const TextStyle(fontSize: 13, color: AppColors.inkSoft)),
            ],
          ],
        ),
      ),
    );
  }
}

/// Inline error / notice banner.
class FcBanner extends StatelessWidget {
  final String message;
  final IconData icon;
  final Color background;
  final Color foreground;

  const FcBanner({
    super.key,
    required this.message,
    this.icon = Icons.error_outline,
    this.background = AppColors.errorBg,
    this.foreground = AppColors.errorText,
  });

  const FcBanner.notice({super.key, required this.message, this.icon = Icons.info_outline})
      : background = const Color(0x26E1A63B),
        foreground = AppColors.ink;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(10)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: foreground),
          const SizedBox(width: 10),
          Expanded(
            child: Text(message,
                style: TextStyle(fontSize: 13, color: foreground, height: 1.35)),
          ),
        ],
      ),
    );
  }
}

/// Forest SnackBar confirming a completed action.
void showFcSnack(BuildContext context, String message) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message),
      backgroundColor: AppColors.forest,
      behavior: SnackBarBehavior.floating,
    ),
  );
}

/// Asks the farmer to confirm a destructive action.
Future<bool> confirmFc(
  BuildContext context, {
  required String title,
  required String message,
  required String confirmLabel,
}) async {
  final result = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      backgroundColor: AppColors.cream,
      title: Text(title, style: const TextStyle(fontFamily: 'serif', color: AppColors.ink)),
      content: Text(message, style: const TextStyle(color: AppColors.inkSoft)),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: const Text('Cancel', style: TextStyle(color: AppColors.inkSoft)),
        ),
        TextButton(
          onPressed: () => Navigator.of(context).pop(true),
          child: Text(confirmLabel,
              style: const TextStyle(
                  color: AppColors.errorText, fontWeight: FontWeight.bold)),
        ),
      ],
    ),
  );
  return result ?? false;
}

/// "← Label" link at the top of a drill-down screen. Pops when there is a
/// screen underneath, otherwise goes to [fallbackRoute] (e.g. after a deep link).
class FcBackLink extends StatelessWidget {
  final String label;
  final String fallbackRoute;

  const FcBackLink({super.key, required this.label, required this.fallbackRoute});

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: TextButton.icon(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.inkSoft,
          padding: const EdgeInsets.symmetric(horizontal: 4),
          visualDensity: VisualDensity.compact,
        ),
        onPressed: () =>
            context.canPop() ? context.pop() : context.go(fallbackRoute),
        icon: const Icon(Icons.arrow_back_rounded, size: 18),
        label: Text(label, style: const TextStyle(fontSize: 13)),
      ),
    );
  }
}

/// App bar for the full-screen forms that open above the shell.
PreferredSizeWidget fcFormAppBar(String title) => AppBar(
      backgroundColor: AppColors.forest,
      foregroundColor: AppColors.cream,
      title: Text(title,
          style: const TextStyle(
              fontFamily: 'serif', fontWeight: FontWeight.bold, color: AppColors.cream)),
    );

/// Uppercase label above a form control.
class FcFieldLabel extends StatelessWidget {
  final String text;
  const FcFieldLabel(this.text, {super.key});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6, top: 14),
      child: Text(
        text,
        style: const TextStyle(
          fontSize: 13,
          fontWeight: FontWeight.w600,
          color: AppColors.ink,
        ),
      ),
    );
  }
}
