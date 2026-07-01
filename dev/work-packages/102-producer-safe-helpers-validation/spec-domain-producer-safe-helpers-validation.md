# Specification: WP102 Producer-Safe Helpers, Builders, And Validation

Target output path: `dev/work-packages/102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md`

Date: 2026-06-30

Source material:
- [../../specs/next-gen-arc01-wp.md](../../specs/next-gen-arc01-wp.md)
- [../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md](../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md)
- [../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md](../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md)
- [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines WP102, which adds producer-safe authoring helpers on top of the extracted `UKHO.Search.Ingestion.Contracts` queue-message contract.

WP100 established the dependency-light package boundary. WP101 moved the queue-message DTOs, serializers, and fixtures into that package. WP102 makes the package easier for remote .NET producers to use safely by adding dependency-free factories, typed property helpers, builder support, a non-throwing validator, a serializer facade, and a visible contract-version marker.

### 1.2 Scope

In scope for WP102:
- Add dependency-free helper APIs for constructing `IndexItem`, `DeleteItem`, and `UpdateAcl` envelopes.
- Add typed helper APIs for constructing `IngestionProperty` values correctly.
- Add an `IndexRequestBuilder` or equivalent producer-safe builder surface for common `IndexItem` authoring.
- Add a non-throwing validation surface that reports contract errors without requiring exception-driven control flow.
- Add a serializer facade so producers can use the canonical queue-message serializer without manually wiring converter options.
- Add a visible contract-version marker so compatibility can be reasoned about explicitly.
- Add tests that prove helper parity with the canonical DTO and serializer behavior introduced by WP101.

Out of scope for WP102:
- Queue submission helpers, queue clients, Azure SDK integrations, or provider-queue routing.
- Security-token derivation logic such as File Share business-unit token policy.
- Provider discovery, provider SPI, runtime pipeline behavior, journaling, replay, dead-letter flow, or `ShadowId` generation.
- Browser-host-, Studio-, Workbench-, or host-facing SDK layers.

### 1.3 Stakeholders

- Search platform maintainers who own contract compatibility and package usability.
- Remote .NET producers who need a safe, low-friction authoring surface for queue messages.
- Internal tooling owners whose current local helper code can inform the supported authoring ergonomics.
- Test owners responsible for helper, validator, and serializer regression coverage.

### 1.4 Definitions

- Producer-safe helper: a convenience API that reduces accidental invalid message construction without taking ownership of transport or deployment concerns.
- Serializer facade: a package-owned API that exposes canonical serialization entry points without requiring callers to instantiate `JsonSerializerOptions` manually.
- Contract version marker: a visible package-owned compatibility marker that allows producers and maintainers to discuss queue-message compatibility explicitly.
- Non-throwing validator: an API that reports one or more contract errors without relying on thrown exceptions for expected validation failures.

## 2. System context

### 2.1 Current state

`UKHO.Search.Ingestion.Contracts` now owns the queue-message DTOs, serializer configuration, and producer-facing README, but it still expects consumers to compose valid messages manually from the raw contract types.

Evidence checked:
- [../../src/UKHO.Search.Ingestion.Contracts/README.md](../../src/UKHO.Search.Ingestion.Contracts/README.md) documents the current package surface as DTOs plus canonical serializer options, with queue submission and token derivation explicitly out of scope.
- [../../dev/work-packages/101-queue-message-json-contract/spec-domain-queue-message-json-contract.md](../../dev/work-packages/101-queue-message-json-contract/spec-domain-queue-message-json-contract.md) defines the extracted DTO and JSON contract as complete after WP101.
- [../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs](../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs) contains local helper logic that assembles an `IndexItem` request, injects a `BusinessUnitName` property, derives security tokens, and wraps JSON exceptions.
- [../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs](../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs) shows that token derivation currently lives outside the contracts package and is tied to File Share–specific policy.
- [../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs](../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs) proves that in-repo helper ergonomics exist today, but they are tied to tooling and provider-specific policy rather than to the neutral contracts package.

This means WP101 delivered the canonical contract, but producers still need to understand several low-level DTO rules and serializer details to create valid messages safely.

### 2.2 Proposed state

After WP102:
- `UKHO.Search.Ingestion.Contracts` exposes a producer-safe helper surface alongside the raw DTO surface.
- Producers can create valid envelopes using package-owned static factories and typed property helpers.
- Producers can build complex `IndexItem` payloads through a dedicated builder rather than manually mutating DTO collections.
- Producers can validate a message and receive structured contract errors without depending on exception-driven flows.
- Producers can serialize through a package-owned facade rather than manually calling `IngestionJsonSerializerOptions.Create()`.
- The package exposes a visible contract-version marker that can be referenced in tests, samples, documentation, and future compatibility policy.

The package remains intentionally narrow: it helps construct, validate, and serialize queue-message content, but it still does not own queue delivery, authentication, provider selection, token derivation, or runtime-only concepts.

### 2.3 Assumptions

- Remote producer ergonomics matter enough to justify helper APIs, but not enough to justify a transport SDK in this work package.
- The raw DTOs and validation semantics delivered in WP101 remain the canonical contract baseline that helper APIs must preserve rather than bypass.
- Helper APIs should stay dependency-free and should not reintroduce provider-specific policy from tooling code.
- The producer-facing helper surface should be understandable from package documentation and IntelliSense without requiring repository-internal knowledge.

### 2.4 Constraints

- The package must remain dependency-light and must not add project references or external package references.
- Helper APIs must not submit queue messages, discover providers, calculate File Share security tokens, generate `ShadowId`, or understand journal/dead-letter storage.
- Validation and serializer helper behavior must remain semantically aligned with the raw DTO contract defined in WP101.
- Any builder, factory, or validator API must remain usable by third-party .NET producers without requiring runtime-only types.

## 3. Component / service design (high level)

### 3.1 Components

WP102 introduces or updates six high-level component areas:

1. Envelope factories
   - `CreateIndex`
   - `CreateDelete`
   - `CreateAclUpdate`

2. Typed property helpers
   - helper APIs for `string`, `text`, `DateTime`, `string[]`, and other currently supported property-value types

3. `IndexRequestBuilder`
   - a producer-safe builder for id, timestamp, security tokens, files, and properties

4. Non-throwing validation surface
   - a package-owned validator result model and contract-error model

5. Serializer facade
   - package-owned serialize and deserialize entry points that preserve canonical options

6. Contract version marker
   - a visible compatibility marker exposed from the package surface

### 3.2 Data flows

Current flow after WP101:
1. A producer manually constructs raw DTOs from `UKHO.Search.Ingestion.Contracts`.
2. The producer must know which DTO constructor and collection rules matter.
3. The producer serializes via `JsonSerializer` plus `IngestionJsonSerializerOptions.Create()`.
4. Validation failures are typically surfaced through exceptions.

Target flow after WP102:
1. A producer chooses either raw DTOs or package-owned helper APIs.
2. The producer creates typed properties and envelope payloads through factories or an `IndexRequestBuilder`.
3. The producer optionally validates the payload through a non-throwing validator.
4. The producer serializes through a package-owned serializer facade or through raw `JsonSerializer` parity paths.
5. The runtime continues to consume the same canonical JSON wire contract introduced in WP101.

### 3.3 Key decisions

- Surface-shape decision: the package should expose helper APIs in addition to, not instead of, the raw DTO contract.
- Dependency decision: helper APIs must remain dependency-free and must not pull in provider or transport abstractions.
- Policy-boundary decision: helper APIs must not absorb File Share–specific token derivation, `BusinessUnitName` conventions, or other provider-local policies currently found in emulator tooling.
- Validation decision: WP102 must add a non-throwing validator, but raw DTO constructors and deserialization should continue to preserve their existing exception-based enforcement semantics.
- Validation result decision: the non-throwing validator should expose a flat core error model with `code`, `path`, and `message` so external producers can log, display, serialize, and test errors without depending on a UI-oriented grouping shape.
- Serializer decision: the facade should wrap the canonical serializer settings already defined by the package rather than introduce an alternative wire-contract implementation.
- Builder decision: the producer-safe builder should expose a simple terminal authoring model centered on `Build()` and `TryBuild(...)`, and should not require incremental live validation state in the first cut.
- Compatibility decision: the visible contract-version marker belongs in the contracts package itself so producers and maintainers can reference one obvious compatibility symbol.
- Contract-version decision: the first implementation should expose a simple visible constant or string surface rather than a richer compatibility descriptor so tests, samples, and documentation can reference one obvious marker without adding unnecessary type complexity.
- Documentation decision: the package README and follow-on producer guidance should describe helper APIs as authoring conveniences, not as queue submission infrastructure.

## 4. Functional requirements

FR1. `UKHO.Search.Ingestion.Contracts` shall expose package-owned static factory APIs for creating `IndexItem`, `DeleteItem`, and `UpdateAcl` message envelopes.

FR2. The package shall expose typed property helper APIs for the supported property types so producers can create `IngestionProperty` instances without manually pairing `Type` and `Value` incorrectly.

FR3. The package shall expose an `IndexRequestBuilder` or equivalent producer-safe builder surface for document id, timestamp, security tokens, files, and properties.

FR4. The builder surface shall preserve the same validity rules as the underlying DTO contract and shall not weaken required-field or collection constraints.

FR5. The package shall expose a non-throwing validation API that returns structured contract errors for invalid messages.

FR6. The validation API shall be usable independently of queue submission or runtime-only abstractions.

FR6a. The validation API shall expose a flat core error model containing, at minimum, an error code, a contract path, and a human-readable message for each validation failure.

FR7. The package shall expose a serializer facade so producers can serialize and deserialize queue messages without manually registering converter options.

FR8. The serializer facade shall emit and consume the same JSON contract as `JsonSerializer` with `IngestionJsonSerializerOptions.Create()`.

FR9. The package shall expose a visible contract-version marker that can be inspected by producers, tests, and documentation.

FR9a. The initial contract-version marker shall be exposed through a simple constant or string surface rather than a richer compatibility object model.

FR10. Helper APIs shall not derive File Share or provider-specific security tokens.

FR11. Helper APIs shall not generate `ShadowId`, journal identity, dead-letter metadata, or other runtime-owned values.

FR12. Helper APIs shall not submit queue messages or own queue naming, authentication, or deployment routing concerns.

FR13. Existing package README guidance shall be updated as needed so the supported helper surface is discoverable and explained in current-state terms.

## 5. Non-functional requirements

NFR1. WP102 shall preserve the dependency-light boundary established in WP100 and exercised in WP101.

NFR2. Helper APIs shall remain deterministic and side-effect-free except for object construction, validation, and serialization.

NFR3. Helper APIs shall remain wire-compatible with the canonical DTO and JSON contract introduced in WP101.

NFR4. Validation and serializer tests shall make helper regressions obvious without requiring a full-solution test run.

NFR5. The helper surface shall remain comprehensible to external producers from package documentation and IntelliSense alone.

NFR6. The contracts package shall remain free of queue-client, provider-policy, logging, storage, Studio, UI, and host dependencies after the new helper APIs are added.

NFR7. The first-cut helper APIs shall prefer simple authoring and inspection semantics over richer but heavier object models when both options preserve the same queue-message compatibility guarantees.

## 6. Data model

### 6.1 Data model additions

WP102 introduces package-owned authoring and validation models in addition to the existing DTOs. Likely additions include:
- helper/factory entry points for envelope creation
- property-factory entry points for typed `IngestionProperty` creation
- builder state and result types for `IndexRequestBuilder`
- validation result and flat contract-error models with `code`, `path`, and `message`
- serializer facade request/response surface where needed
- contract version marker model or constant surface

### 6.2 Data model preservation rules

The helper layer must preserve the existing DTO model rules introduced by WP101, including:
- exact-one envelope payload behavior
- `IndexRequest` security-token and file validation
- `IngestionPropertyList` name normalization and uniqueness behavior
- lower-case property-type token serialization

### 6.3 Data model exclusions

WP102 shall not introduce data models for:
- queue submission requests or queue client configuration
- provider descriptors or provider registration
- File Share token policy or `BusinessUnitName` conventions
- journal, replay, dead-letter, outcome, or supersession records
- runtime-only pipeline or canonical-document concerns

## 7. Interfaces & integration

### 7.1 Internal integration points

The following internal surfaces inform WP102 and may be used as evidence or migration targets:
- [../../src/UKHO.Search.Ingestion.Contracts](../../src/UKHO.Search.Ingestion.Contracts) as the canonical home of the helper APIs
- [../../test/UKHO.Search.Ingestion.Contracts.Tests](../../test/UKHO.Search.Ingestion.Contracts.Tests) as the focused test project for package-owned behavior
- [../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs](../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs) as existing in-repo evidence of desired authoring ergonomics that must be generalized and de-policy-fied

### 7.2 External integration model

Remote producers should be able to:
- construct a valid queue-message envelope using only `UKHO.Search.Ingestion.Contracts`
- validate the envelope without depending on exceptions for expected invalid states
- serialize the envelope without hand-registering converter options
- remain responsible for queue transport, authentication, retries, routing, and token derivation policy outside the package

The preferred first-cut calling pattern should be simple enough for producer code to read naturally:
- use factories or typed property helpers for straightforward messages
- use `IndexRequestBuilder` when incremental authoring is clearer
- use `TryBuild(...)` or validator APIs when callers need non-throwing failure handling

### 7.3 Compatibility surface

The helper layer must preserve compatibility with:
- the DTO and JSON contract defined in WP101
- the fixture-backed examples already published in the contracts test project
- future guidance that distinguishes core contracts from any later optional queue-submission package such as a potential `UKHO.Search.Ingestion.AzureQueues`

## 8. Observability (logging/metrics/tracing)

WP102 shall not introduce package-level logging, metrics, or tracing dependencies.

Validation and serializer surfaces may expose structured error information for callers, but runtime observability remains the responsibility of the consuming process rather than of the contracts package.

## 9. Security & compliance

WP102 shall not take ownership of authentication, authorization, queue credentials, or provider-security policy.

The package may help producers avoid malformed payloads, but it shall not imply that a syntactically valid message is authorized for submission to any particular queue or provider.

Security-token derivation remains explicitly upstream in this version and must not be inferred or silently automated inside helper APIs.

## 10. Testing strategy

WP102 validation should include:
- unit tests for static factories across success and invalid-input paths
- unit tests for typed property helpers and their parity with raw DTO construction
- unit tests for `IndexRequestBuilder` success and failure paths
- unit tests for non-throwing validator result shapes and reported errors
- unit tests proving serializer facade parity with raw `JsonSerializer` plus package options
- focused tests proving the contract-version marker is visible and stable in the intended surface

Where practical, tests should also prove that provider-specific policy such as File Share security-token derivation remains outside the contracts package.

## 11. Rollout / migration

WP102 should be introduced as a backward-compatible extension of the WP101 package surface.

Expected rollout characteristics:
- existing raw DTO consumers can continue using the package without adopting the new helper APIs immediately
- producer guidance can start recommending helper APIs as the preferred authoring surface once tests are in place
- broader in-repo consumer convergence remains primarily in WP103, though selective test or tooling uptake may inform the implementation

## 12. Open questions

No open questions remain at the specification level for WP102.

The following implementation decisions are now captured by the spec and should be treated as the working defaults unless later evidence forces a change:
- the validator exposes a flat core error model with `code`, `path`, and `message`
- the builder surface centers on `Build()` and `TryBuild(...)` rather than incremental live validation state
- the contract-version marker starts as a simple visible constant or string surface