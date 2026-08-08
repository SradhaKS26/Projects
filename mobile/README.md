# Service Management Mobile (Flutter)

Phase 1 shell for customers and service providers.

## Stack

- Flutter + Dart
- Material 3
- Riverpod
- Dio
- flutter_secure_storage
- go_router

## Structure

```text
lib/
  core/
    networking/
    routing/
    storage/
    theme/
  features/
    authentication/
    home/
    services/
    profile/
```

## Run

```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/api
```

The app talks only to the ASP.NET Core API. It never accesses PostgreSQL directly.
