import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../authentication/data/auth_repository.dart';
import '../../providers/data/provider_repository.dart';
import '../../requests/data/request_repository.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;
    final profile = isProvider ? ref.watch(myProviderProfileProvider) : null;
    final jobs = isCustomer ? ref.watch(myJobsProvider) : null;
    final received = isProvider ? ref.watch(receivedReviewsProvider) : null;

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(currentUserProvider);
          if (isProvider) {
            ref.invalidate(myProviderProfileProvider);
            ref.invalidate(receivedReviewsProvider);
          }
          if (isCustomer) {
            ref.invalidate(myJobsProvider);
          }
        },
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text(
              user == null ? 'Signed in' : '${user.firstName} ${user.lastName}'.trim(),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            if (user?.email != null) ...[
              const SizedBox(height: 4),
              Text(user!.email),
            ],
            const SizedBox(height: 16),
            if (isCustomer)
              jobs!.when(
                loading: () => const LinearProgressIndicator(),
                error: (_, __) => const Text('Unable to load request history.'),
                data: (items) {
                  final history = items.where((job) => !job.isOpen).length;
                  final pendingReview = items.where((job) => job.canReview).length;
                  return Text(
                    pendingReview > 0
                        ? '$history past request${history == 1 ? '' : 's'}. $pendingReview waiting for a rating on Requests.'
                        : '$history past request${history == 1 ? '' : 's'}. Open Requests to track or rate them.',
                  );
                },
              ),
            if (isProvider) ...[
              profile!.when(
                loading: () => const LinearProgressIndicator(),
                error: (_, __) => const Text('Unable to load your provider rating.'),
                data: (item) => Text(
                  item.reviewCount == 0
                      ? 'No customer ratings yet. Ratings appear after completed jobs.'
                      : 'Customer rating ${item.rating.toStringAsFixed(1)} from ${item.reviewCount} review${item.reviewCount == 1 ? '' : 's'}.',
                ),
              ),
              const SizedBox(height: 12),
              received!.when(
                loading: () => const SizedBox.shrink(),
                error: (_, __) => const SizedBox.shrink(),
                data: (items) {
                  if (items.isEmpty) {
                    return const SizedBox.shrink();
                  }
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Recent reviews', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      for (final review in items.take(5))
                        ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(
                            '${review.rating}/5${review.serviceName == null ? '' : ' · ${review.serviceName}'}',
                          ),
                          subtitle: Text(review.comment ?? 'No comment'),
                        ),
                    ],
                  );
                },
              ),
            ],
            const SizedBox(height: 24),
            OutlinedButton(
              onPressed: () async {
                await ref.read(authRepositoryProvider).logout();
                ref.invalidate(currentUserProvider);
                if (context.mounted) {
                  context.go('/login');
                }
              },
              child: const Text('Sign out'),
            ),
          ],
        ),
      ),
    );
  }
}
