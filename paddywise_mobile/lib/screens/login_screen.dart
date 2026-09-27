import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../theme/app_theme.dart';
import '../services/auth_service.dart';

/// Kumburu vector logo icon
class KumburuLogoIcon extends StatelessWidget {
  final double size;
  final Color color;

  const KumburuLogoIcon({
    super.key,
    this.size = 28,
    this.color = AppColors.shoot,
  });

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      size: Size(size, size),
      painter: _KumburuLogoPainter(color: color),
    );
  }
}

class _KumburuLogoPainter extends CustomPainter {
  final Color color;

  _KumburuLogoPainter({required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 2.0 * (size.width / 24.0)
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;

    final sx = size.width / 24.0;
    final sy = size.height / 24.0;

    // Stem: M12 22v-8
    final stem = Path()
      ..moveTo(12 * sx, 22 * sy)
      ..lineTo(12 * sx, 14 * sy);
    canvas.drawPath(stem, paint);

    // Left leaf: M12 14c-3-2-6-3-6-6 0-3 3-4 6-4
    final leftLeaf = Path()
      ..moveTo(12 * sx, 14 * sy)
      ..cubicTo(9 * sx, 12 * sy, 6 * sx, 11 * sy, 6 * sx, 8 * sy)
      ..cubicTo(6 * sx, 5 * sy, 9 * sx, 4 * sy, 12 * sx, 4 * sy);
    canvas.drawPath(leftLeaf, paint);

    // Right leaf: M12 14c3-2 6-3 6-6 0-3-3-4-6-4
    final rightLeaf = Path()
      ..moveTo(12 * sx, 14 * sy)
      ..cubicTo(15 * sx, 12 * sy, 18 * sx, 11 * sy, 18 * sx, 8 * sy)
      ..cubicTo(18 * sx, 5 * sy, 15 * sx, 4 * sy, 12 * sx, 4 * sy);
    canvas.drawPath(rightLeaf, paint);

    // Central vein: M12 4v10
    final centerVein = Path()
      ..moveTo(12 * sx, 4 * sy)
      ..lineTo(12 * sx, 14 * sy);
    canvas.drawPath(centerVein, paint);
  }

  @override
  bool shouldRepaint(covariant _KumburuLogoPainter oldDelegate) =>
      oldDelegate.color != color;
}

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin({String? overrideEmail, String? overridePass}) async {
    final email = (overrideEmail ?? _emailController.text).trim();
    final password = overridePass ?? _passwordController.text;

    if (email.isEmpty || password.isEmpty) {
      setState(() {
        _errorMessage = 'Please enter both your email and password.';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      await AuthService.login(
        email: email,
        password: password,
      );

      if (!mounted) return;
      context.go('/dashboard');
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceAll('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }


  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SizedBox(height: 12),

                  // Mobile App Branding
                  Center(
                    child: Container(
                      width: 68,
                      height: 68,
                      decoration: BoxDecoration(
                        color: AppColors.forest,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.forest.withAlpha(50),
                            blurRadius: 16,
                            offset: const Offset(0, 6),
                          ),
                        ],
                      ),
                      child: const Center(
                        child: KumburuLogoIcon(size: 34, color: AppColors.shoot),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Brand name & tagline
                  const Text(
                    'Kumburu',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontFamily: 'serif',
                      fontSize: 30,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                      letterSpacing: -0.5,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Paddy field, connected',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 14,
                      color: AppColors.inkSoft,
                      fontWeight: FontWeight.w400,
                    ),
                  ),
                  const SizedBox(height: 36),

                  // Screen Title
                  const Text(
                    'Sign in',
                    style: TextStyle(
                      fontFamily: 'serif',
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Access your paddy field and cultivation tools',
                    style: TextStyle(
                      fontSize: 13.5,
                      color: AppColors.inkSoft,
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Error notification banner
                  if (_errorMessage != null) ...[
                    Builder(
                      builder: (context) {
                        final isPending = _errorMessage!.toLowerCase().contains('pending admin verification');
                        return Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: isPending ? const Color(0xFFFEF3C7) : AppColors.errorBg,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                              color: isPending ? const Color(0xFFFDE68A) : const Color(0xFFFCA5A5),
                            ),
                          ),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Icon(
                                isPending ? Icons.schedule_rounded : Icons.error_outline_rounded,
                                color: isPending ? const Color(0xFFB45309) : AppColors.errorText,
                                size: 18,
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  _errorMessage!,
                                  style: TextStyle(
                                    color: isPending ? const Color(0xFF92400E) : AppColors.errorText,
                                    fontSize: 12.5,
                                    height: 1.35,
                                    fontWeight: isPending ? FontWeight.w500 : FontWeight.normal,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Email input
                  const Text(
                    'Email Address',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.ink,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.next,
                    style: const TextStyle(fontSize: 15, color: AppColors.ink),
                    decoration: InputDecoration(
                      hintText: 'name@example.com',
                      prefixIcon: const Icon(Icons.mail_outline_rounded, size: 20),
                      filled: true,
                      fillColor: Colors.white,
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 14,
                      ),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.line),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.line),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide:
                            const BorderSide(color: AppColors.shoot, width: 2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Password input
                  const Text(
                    'Password',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.ink,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    textInputAction: TextInputAction.done,
                    onSubmitted: (_) => _handleLogin(),
                    style: const TextStyle(fontSize: 15, color: AppColors.ink),
                    decoration: InputDecoration(
                      hintText: '••••••••',
                      prefixIcon: const Icon(Icons.lock_outline_rounded, size: 20),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword
                              ? Icons.visibility_outlined
                              : Icons.visibility_off_outlined,
                          size: 20,
                          color: AppColors.inkSoft,
                        ),
                        onPressed: () {
                          setState(() {
                            _obscurePassword = !_obscurePassword;
                          });
                        },
                      ),
                      filled: true,
                      fillColor: Colors.white,
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 14,
                      ),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.line),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.line),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide:
                            const BorderSide(color: AppColors.shoot, width: 2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Sign In Button
                  SizedBox(
                    height: 50,
                    child: ElevatedButton(
                      onPressed: _isLoading ? null : () => _handleLogin(),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.gold,
                        foregroundColor: AppColors.forestDeep,
                        disabledBackgroundColor: AppColors.gold.withAlpha(160),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      child: _isLoading
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2.2,
                                color: AppColors.forestDeep,
                              ),
                            )
                          : const Text(
                              'Sign In',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                    ),
                  ),

                  // Footer link: Request access
                  Wrap(
                    alignment: WrapAlignment.center,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      const Text(
                        "Don't have an account? ",
                        style: TextStyle(
                          color: AppColors.inkSoft,
                          fontSize: 14,
                        ),
                      ),
                      GestureDetector(
                        onTap: () {
                          context.go('/register');
                        },
                        child: const Text(
                          'Request access',
                          style: TextStyle(
                            color: AppColors.forest,
                            fontWeight: FontWeight.bold,
                            decoration: TextDecoration.underline,
                            fontSize: 14,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
