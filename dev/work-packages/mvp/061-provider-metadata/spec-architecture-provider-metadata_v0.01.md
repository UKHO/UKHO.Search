# Provider metadata and split registration specification

- Work Package: `061-provider-metadata`
- Version: `v0.01`
- Status: `Draft`
- Last updated: `2026-03-22`

## 1. Overview

### 1.1 Purpose

Define a formal provider metadata model for ingestion providers so that provider identity, naming, and discovery are consistent across hosts without creating any runtime dependency between `StudioApiHost` and `IngestionServiceHost`.

This work package formalizes a split-registration approach in each provider package so that:

- provider packages own their canonical metadata
- `IngestionServiceHost` can compose provider metadata together with runtime ingestion services
- `StudioApiHost` can compose provider metadata only, without needing ingestion runtime services
- Theia and `StudioApiHost` remain development-time-only concerns and are not required in live deployments

### 1.2 Scope

In scope:

- define a shared provider metadata contract
- define a shared catalog abstraction for provider metadata lookup and enumeration
- define split registration in each provider package for metadata-only registration and runtime registration
- define startup validation rules for ingestion runtime enablement
- define how `StudioApiHost` can expose provider metadata through a development-time API without depending on `IngestionServiceHost`
- define documentation updates required in `wiki/`

Out of scope:

- implementing a plugin or reflection-based discovery mechanism
- creating cross-process discovery between `StudioApiHost` and `IngestionServiceHost`
- changing production deployment topology to include `StudioApiHost` or Theia
- defining provider-specific business rules beyond provider identity and composition

### 1.3 Stakeholders

- developers implementing new ingestion providers
- maintainers of `IngestionServiceHost`
- maintainers of `StudioApiHost`
- developers working on the Theia studio shell
- operations teams responsible for live deployments

### 1.4 Definitions

- **Provider**: a source-specific ingestion implementation that can deserialize ingestion messages and process them through a provider-owned pipeline.
- **Provider descriptor**: shared metadata that formally describes a provider, including its canonical identity.
- **Provider catalog**: a shared service that exposes all known provider descriptors registered in a host.
- **Metadata registration**: dependency injection registration that contributes provider descriptors and related metadata-only services.
- **Runtime registration**: dependency injection registration that contributes provider factories and other ingestion runtime services.
- **Split registration**: the requirement that each provider package exposes separate registration paths for metadata and runtime wiring.
- **Development-time hosts**: `StudioApiHost` and Theia-based studio experiences that are used during local/developer workflows and are not part of live production deployment.

## 2. System context

### 2.1 Current state

The current ingestion provider model already has a canonical runtime concept of provider identity through `IIngestionDataProviderFactory.Name`, with the File Share provider using the slug `file-share`.

Current gaps:

- provider identity is formalized at runtime, but not yet as a shared metadata model
- `StudioApiHost` does not have a first-class way to discover known providers without either duplicating strings or coupling to ingestion-specific runtime registrations
- configuration can enable or disable providers, but configuration alone is not sufficient to define what a provider is
- there is no explicit split between provider metadata registration and provider runtime registration

### 2.2 Proposed state

A provider package will become the authoritative source for both:

1. the provider's canonical metadata
2. the provider's runtime ingestion implementation

Each provider package must expose split registration entry points so that hosts can opt into the appropriate level of composition:

- `IngestionServiceHost` registers provider metadata and provider runtime services
- `StudioApiHost` registers provider metadata only
- live production deployments remain valid even when `StudioApiHost` and Theia are absent

Provider identity will therefore be known to each host because that host composes the relevant provider package directly, not because one host discovers the other at runtime.

### 2.3 Assumptions

- provider identity must remain stable and machine-readable across runtime, diagnostics, rules, and developer tooling
- provider names should remain canonical lowercase slugs such as `file-share`
- the repository will continue to support multiple providers over time
- `StudioApiHost` has access to the shared configuration system when present, but must not depend on `IngestionServiceHost` existing
- provider packages can be referenced directly by hosts that need their metadata

### 2.4 Constraints

- no host-to-host runtime dependency is permitted between `StudioApiHost` and `IngestionServiceHost`
- `StudioApiHost` and Theia are development-time-only components and must not become a production runtime dependency
- provider identity must not be defined only in configuration
- the solution must preserve onion architecture dependency direction
- the design should avoid duplicating canonical provider names across hosts

## 3. Component / service design (high level)

### 3.1 Components

#### Shared provider metadata contract

A shared inward-facing contract must define the formal provider metadata model. This should include:

- a `ProviderDescriptor` type for canonical provider identity and presentation metadata
- an `IProviderCatalog` abstraction for case-insensitive lookup and enumeration of known providers

This contract should live in a shared inward project that both development-time and runtime hosts can reference safely.

#### Provider packages

Each provider package must own its canonical `ProviderDescriptor` and expose split registration methods:

- metadata registration for descriptor/catalog contribution only
- runtime registration for ingestion factories and related runtime services

The provider package remains the single source of truth for its provider name.

#### `IngestionServiceHost`

`IngestionServiceHost` must compose provider metadata and provider runtime registrations. It must use the provider catalog together with configuration-backed enablement rules to validate startup state before performing queue creation, polling, or other ingestion bootstrap actions.

#### `StudioApiHost`

`StudioApiHost` must compose provider metadata registrations only. It may expose provider metadata through a minimal API for development tooling, including Theia, without requiring ingestion runtime services or `IngestionServiceHost` to be present.

#### Theia studio shell

Theia is a consumer of `StudioApiHost` during development. It must rely on the development-time API contract rather than attempting to inspect ingestion runtime state directly.

### 3.2 Data flows

```mermaid
flowchart LR
    PKG[Provider package] --> META[Metadata registration]
    PKG --> RT[Runtime registration]

    META --> STUDIO[StudioApiHost]
    META --> ING[IngestionServiceHost]
    RT --> ING

    STUDIO --> API[/providers API]
    API --> THEIA[Theia development shell]

    CFG[Configuration enablement] --> ING
    ING --> VALIDATE[Startup validation]
```

### 3.3 Key decisions

1. **Provider identity is code-owned metadata, not config-owned metadata.**
   Configuration may enable or disable providers, but it must not be the primary source of canonical provider definition.

2. **Hosts compose provider metadata directly.**
   `StudioApiHost` knows about providers because it references the shared metadata contract and the relevant provider metadata registrations.

3. **Split registration is mandatory.**
   Each provider package must separate metadata registration from runtime registration.

4. **Development-time hosts remain optional.**
   The absence of `StudioApiHost` and Theia in live deployment must not affect ingestion runtime behavior.

## 4. Functional requirements

### FR1 - Shared provider metadata contract

A shared provider metadata contract must be introduced in an inward-facing project.

The contract must include:

- `ProviderDescriptor`
- `IProviderCatalog`

At minimum, `ProviderDescriptor` must support:

- canonical `Name`
- friendly `DisplayName`
- optional `Description`

`Name` must be the canonical machine-readable provider identifier.

### FR2 - Canonical naming rules

Provider names must be canonical lowercase invariant slugs.

Rules:

- names must be unique within a host
- names must compare case-insensitively for lookup purposes
- names must be stable enough to be used in configuration, diagnostics, rules scoping, and API responses

The File Share provider remains the reference example with the canonical name `file-share`.

### FR3 - Split registration in each provider package

Each provider package must expose separate registration paths for:

1. metadata registration
2. runtime registration

Metadata registration must:

- register the provider descriptor into the shared catalog model
- avoid requiring ingestion runtime dependencies such as queue clients, indexing clients, or hosted services

Runtime registration must:

- register the provider's ingestion factory and any provider-specific runtime services
- assume that metadata registration has already been applied, or apply it idempotently as part of runtime composition

### FR4 - Provider catalog behavior

`IProviderCatalog` must provide:

- enumeration of all known provider descriptors registered in the current host
- lookup by provider name using case-insensitive matching
- deterministic failure behavior for duplicate provider names

The provider catalog must not require `IngestionServiceHost` to be present.

### FR5 - `IngestionServiceHost` composition and validation

`IngestionServiceHost` must:

- compose provider metadata registrations
- compose provider runtime registrations
- read the configured enabled-provider list from configuration
- validate the enabled-provider list against the provider catalog and runtime registrations before queue creation, queue polling, or other ingestion bootstrap work begins

Validation must fail fast when:

- an enabled provider name does not exist in the provider catalog
- an enabled provider has metadata but no runtime registration
- duplicate provider identities are registered

### FR6 - `StudioApiHost` provider awareness

`StudioApiHost` must know about providers by composing provider metadata registrations directly.

It must not:

- discover providers from `IngestionServiceHost`
- call `IngestionServiceHost` to obtain provider metadata
- require ingestion runtime registrations to be present

### FR7 - Development-time provider endpoint

`StudioApiHost` must expose a minimal API endpoint for development tooling to retrieve provider metadata.

The endpoint must return provider descriptors known to the local host composition.

If configuration-backed enabled-provider information is available in `StudioApiHost`, the response may also include an enabled-state annotation, but the endpoint's primary purpose is provider discovery from shared metadata.

### FR8 - Development-time-only deployment model

The design must explicitly support the fact that `StudioApiHost` and Theia are development-time-only components.

Therefore:

- live deployments must not require `StudioApiHost`
- live deployments must not require Theia
- provider metadata and runtime composition for live ingestion must remain valid without any development-time host present

### FR9 - Provider package onboarding guidance

The implementation and wiki documentation must define the expected onboarding steps for a new provider package, including:

- define canonical provider descriptor
- implement metadata registration
- implement runtime registration
- register metadata-only composition in development-time hosts as needed
- register metadata plus runtime composition in ingestion runtime hosts

### FR10 - Full automated test coverage for the feature

The implementation of provider metadata and split registration must include full automated test coverage for all new and changed behavior introduced by this work package.

Coverage must include:

- shared provider metadata contract behavior
- provider catalog construction, lookup, ordering, and duplicate-name failure paths
- metadata-only registration for each provider package affected by the change
- runtime registration for each provider package affected by the change
- `IngestionServiceHost` startup validation and fail-fast behavior
- `StudioApiHost` provider-discovery API behavior
- configuration-driven enabled-provider behavior
- live-deployment topology assumptions where development-time hosts are absent

No implementation work for this feature is complete unless the corresponding automated tests have been added and are passing.

## 5. Non-functional requirements

### NFR1 - No host coupling

The design must avoid runtime coupling between `StudioApiHost` and `IngestionServiceHost`.

### NFR2 - Layering compliance

The metadata model and registration pattern must preserve onion architecture dependency direction.

### NFR3 - Deterministic startup failure

Provider configuration mismatches must fail at startup before destructive or irreversible ingestion initialization work begins.

### NFR4 - Extensibility

The model must support additional providers without requiring `StudioApiHost` or `IngestionServiceHost` to hard-code new provider names outside provider-owned registration.

### NFR5 - Documentation completeness

The work package must be reflected in the repository wiki so that developers can discover:

- why provider metadata exists
- how split registration works
- why `StudioApiHost` and Theia remain development-time-only
- how to add a new provider correctly

### NFR6 - Test coverage completeness

This feature must achieve full automated test coverage for the implemented behavior across the affected layers.

At minimum, the delivered tests must collectively verify:

- happy-path behavior
- invalid configuration and fail-fast behavior
- duplicate registration and missing-runtime edge cases
- development-time composition behavior
- production/runtime composition behavior without `StudioApiHost` or Theia

Tests must be treated as part of the Definition of Done for the feature, not as optional follow-up work.

## 6. Data model

### 6.1 Provider descriptor

The target conceptual model is:

| Field | Required | Purpose |
|---|---|---|
| `Name` | Yes | Canonical machine-readable provider slug, e.g. `file-share`. |
| `DisplayName` | Yes | Friendly human-readable name for developer tooling and diagnostics. |
| `Description` | No | Short explanatory text suitable for development-time UI/API consumers. |

### 6.2 Provider catalog

The provider catalog is a host-local read model over registered provider descriptors.

Responsibilities:

- normalize and compare names case-insensitively
- expose all known descriptors in deterministic order
- reject duplicate canonical names

### 6.3 Configuration enablement model

Configuration remains an enablement mechanism, not the source of canonical provider identity.

The enabled-provider configuration model must reference canonical provider names that are defined by provider descriptors.

## 7. Interfaces & integration

### 7.1 Registration interfaces

The implementation should provide clear registration entry points in each provider package for:

- metadata-only registration
- runtime registration

The exact method names are implementation detail, but the split must be explicit and discoverable.

### 7.2 Development-time API contract

`StudioApiHost` must expose a provider discovery endpoint for development tooling.

The endpoint contract should be designed for stable consumption by Theia and related studio tooling.

At minimum, the response should include:

- provider `name`
- provider `displayName`
- provider `description` when available

### 7.3 Configuration integration

The ingestion runtime must treat configuration-backed provider enablement as a reference to provider descriptors rather than a free-form naming model.

## 8. Observability (logging/metrics/tracing)

- Startup validation failures must log which configured provider names were invalid or incomplete.
- Provider registration conflicts must be logged with enough detail to diagnose duplicate names.
- Development-time API responses do not need custom metrics initially, but provider discovery failures should be visible through normal host logging.

## 9. Security & compliance

- The provider discovery endpoint is intended for development-time tooling and must expose metadata only, not secrets or provider-specific credentials.
- Split registration must ensure metadata registration alone does not accidentally initialize queue clients, storage clients, or other privileged runtime dependencies.

## 10. Testing strategy

Full automated test coverage is required for this feature.

Testing must cover:

1. provider catalog behavior
   - case-insensitive lookup
   - duplicate-name rejection
   - deterministic enumeration
   - descriptor normalization and validation behavior if implemented

2. provider package registration
   - metadata registration without runtime services
   - runtime registration with metadata available
   - runtime registration idempotency where supported
   - provider-specific descriptor registration uses the canonical provider name consistently

3. ingestion startup validation
   - enabled provider exists and is runnable
   - enabled provider missing from catalog
   - enabled provider present in catalog but missing runtime registration
   - duplicate provider registrations fail deterministically before queue/bootstrap work begins
   - no queue/bootstrap side effects occur when validation fails

4. `StudioApiHost` development-time API
   - returns provider descriptors from metadata-only composition
   - does not require ingestion runtime services
   - returns the expected canonical names and display metadata
   - handles enabled-state annotation correctly if that behavior is implemented

5. deployment topology assumptions
   - live ingestion runtime remains valid without `StudioApiHost` or Theia

6. regression coverage for affected existing provider(s)
   - the File Share provider continues to expose `file-share` as its canonical name through the new metadata model
   - existing ingestion behavior continues to compose successfully through the split-registration pattern

The test suite for this work package should include the appropriate combination of unit, host-level, and integration tests in the existing test projects so that all functional and failure-path requirements in this specification are exercised automatically.

## 11. Rollout / migration

1. introduce the shared provider metadata contract
2. update the File Share provider package to adopt split registration
3. update `IngestionServiceHost` composition to use metadata plus runtime registration and fail-fast validation
4. update `StudioApiHost` composition to use metadata-only registration and expose provider discovery
5. update Theia integration to consume the provider discovery endpoint during development
6. update wiki pages and source-map references

Backward-compatibility note:

- existing provider names must be preserved to avoid breaking configuration, diagnostics, and indexed provider provenance

## 12. Open questions

1. Should the `StudioApiHost` provider endpoint return only known providers, or known providers plus configuration-derived enabled state when present?
2. Should provider descriptors later grow capability flags for UI feature gating, or stay minimal until a concrete use case appears?
3. Should runtime registration always imply metadata registration, or should hosts be required to call the two registrations explicitly in sequence?