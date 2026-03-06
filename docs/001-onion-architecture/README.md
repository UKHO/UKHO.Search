# Onion Architecture (UKHO.Search)

This repository follows **Onion Architecture**. The goal is to keep domain logic independent of infrastructure and hosting concerns.

## Dependency rule
Dependencies must only point **inward**:

`Hosts (Web/Worker) -> Infrastructure -> Services -> Domain`

Inner layers **must not** reference outer layers.

## Layer mapping in this repo

### Domain (core)
Projects that contain the domain model and domain rules:
- `src/UKHO.Search/UKHO.Search.csproj`
- `src/UKHO.Search.Query/UKHO.Search.Query.csproj`
- `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`

Domain projects must not depend on:
- `UKHO.Search.Services.*`
- `UKHO.Search.Infrastructure.*`
- Host/web projects
- EF Core, HTTP clients, Azure SDKs, or other infrastructure libraries

### Services (application/use-cases)
Projects named `UKHO.Search.Services.*` implement use-cases and orchestration.

Allowed dependencies:
- Can reference Domain projects.
- Must not reference Infrastructure projects or Host projects.

### Infrastructure (adapters)
Projects named `UKHO.Search.Infrastructure.*` implement outward-facing integrations (persistence, external services, messaging, etc.).

Allowed dependencies:
- Can reference Domain and Services projects.
- Must not reference Host projects.

### Hosts / UI
Only UI, API endpoints, hosting, and startup/DI wiring belong in host/web projects (for example under `src/Hosts/*`).

Allowed dependencies:
- Can reference Services and Infrastructure.
- Must not put domain logic or infrastructure implementations here.

## Enforcement
When adding a new project or reference, ensure it respects the dependency direction above.
