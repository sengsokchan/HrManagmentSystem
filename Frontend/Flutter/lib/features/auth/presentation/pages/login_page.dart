import 'package:flutter/material.dart';

import '../../../../core/constants/app_colors.dart';
import '../../../../shared/widgets/brand_header.dart';
import '../providers/hr_controller.dart';
import '../widgets/demo_user_buttons.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.controller});

  final HrController controller;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  var _showAdvanced = false;

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;
    final s = controller.s;

    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            return SingleChildScrollView(
              child: ConstrainedBox(
                constraints: BoxConstraints(minHeight: constraints.maxHeight),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 520),
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          BrandHeader(strings: s),
                          const SizedBox(height: 28),
                          TextField(
                            controller: controller.email,
                            keyboardType: TextInputType.emailAddress,
                            decoration: InputDecoration(
                              labelText: s.workEmail,
                            ),
                            onSubmitted: (_) => controller.signIn(),
                          ),
                          const SizedBox(height: 12),
                          TextField(
                            controller: controller.password,
                            obscureText: true,
                            decoration: InputDecoration(
                              labelText: s.password,
                            ),
                            onSubmitted: (_) => controller.signIn(),
                          ),
                          const SizedBox(height: 18),
                          FilledButton(
                            onPressed: controller.signingIn
                                ? null
                                : controller.signIn,
                            child: Text(
                              controller.signingIn ? s.signingIn : s.signIn,
                            ),
                          ),
                          if (controller.message.isNotEmpty) ...[
                            const SizedBox(height: 12),
                            Text(
                              controller.message,
                              style: const TextStyle(color: AppColors.danger),
                            ),
                          ],
                          const SizedBox(height: 20),
                          DemoUserButtons(controller: controller),
                          const SizedBox(height: 12),
                          TextButton(
                            onPressed: () {
                              setState(() => _showAdvanced = !_showAdvanced);
                            },
                            child: Text(
                              _showAdvanced ? s.hideAdvanced : s.advancedSettings,
                            ),
                          ),
                          if (_showAdvanced) ...[
                            TextField(
                              controller: controller.apiUrl,
                              decoration: InputDecoration(
                                labelText: s.apiUrl,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
