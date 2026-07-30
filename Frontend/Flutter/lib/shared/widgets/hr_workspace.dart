import 'package:flutter/material.dart';

import '../../features/attendance/presentation/pages/attendance_page.dart';
import '../../features/auth/presentation/providers/hr_controller.dart';
import '../../features/dashboard/presentation/pages/home_page.dart';
import '../../features/leave/presentation/pages/leave_page.dart';
import '../../features/profile/presentation/pages/profile_page.dart';
import '../helpers/view_helpers.dart';
import 'mobile_app_bar.dart';

class HrWorkspace extends StatefulWidget {
  const HrWorkspace({super.key, required this.controller});

  final HrController controller;

  @override
  State<HrWorkspace> createState() => _HrWorkspaceState();
}

class _HrWorkspaceState extends State<HrWorkspace> {
  late final PageController _pageController;
  var _syncingFromNav = false;

  HrController get controller => widget.controller;

  int get _currentIndex =>
      controller.views.indexOf(controller.activeView).clamp(0, controller.views.length - 1);

  @override
  void initState() {
    super.initState();
    _pageController = PageController(initialPage: _currentIndex);
    controller.addListener(_onControllerChanged);
  }

  @override
  void dispose() {
    controller.removeListener(_onControllerChanged);
    _pageController.dispose();
    super.dispose();
  }

  void _onControllerChanged() {
    if (!mounted || !_pageController.hasClients) return;
    final target = _currentIndex;
    final currentPage = _pageController.page?.round() ?? _pageController.initialPage;
    if (currentPage != target) {
      _syncingFromNav = true;
      _pageController
          .animateToPage(
            target,
            duration: const Duration(milliseconds: 280),
            curve: Curves.easeOutCubic,
          )
          .whenComplete(() => _syncingFromNav = false);
    }
    setState(() {});
  }

  Future<void> _goToTab(int index) async {
    _syncingFromNav = true;
    final view = controller.views[index];
    if (controller.activeView != view) {
      controller.selectView(view);
    }
    if (_pageController.hasClients) {
      await _pageController.animateToPage(
        index,
        duration: const Duration(milliseconds: 280),
        curve: Curves.easeOutCubic,
      );
    }
    _syncingFromNav = false;
  }

  void _onPageChanged(int index) {
    if (_syncingFromNav) return;
    final view = controller.views[index];
    if (controller.activeView != view) {
      controller.selectView(view);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = controller.s;
    final loadingShell = controller.loading &&
        controller.attendance.isEmpty &&
        controller.leaveRequests.isEmpty;

    return Scaffold(
      appBar: MobileAppBar(
        title: controller.activeViewLabel,
        subtitle: controller.subtitle,
        loading: controller.loading || controller.actionInProgress,
        onRefresh: controller.loadWorkspace,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: _goToTab,
        destinations: controller.views
            .map(
              (view) => NavigationDestination(
                icon: Icon(ViewHelpers.iconFor(view)),
                label: controller.viewLabel(view),
              ),
            )
            .toList(),
      ),
      body: Column(
        children: [
          if (controller.message.isNotEmpty)
            MaterialBanner(
              content: Text(controller.message),
              leading: const Icon(Icons.info_outline),
              actions: [
                TextButton(
                  onPressed: controller.clearMessage,
                  child: Text(s.dismiss),
                ),
              ],
            ),
          Expanded(
            child: loadingShell
                ? const Center(child: CircularProgressIndicator())
                : PageView(
                    controller: _pageController,
                    onPageChanged: _onPageChanged,
                    physics: const BouncingScrollPhysics(),
                    children: [
                      _TabPage(child: HomePage(controller: controller)),
                      _TabPage(child: AttendancePage(controller: controller)),
                      _TabPage(child: LeavePage(controller: controller)),
                      _TabPage(child: ProfilePage(controller: controller)),
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}

class _TabPage extends StatelessWidget {
  const _TabPage({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      child: child,
    );
  }
}
