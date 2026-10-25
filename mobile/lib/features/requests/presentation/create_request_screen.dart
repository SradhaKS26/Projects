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
    this.fulfillmentKind = 'onSite',
  });

  final String serviceId;
  final String serviceName;
  final double price;
  final String fulfillmentKind;

  @override
  ConsumerState<CreateRequestScreen> createState() => _CreateRequestScreenState();
}

class _CreateRequestScreenState extends ConsumerState<CreateRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _pickup = _AddressControllers();
  final _dropoff = _AddressControllers();
  final _notes = TextEditingController();
  final _parcel = TextEditingController();
  final _recipientName = TextEditingController();
  final _recipientPhone = TextEditingController();
  var _passengers = 1;
  var _saving = false;
  String? _error;

  bool get _isRide => widget.fulfillmentKind == 'ride';
  bool get _isParcel => widget.fulfillmentKind == 'parcel';
  bool get _isTrip => _isRide || _isParcel;

  @override
  void dispose() {
    _pickup.dispose();
    _dropoff.dispose();
    _notes.dispose();
    _parcel.dispose();
    _recipientName.dispose();
    _recipientPhone.dispose();
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
            addressLine1: _pickup.line1.text.trim(),
            addressLine2: _pickup.optionalLine2,
            city: _pickup.city.text.trim(),
            state: _pickup.state.text.trim(),
            postalCode: _pickup.postal.text.trim(),
            notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
            destination: _isTrip ? _dropoff.toJson() : null,
            passengerCount: _isRide ? _passengers : null,
            parcelDescription: _isParcel ? _parcel.text.trim() : null,
            recipientName: _isParcel ? _recipientName.text.trim() : null,
            recipientPhone: _isParcel ? _recipientPhone.text.trim() : null,
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
          fallback: 'Unable to place this request. Check the details and try again.',
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

    final title = _isRide
        ? 'Book ${widget.serviceName}'
        : _isParcel
            ? 'Send with ${widget.serviceName}'
            : 'Request ${widget.serviceName}';

    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              _isRide
                  ? '\$${widget.price.toStringAsFixed(2)} base fare · Nearby drivers have 10 minutes to accept ${widget.serviceName}.'
                  : _isParcel
                      ? '\$${widget.price.toStringAsFixed(2)} · A courier has 10 minutes to accept this pickup and drop-off.'
                      : '\$${widget.price.toStringAsFixed(2)} · Eligible providers have 10 minutes to accept. Contact details stay hidden until someone accepts.',
            ),
            const SizedBox(height: 16),
            Text(_isTrip ? 'Pickup' : 'Service address', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            _AddressFields(controllers: _pickup),
            if (_isTrip) ...[
              const SizedBox(height: 20),
              Text('Drop-off', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              _AddressFields(controllers: _dropoff),
            ],
            if (_isRide) ...[
              const SizedBox(height: 20),
              DropdownButtonFormField<int>(
                key: ValueKey(_passengers),
                initialValue: _passengers,
                decoration: const InputDecoration(labelText: 'Passengers'),
                items: [
                  for (var i = 1; i <= 6; i++)
                    DropdownMenuItem(value: i, child: Text('$i')),
                ],
                onChanged: (value) => setState(() => _passengers = value ?? 1),
              ),
            ],
            if (_isParcel) ...[
              const SizedBox(height: 20),
              TextFormField(
                controller: _parcel,
                maxLines: 2,
                decoration: const InputDecoration(labelText: 'What are you sending?'),
                validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _recipientName,
                decoration: const InputDecoration(labelText: 'Recipient name'),
                validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _recipientPhone,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(labelText: 'Recipient phone'),
                validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
              ),
            ],
            const SizedBox(height: 12),
            TextFormField(
              controller: _notes,
              maxLines: 3,
              decoration: InputDecoration(
                labelText: _isRide
                    ? 'Notes for the driver (optional)'
                    : _isParcel
                        ? 'Notes for the courier (optional)'
                        : 'Notes (optional)',
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _saving ? null : _submit,
              child: Text(_saving
                  ? 'Placing…'
                  : _isRide
                      ? 'Request ride'
                      : _isParcel
                          ? 'Request delivery'
                          : 'Place request'),
            ),
          ],
        ),
      ),
    );
  }
}

class _AddressControllers {
  final line1 = TextEditingController();
  final line2 = TextEditingController();
  final city = TextEditingController();
  final state = TextEditingController();
  final postal = TextEditingController();

  String? get optionalLine2 => line2.text.trim().isEmpty ? null : line2.text.trim();

  Map<String, dynamic> toJson() => {
        'addressLine1': line1.text.trim(),
        'addressLine2': optionalLine2,
        'city': city.text.trim(),
        'state': state.text.trim(),
        'postalCode': postal.text.trim(),
      };

  void dispose() {
    line1.dispose();
    line2.dispose();
    city.dispose();
    state.dispose();
    postal.dispose();
  }
}

class _AddressFields extends StatelessWidget {
  const _AddressFields({required this.controllers});

  final _AddressControllers controllers;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        TextFormField(
          controller: controllers.line1,
          decoration: const InputDecoration(labelText: 'Address line 1'),
          validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: controllers.line2,
          decoration: const InputDecoration(labelText: 'Address line 2 (optional)'),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: controllers.city,
          decoration: const InputDecoration(labelText: 'City'),
          validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: controllers.state,
          decoration: const InputDecoration(labelText: 'State'),
          validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: controllers.postal,
          decoration: const InputDecoration(labelText: 'Postal code'),
          validator: (value) => value == null || value.trim().isEmpty ? 'Required' : null,
        ),
      ],
    );
  }
}
