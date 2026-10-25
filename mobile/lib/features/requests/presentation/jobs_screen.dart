import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_error.dart';
import '../../authentication/data/auth_repository.dart';
import '../../notifications/data/notification_repository.dart';
import '../data/request_models.dart';
import '../data/request_repository.dart';

class JobsScreen extends ConsumerWidget {
  const JobsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;

    Future<void> refresh() async {
      ref.invalidate(myJobsProvider);
      ref.invalidate(inboxJobsProvider);
      ref.invalidate(assignedJobsProvider);
      ref.invalidate(receivedReviewsProvider);
      ref.invalidate(notificationsProvider);
    }

    Future<void> run(
      Future<void> Function() action,
      String fallback,
    ) async {
      try {
        await action();
        await refresh();
      } catch (error) {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(describeApiError(error, fallback: fallback))),
          );
        }
      }
    }

    Future<void> rate(ServiceJob job) async {
      final draft = await showDialog<_ReviewDraft>(
        context: context,
        builder: (context) => _ReviewDialog(serviceName: job.serviceName),
      );
      if (draft == null) {
        return;
      }
      await run(
        () => ref.read(requestRepositoryProvider).review(
              job.id,
              rating: draft.rating,
              comment: draft.comment,
            ),
        'Could not submit this review.',
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Requests')),
      body: RefreshIndicator(
        onRefresh: refresh,
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
                onAccept: (job) => run(
                  () => ref.read(requestRepositoryProvider).accept(job.id),
                  'Could not accept. It may have been taken or expired.',
                ),
              ),
              const SizedBox(height: 24),
              Text('Your jobs', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(assignedJobsProvider).whenData(
                      (jobs) => jobs.where((job) => job.isOpen).toList(growable: false),
                    ),
                emptyText: 'No active jobs.',
                onStart: (job) => run(
                  () => ref.read(requestRepositoryProvider).start(job.id),
                  'Could not start this job.',
                ),
                onComplete: (job) => run(
                  () => ref.read(requestRepositoryProvider).complete(job.id),
                  'Could not complete this job.',
                ),
                onFail: (job) => run(
                  () => ref.read(requestRepositoryProvider).fail(job.id),
                  'Could not mark this job failed.',
                ),
              ),
              const SizedBox(height: 24),
              Text('Job history', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(assignedJobsProvider).whenData(
                      (jobs) => jobs.where((job) => !job.isOpen).toList(growable: false),
                    ),
                emptyText: 'Completed and cancelled jobs will appear here.',
              ),
              const SizedBox(height: 24),
            ],
            if (isCustomer) ...[
              Text('Your requests', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(myJobsProvider).whenData(
                      (jobs) => jobs.where((job) => job.isOpen).toList(growable: false),
                    ),
                emptyText: 'No active requests. Open Services and tap Request.',
                onCancel: (job) => run(
                  () => ref.read(requestRepositoryProvider).cancel(job.id),
                  'This request can no longer be cancelled.',
                ),
              ),
              const SizedBox(height: 24),
              Text('History', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              const Text('Completed jobs can be rated once. Failed, cancelled, and expired requests stay here for reference.'),
              const SizedBox(height: 12),
              _JobSection(
                asyncJobs: ref.watch(myJobsProvider).whenData(
                      (jobs) => jobs.where((job) => !job.isOpen).toList(growable: false),
                    ),
                emptyText: 'No request history yet.',
                onReview: rate,
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
    this.onStart,
    this.onComplete,
    this.onFail,
    this.onReview,
  });

  final AsyncValue<List<ServiceJob>> asyncJobs;
  final String emptyText;
  final Future<void> Function(ServiceJob job)? onAccept;
  final Future<void> Function(ServiceJob job)? onCancel;
  final Future<void> Function(ServiceJob job)? onStart;
  final Future<void> Function(ServiceJob job)? onComplete;
  final Future<void> Function(ServiceJob job)? onFail;
  final Future<void> Function(ServiceJob job)? onReview;

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
            for (final job in jobs)
              _JobCard(
                job: job,
                onAccept: onAccept,
                onCancel: onCancel,
                onStart: onStart,
                onComplete: onComplete,
                onFail: onFail,
                onReview: onReview,
              ),
          ],
        );
      },
    );
  }
}

class _JobCard extends StatelessWidget {
  const _JobCard({
    required this.job,
    this.onAccept,
    this.onCancel,
    this.onStart,
    this.onComplete,
    this.onFail,
    this.onReview,
  });

  final ServiceJob job;
  final Future<void> Function(ServiceJob job)? onAccept;
  final Future<void> Function(ServiceJob job)? onCancel;
  final Future<void> Function(ServiceJob job)? onStart;
  final Future<void> Function(ServiceJob job)? onComplete;
  final Future<void> Function(ServiceJob job)? onFail;
  final Future<void> Function(ServiceJob job)? onReview;

  @override
  Widget build(BuildContext context) {
    final remaining = job.expiresAt.toLocal().difference(DateTime.now());
    final expiryLabel = job.isPending && remaining.isNegative
        ? 'Expired'
        : job.isPending
            ? '${remaining.inMinutes} min left'
            : job.statusLabel;

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
              if (job.providerRating != null && job.providerReviewCount > 0)
                Text(
                  '${job.providerRating!.toStringAsFixed(1)} · ${job.providerReviewCount} review${job.providerReviewCount == 1 ? '' : 's'}',
                ),
              if (job.provider!.email != null) Text(job.provider!.email!),
              if (job.provider!.phoneNumber != null) Text(job.provider!.phoneNumber!),
            ],
            if (job.timeline.isNotEmpty) ...[
              const SizedBox(height: 8),
              ExpansionTile(
                tilePadding: EdgeInsets.zero,
                title: const Text('Track status'),
                children: [
                  for (final event in job.timeline)
                    ListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.check_circle_outline, size: 20),
                      title: Text(event.label),
                      subtitle: Text(
                        [
                          event.changedAt.toLocal().toString().split('.').first,
                          if (event.notes != null) event.notes!,
                        ].join(' · '),
                      ),
                    ),
                ],
              ),
            ],
            if (job.review != null) ...[
              const SizedBox(height: 8),
              _StarRow(rating: job.review!.rating),
              if (job.review!.comment != null) Text(job.review!.comment!),
            ],
            if (job.isPending && onAccept != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: () => onAccept!(job), child: const Text('Accept')),
            ],
            if (job.isAccepted && onStart != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: () => onStart!(job), child: const Text('Start job')),
            ],
            if (job.isInProgress && onComplete != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: () => onComplete!(job), child: const Text('Complete')),
            ],
            if (job.isInProgress && onFail != null) ...[
              const SizedBox(height: 8),
              OutlinedButton(onPressed: () => onFail!(job), child: const Text('Could not complete')),
            ],
            if ((job.isPending || job.isAccepted || job.isInProgress) && onCancel != null) ...[
              const SizedBox(height: 12),
              OutlinedButton(onPressed: () => onCancel!(job), child: const Text('Cancel request')),
            ],
            if (job.canReview && onReview != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: () => onReview!(job), child: const Text('Rate provider')),
            ],
          ],
        ),
      ),
    );
  }
}

class _StarRow extends StatelessWidget {
  const _StarRow({required this.rating});

  final int rating;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        for (var i = 1; i <= 5; i++)
          Icon(
            i <= rating ? Icons.star : Icons.star_border,
            color: Colors.amber.shade700,
            size: 20,
          ),
      ],
    );
  }
}

class _ReviewDraft {
  const _ReviewDraft({required this.rating, this.comment});

  final int rating;
  final String? comment;
}

class _ReviewDialog extends StatefulWidget {
  const _ReviewDialog({required this.serviceName});

  final String serviceName;

  @override
  State<_ReviewDialog> createState() => _ReviewDialogState();
}

class _ReviewDialogState extends State<_ReviewDialog> {
  var _rating = 5;
  final _comment = TextEditingController();

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Rate ${widget.serviceName}'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              for (var i = 1; i <= 5; i++)
                IconButton(
                  onPressed: () => setState(() => _rating = i),
                  icon: Icon(
                    i <= _rating ? Icons.star : Icons.star_border,
                    color: Colors.amber.shade700,
                  ),
                ),
            ],
          ),
          TextField(
            controller: _comment,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Comment (optional)',
              border: OutlineInputBorder(),
            ),
          ),
        ],
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(
          onPressed: () => Navigator.pop(
            context,
            _ReviewDraft(
              rating: _rating,
              comment: _comment.text.trim().isEmpty ? null : _comment.text.trim(),
            ),
          ),
          child: const Text('Submit'),
        ),
      ],
    );
  }
}
