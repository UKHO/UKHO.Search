# Provider metadata-backed rule loading specification

**Target output path:** `docs/063-provider-metadata-rule-loading/spec-architecture-provider-metadata-rule-loading_v0.01.md`

- Work Package: `063-provider-metadata-rule-loading`
- Version: `v0.01`
- Status: `Draft`
- Last updated: `2026-03-22`

## 1. Overview

### 1.1 Purpose

Strengthen ingestion rule loading and current Studio rule discovery by making provider identity in the rules subsystem depend on the shared `UKHO.Search.ProviderModel` rather than on free-form provider-name strings from configuration alone.

This work package defines how `IngestionServiceHost` rule loading should validate and canonicalize provider names through shared provider metadata, and how the same rules-loading foundation should now be integrated into `StudioApiHost` for read-oriented Studio scenarios without coupling `StudioApiHost` to ingestion runtime services.

### 1.2 Scope

In scope:

- define how ingestion rules loading must integrate with `UKHO.Search.ProviderModel`
- define validation of rule provider keys against `IProviderCatalog`
- define canonicalization of provider names during rule loading
- define the relationship between provider metadata, enabled providers, and loaded rules
- define the shape of reusable rules-loading services suitable for both `IngestionServiceHost` and `StudioApiHost`
- define and implement read-oriented Studio access to rules for all known providers in `StudioApiHost`
- define extensibility constraints so future Studio save/update operations are not made harder
- define required tests and documentation updates for this behavior

Out of scope:

- implementing Studio save/update rule APIs in this package
- implementing provider-specific Studio editing workflows in this package
- changing rule DSL semantics beyond provider identity handling
- changing ingestion runtime enablement semantics beyond their interaction with rules loading
- removing existing rules infrastructure if it can be strengthened through refactoring and reuse

### 1.3 Stakeholders

- maintainers of `IngestionServiceHost`
- maintainers of `StudioApiHost`
- maintainers of Search Studio / Theia
- maintainers of provider packages and provider metadata registrations
- maintainers of rules authoring and storage workflows

### 1.4 Definitions

- **Provider Model**: the shared provider identity and metadata layer in `src/UKHO.Search.ProviderModel`.
- **Canonical provider name**: the provider identity exposed by `ProviderDescriptor.Name`, for example `file-share`.
- **Rule provider key**: the provider segment used in rules storage/configuration, for example `rules:file-share:<ruleId>`.
- **Known provider**: a provider present in `IProviderCatalog`.
- **Enabled provider**: a provider enabled for ingestion runtime execution through configuration.
- **Read-oriented rules access**: loading and inspecting rules without requiring ingestion runtime services.
- **Write-oriented rules access**: future creation, update, or deletion of rules through Studio-side tooling.

## 2. System context

### 2.1 Current state

The solution currently has:

- shared provider identity and metadata in `UKHO.Search.ProviderModel`
- ingestion startup validation of enabled providers against `IProviderCatalog` and runtime registrations
- rules loading that groups rules by provider name from configuration/app configuration
- `StudioApiHost` provider discovery through the shared Provider Model

Current weakness:

- rule loading still treats provider names as configuration-driven strings rather than catalog-backed provider identities
- unknown rule provider keys can drift away from the shared provider metadata model
- provider-name normalization and canonicalization are not strongly aligned with the shared Provider Model
- the current rules-loading path is ingestion-oriented and not yet explicitly shaped for Studio reuse

### 2.2 Desired state

The rules subsystem should use `UKHO.Search.ProviderModel` as the source of truth for provider identity.

This means:

- rules must load against known providers from `IProviderCatalog`
- provider names from rule storage must be canonicalized to `ProviderDescriptor.Name`
- unknown provider rule groups must fail fast
- rules for known-but-disabled providers may still be loaded for discovery and future Studio scenarios
- `StudioApiHost` must now be able to read rules for all known providers through a reusable rules-loading path that does not require ingestion runtime composition
- future save/update support must be anticipated by preserving a clean separation between read and write responsibilities

### 2.3 Architectural direction

The rule-loading flow should become:

1. load raw rule entries from the rules source
2. resolve each provider key against `IProviderCatalog`
3. canonicalize the provider key to `ProviderDescriptor.Name`
4. reject rule entries whose provider key does not resolve to known provider metadata
5. build rule catalogs keyed by canonical provider name
6. allow consuming hosts to decide whether they care about all known providers or only enabled/runtime providers

### 2.4 Assumptions

- `UKHO.Search.ProviderModel` is now the shared source of truth for provider identity
- `StudioApiHost` remains development-time only and must not depend on ingestion runtime services
- Search Studio now needs to inspect rules across all providers, not only enabled ingestion providers
- future Search Studio scenarios will eventually need to save/update rules, but that write path is not part of this work package

### 2.5 Constraints

- the rules subsystem must not bypass canonical provider identity from `IProviderCatalog`
- `StudioApiHost` must not acquire ingestion runtime dependencies just to read rules
- rule provider-name handling must remain generic and must not encode provider-specific domain concepts
- future write capabilities must not be blocked by over-specializing the read model around ingestion-only assumptions

## 3. Component / service design (high level)

### 3.1 Components

#### `UKHO.Search.ProviderModel`

Responsibilities in this work package:

- remain the source of truth for provider identity
- expose canonical provider names and shared metadata through `IProviderCatalog`
- support case-insensitive lookup and canonical output

#### Rules loading components in infrastructure/services

Responsibilities in this work package:

- validate rule provider keys against `IProviderCatalog`
- canonicalize rule provider names through `ProviderDescriptor.Name`
- expose a reusable rules-loading/read model that can serve both ingestion and Studio hosts

#### `IngestionServiceHost`

Responsibilities in this work package:

- continue to use enabled-provider validation for runtime execution
- consume a rules catalog whose provider identities are catalog-backed and canonicalized
- distinguish between known-provider validation and enabled-provider runtime validation

#### `StudioApiHost`

Responsibilities in this work package:

- consume the same provider-aware rules-loading path for read scenarios
- be able to read rules for all known providers without requiring ingestion runtime services
- integrate that read-oriented rules-loading path now rather than deferring it to a later package
- avoid taking on rule save/update behavior in this package while preserving a path for that later addition

### 3.2 Key design decisions

1. **Rule provider identity must be catalog-backed.**
   Rule loading must not trust provider names as free-form strings once `IProviderCatalog` is available.

2. **Unknown rule providers must fail fast.**
   A rules entry referencing an unknown provider is a startup contract violation.

3. **Known-but-disabled providers may still have rules.**
   Rules validity is based on known provider metadata, not on current runtime enablement.

4. **Read and write concerns should be separable.**
   The rules-reading path must be reusable by `StudioApiHost` in this package, and future write operations should layer on top rather than requiring a redesign.

5. **Canonical provider names must be preserved in outputs.**
   Once loaded, rules catalogs and diagnostics should use the canonical provider name from the Provider Model.

## 4. Functional requirements

### FR1 - Validate rule provider keys against `IProviderCatalog`

All provider keys encountered during rule loading must be resolved through `IProviderCatalog`.

If a provider key does not resolve to known provider metadata, rule loading must fail deterministically with a diagnosable validation error.

### FR2 - Canonicalize rule provider names through the Provider Model

Rule provider names must be canonicalized to `ProviderDescriptor.Name` during loading.

The rules subsystem must not continue to propagate raw provider strings from configuration or rule storage once they have been resolved through the catalog.

### FR3 - Distinguish known-provider validation from enabled-provider validation

The system must distinguish between:

- whether a provider is known in shared provider metadata
- whether a provider is enabled for ingestion runtime execution

Rule validity must depend on provider metadata, not on current runtime enablement.

### FR4 - Allow rules for known-but-disabled providers

Rules may exist for providers that are known in metadata but not currently enabled for ingestion runtime execution.

This is required to support development-time rule discovery and future Studio scenarios across all known providers.

### FR5 - Keep ingestion runtime startup validation intact

The existing ingestion runtime validation of enabled providers against metadata and runtime registrations must remain in place.

This work package strengthens rule loading; it does not replace runtime enablement validation.

### FR6 - Introduce a reusable read-oriented rules-loading path

The rules subsystem must expose a reusable read-oriented service boundary that:

- can load rules for all known providers
- can be consumed by `IngestionServiceHost`
- can be consumed by `StudioApiHost`
- does not require ingestion runtime-only services

### FR6a - Integrate read-oriented rules loading into `StudioApiHost` now

`StudioApiHost` must integrate the reusable read-oriented rules-loading path in this package.

This means the host must be able to load and expose rules for all known providers without requiring ingestion runtime registrations, queue services, blob services, or Elasticsearch dependencies that are not required for rule reading.

### FR7 - Support future Studio save/update operations by preserving clean separation

Although save/update operations are out of scope now, the design must preserve a future path for them by:

- separating read-oriented contracts from future write-oriented contracts
- avoiding host-specific assumptions in rules-loading services
- avoiding an API shape that assumes all rule access is ingestion-only or read-only forever

### FR8 - Keep `StudioApiHost` read-only in this package

`StudioApiHost` may read rules for all known providers in this package, but it must not implement save/update/delete rule operations yet.

### FR9 - Canonicalize diagnostics and catalogs

Any diagnostics, logs, or catalogs produced by the rules-loading path must use canonical provider names from the Provider Model.

### FR10 - Add full automated coverage for provider-aware rule loading

Automated tests must cover:

- unknown provider rejection
- case-insensitive provider lookup during rules loading
- canonical output names in loaded rules catalogs
- allowed rules for known-but-disabled providers
- continued runtime validation for enabled providers
- reusable read-oriented rules-loading composition suitable for Studio scenarios

## 5. Non-functional requirements

### NFR1 - Onion architecture

Provider-aware rule loading must preserve the repository's onion architecture and dependency direction.

### NFR2 - Diagnosability

Validation failures for unknown rule providers or invalid provider-name usage must produce clear, diagnosable messages and logs.

### NFR3 - Determinism

Rules loading and provider-name canonicalization must be deterministic.

### NFR4 - Extensibility for future writes

The design must not make future save/update operations harder by collapsing read and write concerns into a single ingestion-specific implementation.

### NFR5 - Runtime separation

`StudioApiHost` must be able to read rules without requiring queue, blob, Elasticsearch, or ingestion runtime provider services.

## 6. Data and contract model

### 6.1 Provider identity in rules

The rules subsystem must treat provider identity as:

- input: raw provider key from rule storage/configuration
- resolution: lookup through `IProviderCatalog`
- output: canonical provider name from `ProviderDescriptor.Name`

### 6.2 Read-oriented rules access model

The read-oriented rules path should expose a model capable of:

- enumerating providers with loaded rules
- returning rules by canonical provider name
- preserving validation results and diagnostics
- being reused by multiple hosts

### 6.3 Future write-oriented extension point

The design should leave room for future write/update support by allowing a separate write-facing abstraction later, for example:

- rule persistence writer
- rule update command service
- rule authoring validation service

This write-facing capability must remain out of scope for this package.

## 7. Interfaces & integration

### 7.1 `IngestionServiceHost`

`IngestionServiceHost` should integrate the shared Provider Model into its rules-loading path so rule providers are validated and canonicalized through `IProviderCatalog`.

### 7.2 `StudioApiHost`

`StudioApiHost` must reuse the same provider-aware rules-loading path in read mode in this package so it can inspect rules for all known providers.

The host integration should be designed so that a later write-oriented rule service can be added alongside the read path rather than forcing a redesign of the Studio-facing contracts.

### 7.3 Rules source integration

Rules sources such as App Configuration-backed loading must remain supported, but provider keys from those sources must be validated and canonicalized through the Provider Model.

## 8. Observability

The implementation should log:

- count of loaded providers and rules
- rejected or invalid rule entries
- unknown provider identities encountered during loading
- canonical provider names used in final catalogs

## 9. Security & compliance

- this package does not introduce new secret flows
- future save/update operations must be designed with authorization and auditability in mind, but that is deferred

## 10. Testing strategy

Testing must cover:

1. rules loading rejects unknown providers
2. provider keys are matched case-insensitively and canonicalized
3. loaded rules catalogs use canonical provider names
4. known-but-disabled providers may still have rules
5. enabled-provider runtime validation still behaves correctly
6. Studio-compatible read composition does not require ingestion runtime services

## 11. Rollout / migration

1. integrate `IProviderCatalog` into rules loading
2. canonicalize provider names during rule loading
3. fail fast for unknown provider rule groups
4. preserve separate runtime enablement validation for ingestion execution
5. expose and integrate a reusable read-oriented rules-loading path for `StudioApiHost`
6. defer save/update rule operations to a later work package
7. update wiki and work package documentation to reflect the strengthened rule-loading model

## 12. Open questions

1. Should the read-oriented rules-loading service be extracted from existing ingestion rules components, or should a new host-neutral facade be introduced over them?
2. For `StudioApiHost`, should future rule save/update operations use the same provider-aware catalog service with separate write commands, or a distinct write service entirely?
3. Should the system surface providers with no rules explicitly in Studio read models, or only providers that currently have rules loaded?
