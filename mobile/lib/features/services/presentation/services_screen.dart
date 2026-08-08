import 'package:flutter/material.dart';

class ServicesScreen extends StatelessWidget {
  const ServicesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Services')),
      body: const Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Service categories and catalog browsing will be added in Phase 2.',
        ),
      ),
    );
  }
}
