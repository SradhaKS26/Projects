import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../authentication/data/auth_repository.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    final isProvider = user?.isProvider == true;
    final isCustomer = user?.isCustomer == true;

    return Scaffold(
      appBar: AppBar(title: const Text('Home')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Welcome',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              isProvider
                  ? 'Browse the catalog as usual. Incoming work is on Requests. Your approval status is on Application.'
                  : isCustomer
                      ? 'Open Services, pick a type, and tap Request. Your requests are on the Requests tab.'
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
          ],
        ),
      ),
    );
  }
}
