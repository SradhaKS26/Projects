import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../authentication/data/auth_repository.dart';
import '../data/request_models.dart';
import '../data/request_repository.dart';

class JobsScreen extends ConsumerWidget {
  const JobsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;

    return Scaffold(
      appBar: AppBar(title: const Text('Requests')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(myJobsProvider);
          ref.invalidate(inboxJobsProvider);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (isProvider) ...[
              Text('Incoming requests', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              const Text(
                'You only receive jobs when you are approved, active, available, and registered for that service. Contacts appear after you accept.',
              ),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(inboxJobsProvider),
                emptyText: 'No open jobs right now. Pull to refresh.',
                onAccept: (job) async {
                  try {
                    await ref.read(requestRepositoryProvider).accept(job.id);
                    ref.invalidate(inboxJobsProvider);
                    ref.invalidate(myJobsProvider);
                  } catch (_) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Could not accept. It may have been taken or expired.')),
                      );
                    }
                  }
                },
              ),
              const SizedBox(height: 24),
            ],
            if (isCustomer) ...[
              Text('Your requests', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(myJobsProvider),
                emptyText: 'You have not placed a request yet. Open Services and tap Request.',
                onCancel: (job) async {
                  try {
                    await ref.read(requestRepositoryProvider).cancel(job.id);
                    ref.invalidate(myJobsProvider);
                  } catch (_) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('This request can no longer be cancelled.')),
                      );
                    }
                  }
                },
              ),
            ],
            if (!isProvider && !isCustomer)
              const Text('Sign in as a customer or provider to see requests.'),
          ],
        ),
      ),
    );
  }
}

class _JobSection extends StatelessWidget {
  const _JobSection({
    required this.asyncJobs,
    required this.emptyText,
    this.onAccept,
    this.onCancel,
  });

  final AsyncValue<List<ServiceJob>> asyncJobs;
  final String emptyText;
  final Future<void> Function(ServiceJob job)? onAccept;
  final Future<void> Function(ServiceJob job)? onCancel;

  @override
  Widget build(BuildContext context) {
    return asyncJobs.when(
      loading: () => const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: Center(child: CircularProgressIndicator()),
      ),
      error: (_, __) => const Padding(
        padding: EdgeInsets.symmetric(vertical: 12),
        child: Text('Unable to load jobs.'),
      ),
      data: (jobs) {
        if (jobs.isEmpty) {
          return Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Text(emptyText),
          );
        }

        return Column(
          children: [
            for (final job in jobs) _JobCard(job: job, onAccept: onAccept, onCancel: onCancel),
          ],
        );
      },
    );
  }
}

class _JobCard extends StatelessWidget {
  const _JobCard({required this.job, this.onAccept, this.onCancel});

  final ServiceJob job;
  final Future<void> Function(ServiceJob job)? onAccept;
  final Future<void> Function(ServiceJob job)? onCancel;

  @override
  Widget build(BuildContext context) {
    final remaining = job.expiresAt.toLocal().difference(DateTime.now());
    final expiryLabel = job.isPending && remaining.isNegative
        ? 'Expired'
        : job.isPending
            ? '${remaining.inMinutes} min left'
            : job.status;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${job.categoryName} · ${job.serviceName}', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text('\$${job.price.toStringAsFixed(2)} · $expiryLabel'),
            if (job.notes != null) ...[
              const SizedBox(height: 8),
              Text(job.notes!),
            ],
            if (job.address != null) ...[
              const SizedBox(height: 8),
              Text(job.address!.summary),
            ],
            if (job.customer != null) ...[
              const SizedBox(height: 8),
              Text('Customer: ${job.customer!.displayName}'),
              if (job.customer!.email != null) Text(job.customer!.email!),
              if (job.customer!.phoneNumber != null) Text(job.customer!.phoneNumber!),
            ],
            if (job.provider != null) ...[
              const SizedBox(height: 8),
              Text('Provider: ${job.provider!.displayName}'),
              if (job.provider!.email != null) Text(job.provider!.email!),
              if (job.provider!.phoneNumber != null) Text(job.provider!.phoneNumber!),
            ],
            if (job.isPending && onAccept != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: () => onAccept!(job), child: const Text('Accept')),
            ],
            if (job.isPending && onCancel != null) ...[
              const SizedBox(height: 12),
              OutlinedButton(onPressed: () => onCancel!(job), child: const Text('Cancel request')),
            ],
          ],
        ),
      ),
    );
  }
}
