import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../authentication/data/auth_repository.dart';
import '../../notifications/data/notification_models.dart';
import '../../notifications/data/notification_repository.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;
    final notifications = user == null ? const AsyncValue.data(<AppNotification>[]) : ref.watch(notificationsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Home')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(notificationsProvider),
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Text(
              'Welcome',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              isProvider
                  ? 'Incoming work is on Requests. Accept, start, and complete jobs there. Approval status is on Application.'
                  : isCustomer
                      ? 'Open Services, pick a type, and tap Request. Track status and rate completed jobs on Requests. Updates appear below.'
                      : 'Browse the catalog. Customers request a service from Services. Providers use Requests and Application.',
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: () => context.go('/services'),
              icon: const Icon(Icons.handyman_outlined),
              label: Text(isCustomer ? 'Request a service' : 'Browse services'),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: () => context.go('/jobs'),
              icon: const Icon(Icons.assignment_outlined),
              label: const Text('Requests'),
            ),
            const SizedBox(height: 24),
            Text('Updates', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            notifications.when(
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (_, __) => const Text('Unable to load updates. Pull to refresh.'),
              data: (items) {
                if (items.isEmpty) {
                  return const Text('No updates yet.');
                }

                return Column(
                  children: [
                    for (final item in items.take(8))
                      ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Icon(
                          item.isRead ? Icons.notifications_none : Icons.notifications_active_outlined,
                        ),
                        title: Text(item.title),
                        subtitle: Text(item.body),
                        onTap: item.isRead
                            ? () => context.go('/jobs')
                            : () async {
                                await ref.read(notificationRepositoryProvider).markRead(item.id);
                                ref.invalidate(notificationsProvider);
                                if (context.mounted) {
                                  context.go('/jobs');
                                }
                              },
                      ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
