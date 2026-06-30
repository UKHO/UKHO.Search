# Specification: WP100 Remote Ingestion Queue Contracts Boundary

Target output path: `dev/work-packages/100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md`

Date: 2026-06-30

Source material:
- [../../specs/next-gen-arc01-wp.md](../../specs/next-gen-arc01-wp.md)
- [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the boundary for a new dependency-light .NET assembly named `UKHO.Search.Ingestion.Contracts`. The assembly exists to let remote .NET producers construct, validate, and serialize ingestion queue messages without taking a dependency on the ingestion runtime, provider implementations, host projects, or deployment-specific queue clients.

WP100 does not extract the message types yet. It establishes the contractual and architectural rules that later work packages must follow when the queue-message wire types move into the new assembly.

### 1.2 Scope

In scope for WP100:
- Define the new project and package identity.
- Define its architectural position in the solution and onion boundaries.
- Define target framework, nullable, documentation, and semantic-versioning expectations.
- Define allowed and forbidden dependencies.
- Define the validation approach that proves the assembly remains dependency-light.
- Define the intended consumer profile for the package.

Out of scope for WP100:
- Implementing queue-message type extraction.
- Adding builders, helpers, or validators beyond boundary placeholders.
- Submitting messages to Azure Queue Storage or any other transport.
- Provider discovery, provider registration, Studio integration, React integration, or runtime pipeline changes.
- Journal, replay, dead-letter, or `CanonicalDocument` concerns.

### 1.3 Stakeholders

- Search platform maintainers who own ingestion contracts and compatibility.
- Remote .NET producers that need to emit valid ingestion queue messages.
- Ingestion runtime owners who will later consume the extracted contract assembly.
- Test owners responsible for compatibility fixtures and dependency-audit checks.

### 1.4 Definitions

- Remote producer: A non-Search .NET process that already knows where to submit a provider queue message and only needs the message contract.
- Contract assembly: A package containing message DTOs, serialization settings, and validation behavior, but no transport or runtime services.
- Wire contract: The exact JSON shape and validation behavior accepted by ingestion queues.
- Dependency-light: No references to Search runtime projects, Azure SDKs, storage clients, logging, or other infrastructure-oriented libraries.

## 2. System context

### 2.1 Current state

The current queue-message contract is embedded inside the ingestion runtime project instead of a separate contract assembly.

Evidence checked:
- [../../src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj](../../src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj) currently targets `net10.0` and references [../../src/UKHO.Search/UKHO.Search.csproj](../../src/UKHO.Search/UKHO.Search.csproj).
- Queue-message DTOs and serializer options live under [../../src/UKHO.Search.Ingestion/Requests](../../src/UKHO.Search.Ingestion/Requests) and [../../src/UKHO.Search.Ingestion/Requests/Serialization](../../src/UKHO.Search.Ingestion/Requests/Serialization).
- Runtime provider contracts consume `IngestionRequest` directly via [../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs](../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs).
- Queue polling in [../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs) deserializes provider queue message text into `IngestionRequest` and routes on `RequestType`.
- Producer-style helper code already exists in tooling form at [../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs](../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs), and it also derives security tokens through [../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs](../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs).
- Existing behavior is guarded by JSON and validation tests in [../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs](../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs), [../../test/UKHO.Search.Ingestion.Tests/IngestionPropertyListTests.cs](../../test/UKHO.Search.Ingestion.Tests/IngestionPropertyListTests.cs), and [../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs](../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs).

This means external producers currently have no narrow package to reference. They would have to depend on a runtime project with broader Search-domain coupling than their use case requires.

### 2.2 Proposed state

Introduce a new domain-layer assembly and NuGet package:
- Project path: `src/UKHO.Search.Ingestion.Contracts/UKHO.Search.Ingestion.Contracts.csproj`
- Package ID: `UKHO.Search.Ingestion.Contracts`
- Assembly name: `UKHO.Search.Ingestion.Contracts`
- Root namespace: `UKHO.Search.Ingestion.Contracts`

The new assembly will become the sole home for ingestion queue-message contracts and contract-owned serialization settings. It will be consumable by:
- Remote .NET producers outside the Search solution.
- Search runtime projects that currently use the embedded contract types.
- Test projects validating wire compatibility.

The new assembly will not submit queue messages and will not discover providers. Deployment-specific routing, queue naming, authentication, and transport concerns remain outside the package boundary.

### 2.3 Assumptions

- The first external producer audience is .NET only.
- Queue submission mechanics are already solved in consuming environments and are not part of this contract package.
- Wire compatibility with the current JSON contract is more important than minimizing internal namespace churn.
- Search runtime projects can reference a lower target framework contract assembly without functional loss.

### 2.4 Constraints

- The package must remain independent of Studio, React, Blazor, provider SPI, queue clients, Azure SDKs, storage SDKs, SQL, App Configuration, Aspire, Elasticsearch, runtime pipeline abstractions, and journaling concerns.
- The package must preserve the existing queue JSON shape and validation semantics when WP101 performs the extraction.
- The package must remain usable by third-party .NET producers without requiring solution-internal dependency graphs.
- The package must follow repository documentation standards, including XML documentation for public API and developer-level comments for internal implementation.

## 3. Component / service design (high level)

### 3.1 Components

WP100 defines three high-level deliverables:

1. `UKHO.Search.Ingestion.Contracts` project
   - Contains only queue-message contract types and contract-owned serialization utilities once later work packages land.
   - Contains no transport, storage, provider, or runtime orchestration logic.

2. Boundary documentation
   - Documents the intended consumers, allowed dependencies, forbidden dependencies, and package expectations.

3. Dependency audit validation
   - A targeted test or build-time assertion that fails if project references or package references violate the boundary.

### 3.2 Data flows

Current flow:
1. A producer or emulator constructs an `IngestionRequest` using types from `UKHO.Search.Ingestion`.
2. The producer serializes JSON with `IngestionJsonSerializerOptions.Create()`.
3. The message is submitted to a provider queue by deployment-specific code.
4. Ingestion runtime deserializes the message text back into `IngestionRequest`.

Target flow after Arc 01:
1. A remote producer references `UKHO.Search.Ingestion.Contracts` only.
2. The producer constructs a queue-message DTO from the contracts package.
3. The producer serializes JSON using package-owned serializer options or facade APIs.
4. The producer submits JSON through its own queue client or deployment-specific transport.
5. Search runtime consumes the same contract assembly for deserialization and processing.

### 3.3 Key decisions

- Architectural position: `UKHO.Search.Ingestion.Contracts` is a Domain-layer contract assembly because it defines pure message shape and validation rules with no outward dependencies.
- Namespace direction: public types should live under `UKHO.Search.Ingestion.Contracts` so the package boundary is explicit to internal and external consumers.
- Target framework: the package should target `net8.0` initially so current `net10.0` solution projects can consume it while external producers get an LTS baseline.
- Package maturity: the first publish should use semantic version `0.1.0` until extraction, migration, guidance, and compatibility fixtures are complete; the first locked wire-contract release should be `1.0.0`.
- Distribution decision: prerelease packages before `1.0.0` should be distributed through internal feeds only.
- Compatibility-marker decision: the visible contract compatibility marker is required, but its implementation belongs to WP102 alongside producer-safe helper APIs.
- Dependency policy: the project should have no project references and no non-BCL package references.

## 4. Functional requirements

FR1. The solution shall introduce a new project at `src/UKHO.Search.Ingestion.Contracts/UKHO.Search.Ingestion.Contracts.csproj`.

FR2. The project shall build independently of host, infrastructure, service, provider, and Studio projects.

FR3. The package shall be explicitly positioned for remote queue producers only.

FR4. The package shall own the queue-message wire contract and the serializer configuration required to produce and consume that contract.

FR5. The package shall not own queue submission, queue naming, queue selection, authentication, provider discovery, provider registration, or provider execution behavior.

FR6. The project file shall declare nullable reference types as enabled.

FR7. The project file shall enable XML documentation generation so package consumers receive API documentation in IntelliSense and packaged artifacts.

FR8. The public API surface shall use the `UKHO.Search.Ingestion.Contracts` root namespace.

FR9. The package shall be versioned independently using semantic versioning.

FR10. A dependency-audit test or equivalent automated validation shall fail if forbidden package references or project references are introduced.

FR11. The package documentation shall explicitly state the allowed dependencies, intended consumers, and non-goals.

FR12. Any prerelease package prior to `1.0.0` shall be published to internal package feeds only.

FR13. The package roadmap shall reserve an explicit compatibility marker as a required WP102 deliverable rather than implementing it in WP100.

## 5. Non-functional requirements

NFR1. The package shall remain dependency-light, meaning no references to any Search solution project or infrastructure/client package.

NFR2. The package shall remain transport-agnostic.

NFR3. The package shall be deterministic to build in isolation from the rest of the ingestion runtime.

NFR4. The package shall preserve future wire-compatibility obligations by treating JSON field names, enum tokens, and validation behavior as a versioned public contract.

NFR5. The package shall be documented to the same standard as any externally consumed library in this repository.

NFR6. The package shall be small enough to serve as a producer dependency rather than an SDK bundle.

## 6. Data model

WP100 does not introduce new DTO shapes, but it fixes the ownership boundary for the following contract families that later work packages will move into the package:
- Message envelope types.
- Operation payload types.
- Property and file metadata types.
- Serialization settings and converters required for the queue wire format.

The package boundary explicitly excludes these data families:
- Provider descriptors and provider catalogs.
- Provider service interfaces such as `IIngestionDataProvider`.
- Studio provider contracts.
- Journal, replay, dead-letter, and outcome records.
- Runtime pipeline documents such as `CanonicalDocument`.
- File-share security-token policy inputs and outputs.

## 7. Interfaces & integration

### 7.1 Allowed dependencies

The new project may depend on:
- The .NET base class library.
- `System.Text.Json` APIs that ship with the target framework.

The project shall not reference:
- Any Search solution project.
- Azure SDK packages.
- Storage, queue, SQL, or Elasticsearch client libraries.
- Logging abstractions.
- Aspire, App Configuration, or configuration-provider libraries.
- Studio, Blazor, React, RulesWorkbench, or provider implementation assemblies.

### 7.2 External integration model

The contracts package exposes construction, validation, and serialization primitives only. External producers are responsible for:
- Selecting the correct provider queue.
- Authenticating to their queue or transport.
- Submitting the JSON payload.
- Managing retries and operational telemetry in their own environment.

During prerelease iterations before `1.0.0`, external distribution is not assumed. Integration outside the organization should wait until the wire contract has completed extraction, migration, and compatibility hardening.

### 7.3 Internal integration model

Later work packages will update runtime and test projects to consume the new package instead of `UKHO.Search.Ingestion.Requests`. WP100 does not require those references to move yet, but it defines that migration target.

The explicit compatibility marker is part of that future integration story, but it is intentionally deferred to WP102 so it ships together with the producer-facing helper surface.

## 8. Observability (logging/metrics/tracing)

The contracts package shall not take a dependency on logging, metrics, or tracing abstractions.

Observability responsibilities remain with callers:
- Producers log their own submission behavior.
- Runtime hosts log queue polling, deserialization failures, and processing outcomes.

The only validation artifact inside the contract boundary is automated test output demonstrating that dependency rules and independent build requirements still hold.

## 9. Security & compliance

The contracts package shall not perform authentication, authorization, token derivation, or secret retrieval.

Security-specific rules for WP100:
- Security tokens remain producer-supplied payload data, not derived package behavior.
- The package shall not embed queue endpoints, credentials, or tenant-specific routing logic.
- The package shall not introduce persistence of payloads, poison-message handling, or journal storage.

Compliance note:
- Because the package may carry security token values inside message DTOs, documentation must clearly state that it is a data-shaping library, not a security-policy engine.

## 10. Testing strategy

WP100 validation must include:
- Independent build of `UKHO.Search.Ingestion.Contracts`.
- An automated dependency-audit test that fails if the project gains a `ProjectReference` or forbidden `PackageReference`.
- A documentation review confirming that the package description and dependency rules are explicit.

Recommended implementation of the dependency audit:
- Load the `.csproj` as XML in a targeted test.
- Assert that no `ProjectReference` items exist.
- Assert that no `PackageReference` items exist other than packages explicitly allowed by the specification.

WP100 does not require golden JSON fixtures yet. Those belong to WP101.

## 11. Rollout / migration

WP100 rollout is preparatory.

Implementation sequence:
1. Add the new project and package metadata.
2. Add XML documentation and repository-standard developer comments.
3. Add the dependency-audit validation.
4. Publish the package as an internal prerelease if packaging infrastructure is available.
5. Leave runtime consumers unchanged until WP101 and WP103 perform extraction and migration.

Migration impact:
- No producer behavior changes occur in WP100.
- No queue-message wire changes are allowed in WP100.
- Internal namespace churn is acceptable later if it is required to make the contract boundary explicit.
- Broader external producer consumption should begin only after the `1.0.0` contract baseline is ready.

## 12. Decisions captured

The following decisions are closed for WP100:
- Prerelease package distribution before `1.0.0` is internal-only.
- The explicit contract compatibility marker is required, but it will be introduced in WP102 rather than WP100.
- WP100 remains boundary-defining and does not expand into producer helper APIs beyond documenting the reserved follow-on work.