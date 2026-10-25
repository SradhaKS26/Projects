import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../authentication/data/auth_repository.dart';
import '../../notifications/data/notification_models.dart';
import '../../notifications/data/notification_repository.dart';
import '../../services/data/catalog_models.dart';
import '../../services/data/catalog_repository.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;
    final notifications = user == null ? const AsyncValue.data(<AppNotification>[]) : ref.watch(notificationsProvider);
    final categories = isCustomer
        ? ref.watch(serviceCategoriesProvider)
        : const AsyncValue<List<ServiceCategory>>.data([]);

    return Scaffold(
      appBar: AppBar(title: const Text('Home')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(notificationsProvider);
          if (isCustomer) {
            ref.invalidate(serviceCategoriesProvider);
          }
        },
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Text(
              'Welcome',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              isCustomer && isProvider
                  ? 'Book a taxi or send a package from here. Offer to drive or deliver from Offer services — admin verifies your documents first.'
                  : isProvider
                      ? 'Taxi and delivery jobs appear on Requests when you are approved, available, and registered for that service.'
                      : isCustomer
                          ? 'Book a taxi or send a package. You can also offer those services from Offer services after admin approval.'
                          : 'Browse the catalog. One account can both book and provide.',
            ),
            const SizedBox(height: 24),
            if (isCustomer)
              categories.when(
                loading: () => const LinearProgressIndicator(),
                error: (_, __) => FilledButton.icon(
                  onPressed: () => context.go('/services'),
                  icon: const Icon(Icons.handyman_outlined),
                  label: const Text('Request a service'),
                ),
                data: (items) {
                  ServiceCategory? firstWhereOrNull(bool Function(ServiceCategory) test) {
                    for (final item in items) {
                      if (test(item)) {
                        return item;
                      }
                    }
                    return null;
                  }

                  final taxi = firstWhereOrNull((item) => item.isRide) ??
                      firstWhereOrNull((item) => item.name.toLowerCase() == 'taxi');
                  final delivery = firstWhereOrNull((item) => item.isParcel) ??
                      firstWhereOrNull((item) => item.name.toLowerCase() == 'delivery');
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (taxi != null)
                        FilledButton.icon(
                          onPressed: () => context.go(
                            '/services/${taxi.id}?name=${Uri.encodeComponent(taxi.name)}',
                          ),
                          icon: const Icon(Icons.local_taxi_outlined),
                          label: const Text('Book a taxi'),
                        ),
                      if (taxi != null && delivery != null) const SizedBox(height: 12),
                      if (delivery != null)
                        FilledButton.tonalIcon(
                          onPressed: () => context.go(
                            '/services/${delivery.id}?name=${Uri.encodeComponent(delivery.name)}',
                          ),
                          icon: const Icon(Icons.local_shipping_outlined),
                          label: const Text('Send a package'),
                        ),
                      if (taxi == null && delivery == null)
                        FilledButton.icon(
                          onPressed: () => context.go('/services'),
                          icon: const Icon(Icons.handyman_outlined),
                          label: const Text('Request a service'),
                        ),
                    ],
                  );
                },
              )
            else
              FilledButton.icon(
                onPressed: () => context.go('/services'),
                icon: const Icon(Icons.handyman_outlined),
                label: const Text('Browse services'),
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
