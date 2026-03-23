# Provider metadata and split registration

Provider identity in `UKHO.Search` needs to be available in more than one place:

- ingestion runtime needs it for queue-backed processing, diagnostics, and fail-fast validation
- development-time tooling needs it for provider discovery and read-only rule discovery in `StudioApiHost` and Theia

In the current implementation, generic provider identity, metadata, catalogs, and registration helpers now live in the shared `src/UKHO.Search.ProviderModel` project so both ingestion and studio composition can use the same source of truth.

The important constraint is that `StudioApiHost` must **not** depend on `IngestionServiceHost` being present. In live deployments, `StudioApiHost` and Theia are not expected to be deployed at all.

## Why this exists

Historically, provider identity has existed at runtime through provider factory names such as `file-share`. That is enough for ingestion execution, but it is not enough for development tooling that needs a formal, shared view of what providers exist.

Configuration alone is not the answer, because configuration can say which providers are enabled but should not become the source of truth for provider identity.

The chosen model is therefore:

- provider identity is defined in code as shared metadata
- provider packages own that metadata
- hosts compose provider metadata directly
- configuration references provider names only for enablement

## Core concepts

### `ProviderDescriptor`

A provider descriptor is the formal metadata definition for a provider.

At minimum it should contain:

- canonical `Name` such as `file-share`
- `DisplayName` for development-time UI/API consumers
- optional `Description`

The `Name` is the machine-readable identifier used across configuration, diagnostics, rules scoping, and API responses.

### `IProviderCatalog`

The provider catalog is a host-local service that exposes:

- all known provider descriptors
- case-insensitive lookup by provider name
- duplicate-name protection

A host knows about providers because it has composed the relevant provider metadata registrations, not because it queried another host.

## Split registration

Each provider package must expose two registration paths:

1. **Metadata registration**
2. **Runtime registration**

This is now a required design rule for provider packages in this repository, not an optional convention.

### Metadata registration

Metadata registration contributes provider descriptors and related metadata-only services.

It must **not** require:

- queue clients
- blob clients
- Elasticsearch clients
- hosted services
- ingestion runtime bootstrapping

This is what allows `StudioApiHost` to know about providers without becoming an ingestion runtime host.

### Runtime registration

Runtime registration contributes the provider's ingestion implementation, including its factory and other runtime dependencies.

`IngestionServiceHost` uses runtime registration together with metadata registration.

In the current implementation, the File Share provider is the reference pattern:

- `AddFileShareProviderMetadata()` for development-time metadata composition
- `AddFileShareProviderRuntime(...)` for ingestion runtime composition

New providers should follow the same mandatory split-registration shape in their own provider package.

## Enabled-provider configuration

Provider identity still comes from code-owned metadata, but runtime enablement is controlled by configuration.

The current ingestion runtime binds enabled providers from the `ingestion` section using the `Providers` collection, which maps to configuration keys such as:

- `ingestion:providers:0 = file-share`

Current behavior:

- configured provider names are matched case-insensitively
- configuration is used only to enable or disable providers, not to define provider identity
- if no providers are configured, ingestion defaults to all registered runtime providers
- invalid configured names fail fast during startup validation

## How hosts use it

### `IngestionServiceHost`

`IngestionServiceHost` should compose:

- provider metadata registrations
- provider runtime registrations

It should then validate configuration-backed enabled providers against:

- the provider catalog
- the runtime registrations

That validation should fail fast before queue creation, queue polling, or other ingestion bootstrap work.

In the current implementation this validation is performed by `IngestionProviderStartupValidator`, and `IngestionPipelineHostedService.StartAsync()` runs it before bootstrap starts.

### `StudioApiHost`

`StudioApiHost` should compose:

- provider metadata registrations only
- read-oriented rules loading
- studio provider registrations

The implemented host composes File Share metadata through `AddFileShareProviderMetadata()`, composes the shared read-oriented rules path through `AddIngestionRulesEngine()`, composes the tandem File Share Studio provider through `AddFileShareStudioProvider()`, validates Studio provider registration against shared provider metadata, and eagerly loads the shared rules reader so invalid provider-backed rules fail startup early.

The current `/providers` response returns the full shared `ProviderDescriptor` metadata shape for each provider, including:

- `name`
- `displayName`
- `description`

`StudioApiHost` also exposes a read-only `/rules` response backed by the same provider-aware rules-loading path used by ingestion.

Current `/rules` behavior:

- returns canonical provider names from `ProviderDescriptor.Name`
- returns the shared rules `schemaVersion`
- returns all known providers, including providers with no rules as empty `rules` arrays
- returns rule summaries rather than write/edit contracts
- fails startup clearly when configured rules reference an unknown provider

These endpoints currently return known provider metadata and read-only rule summaries only. They do not currently return enabled-state annotations, runtime-only details, or write operations.

It must not:

- discover providers from `IngestionServiceHost`
- call `IngestionServiceHost`
- require runtime ingestion services

### Theia

Theia is a development-time consumer of `StudioApiHost`.

It should consume provider metadata through the `StudioApiHost` API instead of trying to inspect ingestion runtime state directly.

## Development-time versus live deployment

This split is intentionally designed so that live deployments do not need any studio components.

- `StudioApiHost` is development-time only
- Theia is development-time only
- live ingestion deployment still works with provider metadata plus runtime registration in `IngestionServiceHost`

That keeps studio tooling optional and prevents production deployment from depending on development-only hosts.

## New provider onboarding

When adding a new provider:

1. define a canonical provider descriptor in the provider package
2. implement metadata registration for that descriptor
3. implement runtime registration for the provider factory and runtime services
4. register metadata-only composition in development-time hosts as needed
5. register any tandem Studio provider in development-time hosts as needed
6. register metadata plus runtime composition in `IngestionServiceHost`
7. ensure configuration enablement uses the canonical provider name from the descriptor

For concrete examples, see:

- `src/UKHO.Search.ProviderModel/*`
- `src/Studio/UKHO.Search.Studio/*`
- `src/Providers/UKHO.Search.Studio.Providers.FileShare/*`
- `src/Providers/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`
- `src/UKHO.Search.Services.Ingestion/Providers/IngestionProviderStartupValidator.cs`
- `src/Studio/StudioApiHost/StudioApiHostApplication.cs`

## Related documents

- [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism)
- [File Share provider](FileShare-Provider)
- [Documentation source map](Documentation-Source-Map)
- `docs/061-provider-metadata/spec-architecture-provider-metadata_v0.01.md`