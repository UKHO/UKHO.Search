# Specification: WP101 Queue Message Types And JSON Contract Extraction

Target output path: `dev/work-packages/101-queue-message-json-contract/spec-domain-queue-message-json-contract.md`

Date: 2026-06-30

Source material:
- [../../specs/next-gen-arc01-wp.md](../../specs/next-gen-arc01-wp.md)
- [../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md](../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md)
- [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines WP101, which extracts the ingestion queue-message wire types and their exact `System.Text.Json` contract into `UKHO.Search.Ingestion.Contracts`.

WP100 established the package boundary and dependency guardrails. WP101 makes that package useful by moving the current queue-message DTO surface and serializer behavior into the contracts assembly while preserving wire compatibility with the JSON that the active ingestion runtime already accepts.

### 1.2 Scope

In scope for WP101:
- Move or share the current queue-message DTO types into `UKHO.Search.Ingestion.Contracts`.
- Move or share the queue-message JSON serializer options and converters needed for exact wire compatibility.
- Preserve current validation behavior on the extracted request types.
- Update the active ingestion runtime and core contract tests so they consume the extracted contracts package.
- Add golden JSON fixtures for the supported message operations.

Out of scope for WP101:
- Producer-safe helper factories, builders, validators, or serializer facades beyond the existing contract-owned JSON options.
- Queue submission helpers, queue clients, or Azure SDK integrations.
- Security-token derivation logic.
- Journal, replay, dead-letter storage, `ShadowId`, or runtime pipeline redesign.
- Broad consumer migration across all tooling and retained edge projects; that belongs to WP103.

### 1.3 Stakeholders

- Search platform maintainers who own queue-message compatibility.
- Ingestion runtime owners who must continue to deserialize queue messages without behavioral regression.
- Remote .NET producers who will eventually reference the contracts package directly.
- Test owners responsible for wire-compatibility fixtures and regression coverage.

### 1.4 Definitions

- Queue-message wire contract: the exact JSON shape, field names, enum tokens, converter behavior, and validation semantics accepted by ingestion.
- Golden fixture: a stable JSON sample used to prove serialization and deserialization compatibility across changes.
- Extracted contract: a type that is owned by `UKHO.Search.Ingestion.Contracts` rather than being embedded in the runtime ingestion project.

## 2. System context

### 2.1 Current state

The contracts package now exists, but it is only a boundary-establishing shell.

Evidence checked:
- [../../src/UKHO.Search.Ingestion.Contracts/UKHO.Search.Ingestion.Contracts.csproj](../../src/UKHO.Search.Ingestion.Contracts/UKHO.Search.Ingestion.Contracts.csproj) exists, targets `net8.0`, packs a README, and has no project or package references.
- Queue-message DTOs are still owned by [../../src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs](../../src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs), [../../src/UKHO.Search.Ingestion/Requests/IndexRequest.cs](../../src/UKHO.Search.Ingestion/Requests/IndexRequest.cs), [../../src/UKHO.Search.Ingestion/Requests/IngestionProperty.cs](../../src/UKHO.Search.Ingestion/Requests/IngestionProperty.cs), and their neighboring request-model files.
- Queue-message JSON configuration is still owned by [../../src/UKHO.Search.Ingestion/Requests/Serialization/IngestionJsonSerializerOptions.cs](../../src/UKHO.Search.Ingestion/Requests/Serialization/IngestionJsonSerializerOptions.cs) and related converters.
- Existing runtime-facing JSON and validation behavior is anchored by [../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs](../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs) and related tests.
- The active runtime still deserializes queue messages through `UKHO.Search.Ingestion.Requests.IngestionRequest`, not through `UKHO.Search.Ingestion.Contracts`.

This means WP100 created the package boundary, but the actual queue-message contract still lives in the runtime project that the package is meant to decouple.

### 2.2 Proposed state

After WP101:
- `UKHO.Search.Ingestion.Contracts` owns the queue-message DTOs, request enums, property/file models, and serializer configuration needed for the wire contract.
- The active ingestion runtime references `UKHO.Search.Ingestion.Contracts` for message deserialization and processing.
- Core JSON tests run against the contracts package rather than against a runtime-local copy.
- Golden JSON fixtures exist for `IndexItem`, `DeleteItem`, and `UpdateAcl` message envelopes.

The resulting state is still deliberately narrow. The package becomes the canonical home of the wire contract, but it does not yet become a convenience SDK for producers.

### 2.3 Assumptions

- The wire JSON accepted today is the compatibility baseline that WP101 must preserve.
- `net10.0` runtime projects can consume the `net8.0` contracts package without loss of functionality.
- The current DTO names and JSON field names are already the intended public wire contract unless the existing tests prove otherwise.
- A small amount of runtime-reference rewiring in the ingestion path is acceptable in WP101 when required to make extraction real.

### 2.4 Constraints

- The extracted contract must remain inside `UKHO.Search.Ingestion.Contracts` and must not pull provider, runtime, host, Studio, or infrastructure dependencies back in.
- Validation behavior must remain constructor/deserialization driven where it exists today.
- JSON property names, discriminator values, property-type tokens, and list serialization shape must remain wire-compatible.
- The contracts package must not gain Azure Queue, logging, App Configuration, Elasticsearch, or journal concepts while the DTOs move.

## 3. Component / service design (high level)

### 3.1 Components

WP101 introduces or updates four main component areas:

1. Contracts assembly DTO surface
   - `IngestionRequest`
   - `IngestionRequestType`
   - `IndexRequest`
   - `DeleteItemRequest`
   - `UpdateAclRequest`
   - `IngestionProperty`
   - `IngestionPropertyType`
   - `IngestionPropertyList`
   - `IngestionFile`
   - `IngestionFileList`

2. Contracts assembly JSON surface
   - serializer options factory
   - property-type converter
   - property-value converter
   - property-list converter

3. Runtime ingestion reference updates
   - the active ingestion runtime consumes the extracted contracts package rather than its local DTO copy

4. Wire-compatibility tests and fixtures
   - existing JSON tests redirected to the extracted contract
   - new golden fixtures for supported envelope operations

### 3.2 Data flows

Current flow:
1. A producer or emulator constructs DTOs from `UKHO.Search.Ingestion.Requests`.
2. JSON is serialized with the runtime-owned serializer options.
3. The ingestion runtime deserializes the message text back into runtime-owned DTO types.

Target flow after WP101:
1. Queue-message DTOs are owned by `UKHO.Search.Ingestion.Contracts`.
2. JSON is serialized and deserialized through contract-owned serializer options.
3. The active ingestion runtime consumes those same extracted contract types directly.
4. Tests verify both round-trip behavior and fixture compatibility against the extracted package.

### 3.3 Key decisions

- Ownership decision: WP101 moves the wire-contract ownership into `UKHO.Search.Ingestion.Contracts` rather than maintaining duplicate DTO definitions.
- Namespace decision: extracted queue-message types should live under `UKHO.Search.Ingestion.Contracts` so external and internal consumers share one obvious contract namespace.
- Compatibility decision: existing JSON field names such as `RequestType`, `IndexItem`, `DeleteItem`, `UpdateAcl`, `Id`, `Properties`, `SecurityTokens`, `Timestamp`, and `Files` remain unchanged.
- Validation decision: exact-one-payload validation for `IngestionRequest`, `IndexRequest` token and file validation, and property-name normalization behavior remain contract behavior, not runtime-only behavior.
- Extraction decision: the DTOs and converters move directly into `UKHO.Search.Ingestion.Contracts` as the canonical owner of the wire contract rather than being redefined in parallel.
- Migration decision: WP101 should avoid a long-lived compatibility shim in `UKHO.Search.Ingestion`; if a compile-time bridge is needed to keep the solution buildable during the refactor, it must be short-lived and removed within WP101 or immediately after the runtime references are updated.
- Fixture decision: golden JSON fixtures should live as explicit files in the contracts test project so the wire contract remains easy to inspect, diff, and reuse.
- Producer-guidance decision: WP101 must include an initial producer-facing documentation page that explains the new third-party authoring capability at the queue-message-contract level, even though broader guidance remains in scope for WP104.
- Migration-boundary decision: WP101 updates the active ingestion runtime and core JSON tests as needed to make extraction real, while WP103 completes the broader convergence across tooling and remaining consumers.

## 4. Functional requirements

FR1. `UKHO.Search.Ingestion.Contracts` shall own the queue-message DTO types currently required by the ingestion wire contract.

FR2. `UKHO.Search.Ingestion.Contracts` shall own the `System.Text.Json` options and converters required to serialize and deserialize the exact queue-message JSON accepted today.

FR3. The extracted DTO surface shall include `IngestionRequest`, `IngestionRequestType`, `IndexRequest`, `DeleteItemRequest`, `UpdateAclRequest`, `IngestionProperty`, `IngestionPropertyType`, `IngestionPropertyList`, `IngestionFile`, and `IngestionFileList`.

FR4. `IngestionRequest` shall preserve the exact-one-payload invariant and the `RequestType`-to-payload alignment validation currently enforced.

FR5. `IndexRequest` shall preserve current validation of `Id`, non-null `Properties`, non-empty `SecurityTokens`, non-empty token values, non-null `Files`, non-null file entries, and rejection of a first-class `Id` duplicate in `Properties`.

FR6. `IngestionPropertyList` shall preserve case-insensitive uniqueness and canonical lower-case property-name normalization.

FR7. `IngestionPropertyType` JSON tokens shall preserve current lower-case wire values.

FR8. The active ingestion runtime shall deserialize and process queue messages using the extracted contracts package rather than a runtime-local copy of the DTOs.

FR9. Existing core ingestion model JSON tests shall be updated to target the extracted contracts package.

FR10. Golden JSON fixtures shall exist for `IndexItem`, `DeleteItem`, and `UpdateAcl` message envelopes.

FR11. The contracts package shall remain free of project references and external package references while the DTOs are extracted.

FR12. WP101 shall provide a producer-facing documentation page describing the new third-party authoring capability exposed by `UKHO.Search.Ingestion.Contracts`.

FR13. The producer-facing page shall explain who the package is for, which queue-message operations and types are available, how a third-party .NET producer constructs valid payloads at the contract level, and which concerns remain explicitly out of scope.

FR14. The producer-facing page shall explain that queue submission, queue naming, authentication, provider queue selection, deployment topology, and security-token derivation remain external concerns rather than package behavior.

## 5. Non-functional requirements

NFR1. Wire compatibility shall be preserved for existing queue-message JSON.

NFR2. The extraction shall not widen the dependency surface of `UKHO.Search.Ingestion.Contracts`.

NFR3. The contracts package shall remain buildable independently after DTO extraction.

NFR4. The new test coverage shall make JSON and validation regressions obvious without requiring a full-solution test run.

NFR5. The extracted contract surface shall remain usable by remote producers without bringing in runtime-specific concepts.

NFR6. The producer-facing documentation page shall remain current-state, concise enough for package consumers to use directly, and aligned with the extracted contract surface actually delivered by WP101.

## 6. Data model

### 6.1 Types moving into the contracts package

WP101 shall move or share the following wire-contract model types into `UKHO.Search.Ingestion.Contracts`:
- `IngestionRequest`
- `IngestionRequestType`
- `IndexRequest`
- `DeleteItemRequest`
- `UpdateAclRequest`
- `IngestionProperty`
- `IngestionPropertyType`
- `IngestionPropertyList`
- `IngestionFile`
- `IngestionFileList`

### 6.2 Behavior that remains part of the data model

The extracted data model is not a passive DTO set. It also owns:
- constructor and deserialization validation
- property-name normalization inside `IngestionPropertyList`
- typed property-value serialization behavior
- omission of `null` envelope payloads and required field behavior where already expressed through the current model

### 6.3 Data model exclusions

WP101 shall not move or introduce:
- `CanonicalDocument`
- provider descriptors or provider catalogs
- provider SPI such as `IIngestionDataProvider`
- journal, replay, dead-letter, or outcome records
- queue client abstractions or submission helpers

## 7. Interfaces & integration

### 7.1 Internal integration points

The following internal surfaces are directly affected by WP101:
- `src/UKHO.Search.Infrastructure.Ingestion/Queue/` where the runtime currently deserializes `IngestionRequest`
- `src/UKHO.Search.Ingestion/Providers/` where provider contracts currently expose `IngestionRequest`
- `test/UKHO.Search.Ingestion.Tests/` where JSON and validation tests currently target runtime-owned DTOs

### 7.2 Compatibility surface

The wire contract must preserve:
- PascalCase JSON field names for envelope and payload properties
- lower-case property-type tokens such as `string`, `text`, and other existing type tokens
- JSON array serialization for files and properties where currently expected
- rejection of legacy payload property names that existing tests already prove invalid

### 7.3 Integration boundary for later work

WP101 creates the canonical contract surface that later work packages will extend and propagate:
- WP102 adds producer-facing helpers on top of the extracted contract
- WP103 updates the wider in-repo consumer set to converge on the extracted package
- WP104 documents remote-producer usage against the extracted package

### 7.4 Producer-facing documentation surface

WP101 shall introduce an initial producer-facing page for the extracted contract surface.

Preferred location:
- package-local guidance in `src/UKHO.Search.Ingestion.Contracts/README.md`

Minimum content:
- intended audience and scope of the package
- supported queue-message operations and their contract role
- the distinction between contract authoring and queue submission
- explicit exclusions such as provider discovery, queue routing, queue authentication, security-token derivation, journal identity, and runtime pipeline ownership

This page is not the full end-state producer guidance promised by WP104, but it must be sufficient for contributors and early adopters to understand the new third-party authoring capability introduced by the extracted contracts package.

## 8. Observability (logging/metrics/tracing)

WP101 does not add logging or tracing dependencies to the contracts package.

Observability for this work package remains test- and validation-oriented:
- build success for the contracts package
- passing targeted JSON tests
- passing golden-fixture tests
- passing dependency-boundary tests already introduced in WP100

Runtime logging behavior remains owned by the runtime hosts and infrastructure layers.

## 9. Security & compliance

WP101 preserves the current position that security tokens are payload data supplied by the producer side.

Security-specific constraints:
- token presence and non-empty token-value validation must remain intact on `IndexRequest` and `UpdateAclRequest`
- the contracts package must not start deriving, mutating by policy, or discovering security tokens
- the extraction must not introduce secret storage, queue authentication logic, or journal identity semantics

Compliance note:
- because the wire contract includes security-token payload data, compatibility changes to token validation remain contract-significant and must be treated as wire-breaking unless explicitly designed otherwise

## 10. Testing strategy

WP101 validation must include:
- independent build of `UKHO.Search.Ingestion.Contracts`
- passing dependency-boundary tests from WP100
- updated ingestion model JSON tests running against the extracted contracts package
- new golden-fixture tests for `IndexItem`, `DeleteItem`, and `UpdateAcl`

Recommended test structure:
- keep low-level contract and serializer tests close to the contracts package
- preserve targeted runtime tests that prove the ingestion runtime still deserializes through the extracted package
- store golden JSON fixture files as stable repository assets in the contracts test project

WP101 does not require the broader tool and emulator convergence tests that belong to WP103.

## 11. Rollout / migration

Recommended implementation sequence:
1. Move or share the DTO and serializer types into `UKHO.Search.Ingestion.Contracts`.
2. Update namespaces and references in the active ingestion runtime path so runtime deserialization uses the extracted package.
3. Update core JSON and validation tests to target the extracted package.
4. Add golden JSON fixture files proving envelope compatibility.
5. Add or refresh the initial producer-facing documentation page for the extracted package surface.
6. Re-run the contracts build, boundary tests, and targeted ingestion-model tests.

Migration notes:
- the extraction should avoid a long-lived duplicate DTO model inside `UKHO.Search.Ingestion`
- if temporary forwarding or re-exporting is required to keep the solution buildable during the change, it should be treated as a short-lived migration step rather than the final architecture and removed within WP101 or immediately after the runtime references are updated
- broader consumer convergence across tooling and retained edge projects is explicitly deferred to WP103

## 12. Decisions captured

- The DTO and converter ownership moves directly into `UKHO.Search.Ingestion.Contracts`.
- A long-lived compatibility shim in `UKHO.Search.Ingestion` is not acceptable; only a short-lived compile-time bridge is permitted if needed to keep the solution buildable during extraction.
- Golden JSON fixtures will live as explicit files in the contracts test project.
- WP101 includes an initial producer-facing documentation page describing the new third-party authoring capability at the contract level.
- Full producer onboarding, helper APIs, and broader compatibility guidance remain part of the later WP102-WP104 slices.