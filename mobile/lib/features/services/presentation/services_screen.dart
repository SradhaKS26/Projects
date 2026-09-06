import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../data/catalog_repository.dart';

class ServicesScreen extends ConsumerWidget {
  const ServicesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(serviceCategoriesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Services')),
      body: categories.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _CatalogError(
          message: 'Unable to load service categories.',
          onRetry: () => ref.invalidate(serviceCategoriesProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return const _CatalogEmpty(
              message: 'No service categories are available yet.',
            );
          }

          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(serviceCategoriesProvider),
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                final category = items[index];

                return Card(
                  clipBehavior: Clip.antiAlias,
                  child: ListTile(
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 16,
                      vertical: 8,
                    ),
                    leading: CircleAvatar(
                      child: Text(category.name.substring(0, 1).toUpperCase()),
                    ),
                    title: Text(category.name),
                    subtitle: Text(
                      category.description ??
                          '${category.serviceCount} service(s)',
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.go(
                      '/services/${category.id}?name=${Uri.encodeComponent(category.name)}',
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class CategoryServicesScreen extends ConsumerWidget {
  const CategoryServicesScreen({
    super.key,
    required this.categoryId,
    required this.categoryName,
  });

  final String categoryId;
  final String categoryName;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final services = ref.watch(servicesProvider(categoryId));

    return Scaffold(
      appBar: AppBar(title: Text(categoryName)),
      body: services.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _CatalogError(
          message: 'Unable to load services.',
          onRetry: () => ref.invalidate(servicesProvider(categoryId)),
        ),
        data: (items) {
          if (items.isEmpty) {
            return const _CatalogEmpty(
              message: 'No services in this category yet.',
            );
          }

          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(servicesProvider(categoryId)),
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                final service = items[index];
                final duration = service.estimatedDurationMinutes;

                return Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                service.name,
                                style:
                                    Theme.of(context).textTheme.titleMedium,
                              ),
                            ),
                            Text(
                              '\$${service.basePrice.toStringAsFixed(2)}',
                              style: Theme.of(context)
                                  .textTheme
                                  .titleMedium
                                  ?.copyWith(
                                    color:
                                        Theme.of(context).colorScheme.primary,
                                  ),
                            ),
                          ],
                        ),
                        if (service.description != null) ...[
                          const SizedBox(height: 8),
                          Text(service.description!),
                        ],
                        if (duration != null) ...[
                          const SizedBox(height: 12),
                          Row(
                            children: [
                              const Icon(Icons.schedule, size: 16),
                              const SizedBox(width: 6),
                              Text('About $duration min'),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class _CatalogError extends StatelessWidget {
  const _CatalogError({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            FilledButton(onPressed: onRetry, child: const Text('Retry')),
          ],
        ),
      ),
    );
  }
}

class _CatalogEmpty extends StatelessWidget {
  const _CatalogEmpty({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(message, textAlign: TextAlign.center),
      ),
    );
  }
}
