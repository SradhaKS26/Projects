import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/networking/api_error.dart';
import '../../authentication/data/auth_repository.dart';
import '../data/request_repository.dart';

class CreateRequestScreen extends ConsumerStatefulWidget {
  const CreateRequestScreen({
    super.key,
    required this.serviceId,
    required this.serviceName,
    required this.price,
  });

  final String serviceId;
  final String serviceName;
  final double price;

  @override
  ConsumerState<CreateRequestScreen> createState() => _CreateRequestScreenState();
}

class _CreateRequestScreenState extends ConsumerState<CreateRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _line1 = TextEditingController();
  final _line2 = TextEditingController();
  final _city = TextEditingController();
  final _state = TextEditingController();
  final _postal = TextEditingController();
  final _notes = TextEditingController();
  var _saving = false;
  String? _error;

  @override
  void dispose() {
    _line1.dispose();
    _line2.dispose();
    _city.dispose();
    _state.dispose();
    _postal.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      await ref.read(requestRepositoryProvider).place(
            serviceId: widget.serviceId,
            addressLine1: _line1.text.trim(),
            addressLine2: _line2.text.trim().isEmpty ? null : _line2.text.trim(),
            city: _city.text.trim(),
            state: _state.text.trim(),
            postalCode: _postal.text.trim(),
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
          );
      if (!mounted) return;
      ref.invalidate(myJobsProvider);
      context.go('/jobs');
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = describeApiError(
          error,
          fallback: 'Unable to place this request. Check the address and try again.',
        );
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(currentUserProvider).valueOrNull;
    if (user?.isCustomer != true) {
      return Scaffold(
        appBar: AppBar(title: const Text('Request')),
        body: const Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Text('Only customers can place service requests.'),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: Text('Request ${widget.serviceName}')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              '\$${widget.price.toStringAsFixed(2)} · Eligible providers have 10 minutes to accept. Contact details stay hidden until someone accepts.',
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _line1,
              decoration: const InputDecoration(labelText: 'Address line 1'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _line2,
              decoration: const InputDecoration(labelText: 'Address line 2 (optional)'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _city,
              decoration: const InputDecoration(labelText: 'City'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _state,
              decoration: const InputDecoration(labelText: 'State'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _postal,
              decoration: const InputDecoration(labelText: 'Postal code'),
              validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _notes,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'Notes (optional)'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _saving ? null : _submit,
              child: Text(_saving ? 'Placing…' : 'Place request'),
            ),
          ],
        ),
      ),
    );
  }
}
