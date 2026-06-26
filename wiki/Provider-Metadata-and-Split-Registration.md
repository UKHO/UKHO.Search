# Provider metadata and split registration

Provider identity in `UKHO.Search` needs to be available in more than one place:

- ingestion runtime needs it for queue-backed processing, diagnostics, and fail-fast validation
- provider-aware tooling and validation flows need it without being forced to compose ingestion runtime services

In the current implementation, generic provider identity, metadata, catalogs, and registration helpers now live in the shared `src/UKHO.Search.ProviderModel` project so active hosts and tooling can use the same source of truth.

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

This is what allows host-local validation and tooling to know about providers without becoming an ingestion runtime host.

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

### Metadata-only consumers

Any host or tool that only needs provider discovery or validation should compose:

- provider metadata registrations only
- any read-only services that sit on top of the shared provider catalog

Those consumers must not:

- discover providers from `IngestionServiceHost`
- call `IngestionServiceHost` to obtain provider identity
- require runtime ingestion services when metadata-only composition is sufficient

The retained Studio and Theia source trees are an example of detached code that can still reuse the shared provider model later without being part of the current active workflow.

## New provider onboarding

When adding a new provider:

1. define a canonical provider descriptor in the provider package
2. implement metadata registration for that descriptor
3. implement runtime registration for the provider factory and runtime services
4. register metadata-only composition in any tooling or validation host that needs provider discovery without runtime services
5. register metadata plus runtime composition in `IngestionServiceHost`
6. ensure configuration enablement uses the canonical provider name from the descriptor

For concrete examples, see:

- `src/UKHO.Search.ProviderModel/*`
- `src/Providers/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`
- `src/UKHO.Search.Services.Ingestion/Providers/IngestionProviderStartupValidator.cs`

## Related documents

- [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism)
- [File Share provider](FileShare-Provider)
- [Documentation source map](Documentation-Source-Map)
- `dev/work-packages/mvp/061-provider-metadata/spec-architecture-provider-metadata_v0.01.md`