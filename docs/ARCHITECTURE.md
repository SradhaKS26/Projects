# Architecture Overview

## Style

Modular monolith. One deployable ASP.NET Core API. One PostgreSQL database.

Avoid for MVP:

- Microservices
- Kubernetes
- Redis / Kafka / RabbitMQ
- Multiple databases
- SignalR (evaluate later if polling is insufficient)

## Layers

1. **Domain** — entities, enums, state-machine rules, constants
2. **Application** — DTOs, validators, service interfaces
3. **Infrastructure** — EF Core, Identity, JWT, persistence, concrete services
4. **Api** — thin controllers, middleware, Swagger, DI composition

## Authorization

- ASP.NET Core Identity for users/roles/password hashing
- JWT access tokens + refresh tokens
- Permission claims loaded from `RolePermission`
- Policies named `Permission:{Name}` checked by handlers

Clients never decide authorization. Every sensitive action is validated server-side.

## Service request lifecycle (domain-ready)

`Pending -> Accepted/Assigned -> InProgress -> Completed`

Terminal alternatives: `Cancelled`, `Rejected`, `Failed`

Transitions are gated by `ServiceRequestStateMachine` (Phase 3 will expose workflow endpoints).

## Clients

- **Angular admin-web** consumes REST only
- **Flutter mobile** consumes REST only
- File blobs use local disk in Phase 3A via `IFileStorage`; never store files in PostgreSQL. Object storage can replace the implementation later.
