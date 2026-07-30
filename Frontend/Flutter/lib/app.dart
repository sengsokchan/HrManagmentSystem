import 'package:flutter/material.dart';

import 'core/config/app_config.dart';
import 'core/l10n/locale_controller.dart';
import 'core/network/api_client.dart';
import 'core/storage/session_storage.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/datasources/auth_remote_datasource.dart';
import 'features/auth/data/repositories/auth_repository_impl.dart';
import 'features/auth/domain/usecases/sign_in_usecase.dart';
import 'features/auth/presentation/pages/login_page.dart';
import 'features/auth/presentation/providers/hr_controller.dart';
import 'shared/widgets/hr_workspace.dart';

class HrManagementApp extends StatefulWidget {
  const HrManagementApp({super.key});

  @override
  State<HrManagementApp> createState() => _HrManagementAppState();
}

class _HrManagementAppState extends State<HrManagementApp> {
  late final LocaleController localeController;
  late final HrController controller;
  var _booting = true;

  @override
  void initState() {
    super.initState();
    localeController = LocaleController();
    final apiClient = ApiClient(AppConfig.defaultApiUrl);
    final authDataSource = AuthRemoteDataSource(apiClient);
    final authRepository = AuthRepositoryImpl(authDataSource);
    final sessionStorage = SessionStorage();

    controller = HrController(
      apiClient: apiClient,
      signInUseCase: SignInUseCase(authRepository),
      sessionStorage: sessionStorage,
      localeController: localeController,
    );

    Future.wait([localeController.load(), sessionStorage.load()]).whenComplete(() {
      if (!mounted) return;
      if (sessionStorage.token != null) {
        apiClient.configure(
          baseUrl: controller.apiUrl.text,
          token: sessionStorage.token,
        );
        controller.loadWorkspace();
      }
      setState(() => _booting = false);
    });
  }

  @override
  void dispose() {
    controller.dispose();
    localeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_booting) {
      return MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        home: const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        ),
      );
    }

    return AnimatedBuilder(
      animation: Listenable.merge([controller, localeController]),
      builder: (context, _) {
        final isKm = localeController.isKhmer;
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          title: controller.s.appName,
          theme: AppTheme.light,
          locale: Locale(isKm ? 'km' : 'en'),
          home: controller.signedIn
              ? HrWorkspace(controller: controller)
              : LoginPage(controller: controller),
        );
      },
    );
  }
}
