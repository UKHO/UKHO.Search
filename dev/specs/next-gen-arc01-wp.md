# Next-Gen Arc 01 Work Packages: Remote Ingestion Queue Contracts

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 01 extracts the ingestion queue-message wire contract into a narrow .NET assembly for remote queue producers. The target consumer is a third-party .NET process that already knows where and how to submit to a provider queue; it only needs to construct, validate, and serialize the JSON body that ingestion accepts.

This arc deliberately avoids developer API, provider authoring, runtime pipeline, journal, replay, rule, Studio, browser-host UI concerns, Azure Queue client, and file-share policy concerns. Those belong to later arcs or optional packages.

## Numbering

Arc 01 starts the roadmap at WP100.

Reserved buffer before Arc 02: WP105-WP119.

## Evidence Checked

- Current queue-message DTOs and JSON options live in [../../src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs](../../src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs), [../../src/UKHO.Search.Ingestion/Requests/IndexRequest.cs](../../src/UKHO.Search.Ingestion/Requests/IndexRequest.cs), [../../src/UKHO.Search.Ingestion/Requests/IngestionPropertyList.cs](../../src/UKHO.Search.Ingestion/Requests/IngestionPropertyList.cs), [../../src/UKHO.Search.Ingestion/Requests/IngestionFile.cs](../../src/UKHO.Search.Ingestion/Requests/IngestionFile.cs), and [../../src/UKHO.Search.Ingestion/Requests/Serialization/IngestionJsonSerializerOptions.cs](../../src/UKHO.Search.Ingestion/Requests/Serialization/IngestionJsonSerializerOptions.cs).
- Runtime ingestion provider contracts currently consume `IngestionRequest` through [../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs](../../src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs).
- File-share producer-style helpers are local/tooling code today, notably [../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs](../../tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs) and [../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs](../../tools/FileShareEmulator.Common/SecurityTokenPolicy.cs).
- Studio/provider API contracts are separate and must not leak into this package: [../../src/Studio/UKHO.Search.Studio/Providers/IStudioIngestionProvider.cs](../../src/Studio/UKHO.Search.Studio/Providers/IStudioIngestionProvider.cs), [../../src/UKHO.Search.ProviderModel/ProviderDescriptor.cs](../../src/UKHO.Search.ProviderModel/ProviderDescriptor.cs).
- Existing contract-focused tests include [../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs](../../test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs), [../../test/UKHO.Search.Ingestion.Tests/IngestionPropertyListTests.cs](../../test/UKHO.Search.Ingestion.Tests/IngestionPropertyListTests.cs), and [../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs](../../test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs).

## WP100: Define The Contracts Assembly Boundary

Scope:
- Create the formal specification and project boundary for `UKHO.Search.Ingestion.Contracts`.
- Decide target frameworks, nullable settings, package identity, XML documentation expectations, semantic versioning, and where the package sits in the onion architecture.
- State that the assembly is for remote queue producers only: not providers, not Studio clients, not browser UI clients, not Search service hosts, and not runtime pipeline extensions.

Requirements carried:
- Remote .NET producers must be able to reference a dependency-light package, construct an ingestion queue message, serialize it with package-owned JSON options, and submit the resulting JSON through their own queue client or deployment-specific path.
- The package must not reference Studio, Blazor, browser-host UI frameworks, RulesWorkbench, provider implementations, Elasticsearch, Azure SDKs, Aspire, App Configuration, SQL, queue clients, logging abstractions, ingestion pipeline runtime, `CanonicalDocument`, dead-letter, replay, or journal models.
- `ProviderDescriptor`, provider catalogs, `IStudioProvider`, `IIngestionDataProvider`, operation DTOs, rule DTOs, file-share SQL loaders, and file-share security-token policy are explicitly out of scope.

Expected outputs:
- A new contracts project, package metadata, and documentation page describing allowed and forbidden dependencies.
- A dependency audit proving the package is dependency-light and has no infrastructure/client references.

Validation anchors:
- Build the new project independently.
- Add a test that fails if forbidden project or package references are introduced.

## WP101: Extract Queue Message Types And JSON Contract

Scope:
- Move or share the queue-message wire types into the contracts assembly and update current ingestion runtime references.
- Preserve wire-compatible JSON names, request discriminator behavior, and validation semantics.

Requirements carried:
- Include `IngestionRequest`, `IngestionRequestType`, `IndexRequest`, `DeleteItemRequest`, `UpdateAclRequest`, `IngestionProperty`, `IngestionPropertyType`, `IngestionPropertyList`, `IngestionFile`, and `IngestionFileList`.
- Include the System.Text.Json options and converters required for the exact queue JSON, especially typed `IngestionProperty.Value` serialization and lower-case property type tokens.
- Preserve exactly-one operation payload validation on `IngestionRequest`.
- Preserve current `IndexRequest` validation for id, non-null property/file collections, non-empty security tokens, non-empty token values, and no first-class `Id` property duplication.
- Preserve case-insensitive property uniqueness and normalized property names.

Expected outputs:
- Runtime projects consume the extracted contracts instead of an internal-only copy.
- Golden JSON fixtures for `IndexItem`, `DeleteItem`, and `UpdateAcl` messages.

Validation anchors:
- Existing ingestion model JSON tests pass after being pointed at the contracts package.
- New golden-fixture tests assert serialization compatibility.

## WP102: Add Producer-Safe Helpers, Builders, And Validation

Scope:
- Add optional dependency-free conveniences that reduce invalid remote producer messages without expanding the package into an SDK.

Requirements carried:
- Static factories such as `CreateIndex`, `CreateDelete`, and `CreateAclUpdate` are in scope.
- Typed property factories such as `String`, `Text`, `DateTime`, and `StringArray` are in scope.
- An `IndexRequestBuilder` for id, timestamp, security tokens, files, and properties is in scope.
- A non-throwing validator returning structured contract errors is in scope.
- A serializer facade is in scope so producers do not have to remember converter registration.
- A visible contract version marker is required so queue-message compatibility can be reasoned about over time.

Constraints:
- Helpers must stay dependency-free and must not submit messages, discover providers, calculate file-share security tokens, generate `ShadowId`, or know journal/dead-letter storage.

Validation anchors:
- Unit tests cover helper success paths, validation failures, and serializer facade parity with raw `JsonSerializer` plus package options.

## WP103: Refactor In-Repo Consumers To The Extracted Contract

Scope:
- Update ingestion runtime, tests, FileShareEmulator, RulesWorkbench, and retained Studio provider code to reference the extracted contract package where they currently use `UKHO.Search.Ingestion.Requests`.

Requirements carried:
- The active ingestion runtime must continue to deserialize queue messages in [../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs).
- FileShareEmulator, RulesWorkbench, and retained Studio may still contain duplicated source-data reconstruction until Arc 06, but their queue-message DTO references must converge on the same contract.
- The package remains separate from ingestion input journal concepts. Producers must not generate `ShadowId` or learn table/blob journal implementation details.

Validation anchors:
- Run targeted tests for `UKHO.Search.Ingestion.Tests`, `UKHO.Search.Infrastructure.Ingestion.Tests`, `FileShareEmulator.Common.Tests`, `FileShareEmulator.Tests`, `RulesWorkbench.Tests`, and retained Studio provider tests if they remain in scope for the refactor.

## WP104: Publish Producer Guidance And Compatibility Rules

Scope:
- Document how a remote producer should create and serialize messages without acquiring Search runtime dependencies.

Requirements carried:
- Guidance must explain that queue submission helpers are a separate optional package, for example a future `UKHO.Search.Ingestion.AzureQueues`, and are not part of the core contract package.
- Guidance must explain that queue naming, authentication, provider queue selection, and deployment topology are external to the contracts package.
- Guidance must state that security tokens remain upstream in the first version. Any future move of token derivation into ingestion/provider normalization is a deliberate contract change, not an implicit helper addition.
- Guidance must explain that remote producers provide queue-message data only; ingestion owns any later journal identity, outcome, dead-letter, supersession, or replay metadata.

Validation anchors:
- A minimal external-consumer-style sample test can reference only the contracts project, create each operation type, serialize it, and validate the golden JSON.

## Arc Requirement Cross-Check

This arc covers these detailed requirements from the consolidation discussion and arc summary:

- Standalone queue-message contracts package for third-party .NET producers: WP100-WP104.
- Narrow type list: WP101.
- System.Text.Json options/converters and exact wire JSON: WP101-WP102.
- Dependency-light factories, builders, validators, serializer facade, examples, and version marker: WP102-WP104.
- Explicit exclusions for Studio, provider catalogs, provider SPI, runtime pipeline, `CanonicalDocument`, rules, journal, dead-letter, replay, queue clients, Azure SDKs, and file-share policy: WP100, WP102, WP104.
- Queue submission helpers as a later optional package: WP104.
- Producers do not generate `ShadowId` and do not know journal storage: WP103-WP104.
- First-version security-token derivation remains upstream: WP104.
- Current solution references the extracted contract instead of maintaining an internal-only copy: WP101-WP103.

## Handoff To Arc 02

Arc 02 must decide browser-host ownership, deliberate contract boundaries, and API authentication/authorization. It must not use the contracts package as a developer API SDK; this package remains queue-message only.