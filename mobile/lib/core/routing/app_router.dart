import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/authentication/data/auth_models.dart';
import '../../features/authentication/data/auth_repository.dart';
import '../../features/authentication/presentation/login_screen.dart';
import '../../features/home/presentation/home_screen.dart';
import '../../features/profile/presentation/profile_screen.dart';
import '../../features/providers/presentation/provider_application_screen.dart';
import '../../features/requests/presentation/create_request_screen.dart';
import '../../features/requests/presentation/jobs_screen.dart';
import '../../features/services/presentation/services_screen.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/login',
    routes: [
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      ShellRoute(
        builder: (context, state, child) => AppShell(child: child),
        routes: [
          GoRoute(
            path: '/home',
            builder: (context, state) => const HomeScreen(),
          ),
          GoRoute(
            path: '/services',
            builder: (context, state) => const ServicesScreen(),
            routes: [
              GoRoute(
                path: ':categoryId',
                builder: (context, state) => CategoryServicesScreen(
                  categoryId: state.pathParameters['categoryId']!,
                  categoryName: state.uri.queryParameters['name'] ?? 'Services',
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/jobs',
            builder: (context, state) => const JobsScreen(),
          ),
          GoRoute(
            path: '/request/:serviceId',
            builder: (context, state) => CreateRequestScreen(
              serviceId: state.pathParameters['serviceId']!,
              serviceName: state.uri.queryParameters['name'] ?? 'Service',
              price: double.tryParse(state.uri.queryParameters['price'] ?? '') ?? 0,
            ),
          ),
          GoRoute(
            path: '/application',
            builder: (context, state) => const ProviderApplicationScreen(),
          ),
          GoRoute(
            path: '/profile',
            builder: (context, state) => const ProfileScreen(),
          ),
        ],
      ),
    ],
  );
});

class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final location = GoRouterState.of(context).uri.toString();
    final tabs = _tabsFor(user);
    final index = _indexFor(location, tabs);

    return Scaffold(
      body: child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => context.go(tabs[value].path),
        destinations: [
          for (final tab in tabs)
            NavigationDestination(icon: Icon(tab.icon), label: tab.label),
        ],
      ),
    );
  }

  List<_Tab> _tabsFor(AuthUser? user) {
    final tabs = <_Tab>[
      const _Tab('/home', 'Home', Icons.home_outlined),
      const _Tab('/services', 'Services', Icons.handyman_outlined),
      const _Tab('/jobs', 'Requests', Icons.assignment_outlined),
    ];
    if (user?.isProvider == true) {
      tabs.add(const _Tab('/application', 'Application', Icons.badge_outlined));
    }
    tabs.add(const _Tab('/profile', 'Profile', Icons.person_outline));
    return tabs;
  }

  int _indexFor(String location, List<_Tab> tabs) {
    if (location.startsWith('/request')) {
      return tabs.indexWhere((tab) => tab.path == '/services').clamp(0, tabs.length - 1);
    }
    final match = tabs.indexWhere((tab) => location == tab.path || location.startsWith('${tab.path}/'));
    return match < 0 ? 0 : match;
  }
}

class _Tab {
  const _Tab(this.path, this.label, this.icon);

  final String path;
  final String label;
  final IconData icon;
}
