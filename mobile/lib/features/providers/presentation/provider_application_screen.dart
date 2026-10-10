import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../authentication/data/auth_repository.dart';
import '../../services/data/catalog_models.dart';
import '../data/provider_models.dart';
import '../data/provider_repository.dart';

class ProviderApplicationScreen extends ConsumerStatefulWidget {
  const ProviderApplicationScreen({super.key});

  @override
  ConsumerState<ProviderApplicationScreen> createState() =>
      _ProviderApplicationScreenState();
}

class _ProviderApplicationScreenState
    extends ConsumerState<ProviderApplicationScreen> {
  final _description = TextEditingController();
  final _selected = <String>{};
  List<CatalogService> _services = const [];
  List<CategoryDocumentRequirement> _requirements = const [];
  ProviderProfile? _profile;
  var _loading = true;
  var _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _description.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final repo = ref.read(providerRepositoryProvider);
      final profile = await repo.fetchMine();
      final services = await repo.fetchServices();
      _description.text = profile.description ?? '';
      _selected
        ..clear()
        ..addAll(profile.services.map((s) => s.serviceId));
      final categoryIds = services
          .where((s) => _selected.contains(s.id))
          .map((s) => s.categoryId)
          .toSet();
      final requirements = <CategoryDocumentRequirement>[];
      for (final id in categoryIds) {
        requirements.addAll(await repo.fetchRequirements(id));
      }

      if (!mounted) return;
      setState(() {
        _profile = profile;
        _services = services;
        _requirements = requirements;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Unable to load your application.';
        _loading = false;
      });
    }
  }

  Future<void> _refreshRequirements() async {
    final categoryIds = _services
        .where((s) => _selected.contains(s.id))
        .map((s) => s.categoryId)
        .toSet();
    final repo = ref.read(providerRepositoryProvider);
    final requirements = <CategoryDocumentRequirement>[];
    for (final id in categoryIds) {
      requirements.addAll(await repo.fetchRequirements(id));
    }
    if (!mounted) return;
    setState(() => _requirements = requirements);
  }

  Future<void> _save() async {
    if (_selected.isEmpty) {
      setState(() => _error = 'Select at least one service.');
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      final profile = await ref.read(providerRepositoryProvider).saveApplication(
            description: _description.text.trim().isEmpty
                ? null
                : _description.text.trim(),
            serviceIds: _selected.toList(),
          );
      if (!mounted) return;
      setState(() {
        _profile = profile;
        _saving = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Application saved for review.')),
      );
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = 'Unable to save application.';
      });
    }
  }

  Future<void> _upload(CategoryDocumentRequirement requirement) async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: const ['pdf', 'jpg', 'jpeg', 'png', 'webp'],
    );
    final file = result?.files.single;
    if (file?.path == null) {
      return;
    }

    try {
      await ref.read(providerRepositoryProvider).uploadDocument(
            documentRequirementId: requirement.id,
            filePath: file!.path!,
            fileName: file.name,
          );
      await _load();
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Unable to upload document.')),
      );
    }
  }

  Future<void> _setAvailability(String status) async {
    try {
      final profile =
          await ref.read(providerRepositoryProvider).setAvailability(status);
      if (!mounted) return;
      setState(() => _profile = profile);
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Unable to update availability.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final profile = _profile;
    final grouped = <String, List<CatalogService>>{};
    for (final service in _services) {
      grouped.putIfAbsent(service.categoryName, () => []).add(service);
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('My application'),
        actions: [
          TextButton(
            onPressed: () async {
              await ref.read(authRepositoryProvider).logout();
              if (context.mounted) {
                context.go('/login');
              }
            },
            child: const Text('Sign out'),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_error != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(
                        _error!,
                        style: TextStyle(color: Theme.of(context).colorScheme.error),
                      ),
                    ),
                  if (profile != null) ...[
                    Card(
                      child: ListTile(
                        title: Text('Status: ${profile.approvalStatus}'),
                        subtitle: Text(
                          profile.approvalStatus == 'rejected' &&
                                  profile.reviewReason != null
                              ? profile.reviewReason!
                              : profile.approvalStatus == 'approved'
                                  ? 'You can receive work once you are available.'
                                  : 'Until you are approved, this is the only screen you can use.',
                        ),
                      ),
                    ),
                    if (profile.isApproved) ...[
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 8,
                        children: [
                          FilledButton(
                            onPressed: () => _setAvailability('available'),
                            child: const Text('Available'),
                          ),
                          OutlinedButton(
                            onPressed: () => _setAvailability('unavailable'),
                            child: const Text('Unavailable'),
                          ),
                        ],
                      ),
                    ],
                    const SizedBox(height: 16),
                    TextField(
                      controller: _description,
                      enabled: profile.canEdit,
                      maxLines: 3,
                      decoration: const InputDecoration(
                        labelText: 'Description',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'Services you can fulfill',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 8),
                    ...grouped.entries.expand((entry) {
                      return [
                        Padding(
                          padding: const EdgeInsets.only(top: 8, bottom: 4),
                          child: Text(
                            entry.key,
                            style: Theme.of(context).textTheme.titleSmall,
                          ),
                        ),
                        ...entry.value.map(
                          (service) => CheckboxListTile(
                            value: _selected.contains(service.id),
                            title: Text(service.name),
                            subtitle: Text(
                              '\$${service.basePrice.toStringAsFixed(2)}',
                            ),
                            onChanged: profile.canEdit
                                ? (checked) async {
                                    setState(() {
                                      if (checked == true) {
                                        _selected.add(service.id);
                                      } else {
                                        _selected.remove(service.id);
                                      }
                                    });
                                    await _refreshRequirements();
                                  }
                                : null,
                          ),
                        ),
                      ];
                    }),
                    const SizedBox(height: 16),
                    Text(
                      'Required documents',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    if (_requirements.isEmpty)
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 12),
                        child: Text(
                          'Select services to see the documents that category requires.',
                        ),
                      ),
                    ..._requirements.map((req) {
                      final uploaded = profile.documents
                          .where((d) => d.documentRequirementId == req.id)
                          .firstOrNull;
                      return ListTile(
                        title: Text(req.name),
                        subtitle: Text(
                          uploaded == null
                              ? (req.description ?? req.categoryName)
                              : 'Uploaded: ${uploaded.originalFileName}',
                        ),
                        trailing: profile.canEdit
                            ? TextButton(
                                onPressed: () => _upload(req),
                                child: const Text('Upload'),
                              )
                            : null,
                      );
                    }),
                    if (profile.canEdit) ...[
                      const SizedBox(height: 16),
                      FilledButton(
                        onPressed: _saving ? null : _save,
                        child: Text(_saving ? 'Saving…' : 'Save application'),
                      ),
                    ],
                  ],
                ],
              ),
            ),
    );
  }
}
