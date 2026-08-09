import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:service_management_mobile/main.dart';

void main() {
  testWidgets('login shell renders brand', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: ServiceManagementApp()));
    await tester.pumpAndSettle();

    expect(find.text('Service Management'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
  });
}
