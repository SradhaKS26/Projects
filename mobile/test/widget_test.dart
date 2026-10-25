import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:service_management_mobile/core/routing/app_router.dart';
import 'package:service_management_mobile/features/authentication/data/auth_models.dart';
import 'package:service_management_mobile/features/authentication/data/auth_repository.dart';
import 'package:service_management_mobile/features/home/presentation/home_screen.dart';
import 'package:service_management_mobile/features/notifications/data/notification_repository.dart';
import 'package:service_management_mobile/features/requests/data/request_models.dart';
import 'package:service_management_mobile/features/requests/data/request_repository.dart';
import 'package:service_management_mobile/features/requests/presentation/jobs_screen.dart';
import 'package:service_management_mobile/features/services/data/catalog_models.dart';
import 'package:service_management_mobile/features/services/data/catalog_repository.dart';
import 'package:service_management_mobile/features/services/presentation/services_screen.dart';
import 'package:service_management_mobile/main.dart';

void main() {
  testWidgets('login shell renders brand', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: ServiceManagementApp()));
    await tester.pumpAndSettle();

    expect(find.text('Service Management'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
  });

  testWidgets('pending provider keeps normal tabs plus Application', (tester) async {
    await tester.pumpWidget(
      _shell(
        user: _user(roles: const ['ServiceProvider']),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Home'), findsWidgets);
    expect(find.text('Services'), findsWidgets);
    expect(find.text('Requests'), findsWidgets);
    expect(find.text('Application'), findsWidgets);
    expect(find.text('Profile'), findsWidgets);
    expect(find.text('Browse services'), findsOneWidget);
    expect(find.textContaining('Incoming work is on Requests'), findsOneWidget);
  });

  testWidgets('customer does not see Application tab', (tester) async {
    await tester.pumpWidget(
      _shell(
        user: _user(roles: const ['CommonUser']),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Application'), findsNothing);
    expect(find.text('Requests'), findsWidgets);
  });

  testWidgets('customer sees Request on a catalog service', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          currentUserProvider.overrideWith((ref) async => _user(roles: const ['CommonUser'])),
          servicesProvider('cat1').overrideWith((ref) async => [_sedan]),
        ],
        child: const MaterialApp(
          home: CategoryServicesScreen(categoryId: 'cat1', categoryName: 'Taxi'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Sedan'), findsOneWidget);
    expect(find.text('Request'), findsOneWidget);
  });

  testWidgets('customer can rate a completed job from history', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          currentUserProvider.overrideWith((ref) async => _user(roles: const ['CommonUser'])),
          myJobsProvider.overrideWith((ref) async => [_completedJob()]),
        ],
        child: const MaterialApp(home: JobsScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('History'), findsOneWidget);
    expect(find.text('Rate provider'), findsOneWidget);
    expect(find.text('Track status'), findsNothing);
  });

  testWidgets('reviewed job shows stars instead of rate action', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          currentUserProvider.overrideWith((ref) async => _user(roles: const ['CommonUser'])),
          myJobsProvider.overrideWith((ref) async => [_completedJob(review: _review())]),
        ],
        child: const MaterialApp(home: JobsScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Rate provider'), findsNothing);
    expect(find.text('On time.'), findsOneWidget);
  });

  testWidgets('provider does not place requests from the catalog', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          currentUserProvider.overrideWith((ref) async => _user(roles: const ['ServiceProvider'])),
          servicesProvider('cat1').overrideWith((ref) async => [_sedan]),
        ],
        child: const MaterialApp(
          home: CategoryServicesScreen(categoryId: 'cat1', categoryName: 'Taxi'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Sedan'), findsOneWidget);
    expect(find.text('Request'), findsNothing);
  });
}

Widget _shell({required AuthUser user}) {
  final router = GoRouter(
    initialLocation: '/home',
    routes: [
      ShellRoute(
        builder: (context, state, child) => AppShell(child: child),
        routes: [
          GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          GoRoute(path: '/services', builder: (_, __) => const SizedBox()),
          GoRoute(path: '/jobs', builder: (_, __) => const SizedBox()),
          GoRoute(path: '/application', builder: (_, __) => const SizedBox()),
          GoRoute(path: '/profile', builder: (_, __) => const SizedBox()),
        ],
      ),
    ],
  );

  return ProviderScope(
    overrides: [
      currentUserProvider.overrideWith((ref) async => user),
      notificationsProvider.overrideWith((ref) async => const []),
    ],
    child: MaterialApp.router(routerConfig: router),
  );
}

AuthUser _user({required List<String> roles}) {
  return AuthUser(
    id: 'user-1',
    firstName: 'Pat',
    lastName: 'User',
    email: 'pat@test.local',
    roles: roles,
    permissions: const [],
  );
}

const _sedan = CatalogService(
  id: 'svc1',
  categoryId: 'cat1',
  categoryName: 'Taxi',
  name: 'Sedan',
  basePrice: 10,
  isActive: true,
);

ServiceJob _completedJob({JobReview? review}) {
  return ServiceJob(
    id: 'job-1',
    serviceId: 'svc1',
    serviceName: 'Sedan',
    categoryName: 'Taxi',
    price: 10,
    status: 'completed',
    requestedDate: DateTime(2026, 10, 25),
    expiresAt: DateTime(2026, 10, 25),
    review: review,
  );
}

JobReview _review() {
  return JobReview(
    id: 'rev-1',
    rating: 5,
    comment: 'On time.',
    createdAt: DateTime(2026, 10, 25),
  );
}
