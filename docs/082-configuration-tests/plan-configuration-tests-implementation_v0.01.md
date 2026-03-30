# Implementation Plan: Configuration Project Test Coverage Expansion

Target output path: `docs/082-configuration-tests/plan-configuration-tests-implementation_v0.01.md`

Version: `v0.01`  
Status: `Draft`  
Date: `2026-03-30`  
Work Package: `docs/082-configuration-tests/`  
Based on: `docs/082-configuration-tests/spec-domain-configuration-tests_v0.01.md`  
Mandatory standards:
- `./.github/instructions/documentation-pass.instructions.md`
- `./.github/instructions/testing.instructions.md`
- `./.github/instructions/coding-standards.instructions.md`
- `./.github/instructions/documentation.instructions.md`

## 1. Delivery approach

This plan expands test coverage only in the existing target test projects:

- `test/UKHO.Aspire.Configuration.Tests`
- `test/UKHO.Aspire.Configuration.Hosting.Tests`
- `test/UKHO.Aspire.Configuration.Seeder.Tests`
- `test/UKHO.Aspire.Configuration.Emulator.Tests`

The delivery approach is breadth-first and project-focused:

1. replace placeholder coverage in the smallest projects first
2. expand the seeder suite across utilities and orchestration behavior
3. add best-effort emulator unit coverage across the highest-value custom seams
4. keep every work item independently runnable through targeted `dotnet test` execution

## 2. Global implementation constraints

- All code-writing work in this plan MUST follow `./.github/instructions/documentation-pass.instructions.md` in full.
- Compliance with `./.github/instructions/documentation-pass.instructions.md` is a hard Definition of Done gate for every work item.
- For every new or updated test file, implementation MUST add developer-level comments for the test class, every helper type, every method, every constructor, and each test method scenario, setup intent, action, and assertion significance.
- If any public helper members are introduced in test code, their parameters MUST be documented in line with `./.github/instructions/documentation-pass.instructions.md`.
- Production refactoring is out of scope.
- No new test packages should be introduced unless the plan is explicitly revised later.
- Placeholder smoke tests should be removed once meaningful authored tests exist in the corresponding test project.
- `UKHO.Aspire.Configuration.Emulator.Tests` must remain unit-test-only for this work item.
- Do not run the full solution test suite for this work package.

## 3. Work Items

## Core configuration test slices

- [ ] Work Item 1: Replace placeholder coverage in `UKHO.Aspire.Configuration.Tests`
  - **Purpose**: Create meaningful, runnable unit coverage for the core configuration library so environment parsing, App Configuration registration behavior, and external endpoint resolution are all protected by tests.
  - **Acceptance Criteria**:
    - `test/UKHO.Aspire.Configuration.Tests` no longer relies on placeholder-only coverage.
    - `AddsEnvironment` behavior is covered for valid, invalid, equality, and environment-variable-driven flows.
    - `ConfigurationExtensions` behavior is covered for local and non-local registration paths without changing production code.
    - `ExternalServiceRegistry` and `ExternalEndpoint` behavior are covered for resolution, missing configuration, and host substitution cases.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for `test/UKHO.Aspire.Configuration.Tests`
    - Placeholder smoke tests removed or superseded
    - Logging/error-path assertions added where relevant to the behavior under test
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Tests\UKHO.Aspire.Configuration.Tests.csproj --no-restore`
  - [ ] Task 1: Add `AddsEnvironment` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/AddsEnvironmentTests.cs`.
    - [ ] Step 2: Add tests for `TryParse`, `Parse`, equality, `IsLocal`, `IsDev`, `ToString`, and `GetHashCode`.
    - [ ] Step 3: Add environment-variable-driven tests for `GetEnvironment`, including missing and invalid values.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md` to the test file.
  - [ ] Task 2: Add `ConfigurationExtensions` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/ConfigurationExtensionsTests.cs`.
    - [ ] Step 2: Add tests covering local endpoint lookup precedence, trimming, fallback to environment variables, and missing endpoint failure.
    - [ ] Step 3: Add tests covering non-local registration expectations, singleton registration of `IExternalServiceRegistry`, lowercase label usage, and refresh sentinel behavior.
    - [ ] Step 4: Use handwritten test doubles or lightweight host-builder setup only; do not refactor production code.
    - [ ] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add remote endpoint resolution coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalServiceRegistryTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalEndpointTests.cs` if needed, or merge it only if readability improves.
    - [ ] Step 3: Add tests for missing definitions, missing tags, default tag selection, specific tag selection, Docker host substitution, unsupported substitution values, and default scope generation.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 4: Remove placeholder-only coverage and verify the slice
    - [ ] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Tests` once real coverage is present.
    - [ ] Step 2: Run the target test project only.
    - [ ] Step 3: Fix any deterministic test failures caused by setup issues without changing production behavior.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Tests/AddsEnvironmentTests.cs`: New unit tests for environment parsing and equality behavior.
    - `test/UKHO.Aspire.Configuration.Tests/ConfigurationExtensionsTests.cs`: New unit tests for App Configuration registration behavior.
    - `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalServiceRegistryTests.cs`: New unit tests for service definition resolution and host substitution.
    - `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalEndpointTests.cs`: Optional focused tests for endpoint scope behavior.
    - `test/UKHO.Aspire.Configuration.Tests/PlaceholderSmokeTests.cs`: Remove after replacement coverage exists.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Tests\UKHO.Aspire.Configuration.Tests.csproj --no-restore`
    - Optionally verify targeted scenarios with `--filter` during iteration, but finish by running the whole project.
  - **User Instructions**: None.

- [ ] Work Item 2: Replace placeholder coverage in `UKHO.Aspire.Configuration.Hosting.Tests`
  - **Purpose**: Add executable coverage for the Aspire host extension layer so local emulator/seeder wiring and App Configuration resource wiring can be validated safely without changing production behavior.
  - **Acceptance Criteria**:
    - `test/UKHO.Aspire.Configuration.Hosting.Tests` no longer relies on placeholder-only coverage.
    - `DistributedApplicationBuilderExtensions.AddConfiguration` is covered for resource creation, project reference wiring, and environment propagation.
    - `DistributedApplicationBuilderExtensions.AddConfigurationEmulator` is covered for file copying, emulator and seeder wiring, mock references, wait dependencies, and environment propagation.
    - Any coverage for `CopyToTempFile` remains within the existing production contract and does not require production refactoring.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for `test/UKHO.Aspire.Configuration.Hosting.Tests`
    - Placeholder smoke tests removed or superseded
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Hosting.Tests\UKHO.Aspire.Configuration.Hosting.Tests.csproj --no-restore`
  - [ ] Task 1: Add `AddConfiguration` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs`.
    - [ ] Step 2: Add tests for App Configuration resource creation, configuration-aware project reference wiring, and environment propagation.
    - [ ] Step 3: Keep the assertions focused on accessible behavior rather than private implementation details.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Add `AddConfigurationEmulator` coverage
    - [ ] Step 1: Add tests for content-root-relative path resolution and temporary file copy behavior.
    - [ ] Step 2: Add tests for emulator resource creation, external HTTP endpoint setup, health check wiring, and local environment values.
    - [ ] Step 3: Add tests for seeder creation, references, wait conditions, and propagated environment variables.
    - [ ] Step 4: Add tests for configuration-aware project references and waits.
    - [ ] Step 5: Use temporary files and deterministic cleanup where required by the test setup.
    - [ ] Step 6: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Remove placeholder-only coverage and verify the slice
    - [ ] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Hosting.Tests` once real coverage is present.
    - [ ] Step 2: Run the target test project only.
    - [ ] Step 3: Adjust only test setup and helper code if any assertions are unstable.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs`: New unit-focused Aspire builder extension tests.
    - `test/UKHO.Aspire.Configuration.Hosting.Tests/PlaceholderSmokeTests.cs`: Remove after replacement coverage exists.
  - **Work Item Dependencies**: Work Item 1 is recommended first because it establishes the basic style and helper patterns for the configuration test projects.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Hosting.Tests\UKHO.Aspire.Configuration.Hosting.Tests.csproj --no-restore`
  - **User Instructions**: None.

## Seeder test slices

- [ ] Work Item 3: Expand seeder utility and parser coverage in `UKHO.Aspire.Configuration.Seeder.Tests`
  - **Purpose**: Add a broad first-pass safety net around the seeder project's utility and parsing behaviors so file enumeration, key generation, comment stripping, flattening, and external service definition parsing are all demonstrably protected.
  - **Acceptance Criteria**:
    - Existing tests for `AdditionalConfigurationKeyBuilder` and `AdditionalConfigurationFileEnumerator` are retained and expanded.
    - New tests cover `AdditionalConfigurationSeeder`, `JsonStripper`, `JsonFlattener`, and `ExternalServiceDefinitionParser`.
    - Tests remain deterministic and avoid live Azure dependencies.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for the utility/parser subset and then the whole `test/UKHO.Aspire.Configuration.Seeder.Tests` project
    - File-system-backed tests clean up deterministically
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`
  - [ ] Task 1: Extend existing additional-configuration helper tests
    - [ ] Step 1: Extend `AdditionalConfigurationKeyBuilderTests.cs` with validation and ordering cases.
    - [ ] Step 2: Extend `AdditionalConfigurationFileEnumeratorTests.cs` with `EnumerateFiles` and invalid-input cases.
    - [ ] Step 3: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md` to updated test files.
  - [ ] Task 2: Add `AdditionalConfigurationSeeder` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationSeederTests.cs`.
    - [ ] Step 2: Add tests for missing root path, label propagation, key generation, plain-text value writes, cancellation, and multi-file write behavior.
    - [ ] Step 3: Use handwritten fakes for logging and configuration client interactions where practical.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add JSON utility coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonStripperTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonFlattenerTests.cs`.
    - [ ] Step 3: Add tests for line comments, block comments, escaped strings, mixed content, flattening across objects and arrays, labels, nulls, booleans, numeric values, and key-vault reference content types.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 4: Add external service definition parser coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/ExternalServiceDefinitionParserTests.cs`.
    - [ ] Step 2: Add tests for missing environment sections, missing client IDs, empty endpoints, missing default tags, invalid schemes, local placeholder resolution, multi-placeholder rejection, and missing environment variable failures.
    - [ ] Step 3: Add happy-path tests for preserving service, client ID, scheme, tag, original template, placeholder, and resolved URL values.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationKeyBuilderTests.cs`: Extend existing coverage.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationFileEnumeratorTests.cs`: Extend existing coverage.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationSeederTests.cs`: New tests for additional file ingestion behavior.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonStripperTests.cs`: New tests for comment stripping.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonFlattenerTests.cs`: New tests for flattening and content types.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/ExternalServiceDefinitionParserTests.cs`: New tests for parser behavior.
  - **Work Item Dependencies**: Work Item 1 is recommended first for shared test style; otherwise independent.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`
    - During iteration, optional focused runs may target the newly added test classes only.
  - **User Instructions**: None.

- [ ] Work Item 4: Expand seeder orchestration and startup coverage in `UKHO.Aspire.Configuration.Seeder.Tests`
  - **Purpose**: Protect the seeder's orchestration behavior so configuration write ordering, retry logic, hosted-service lifecycle, and top-level seeding entry-point decisions can be verified through a runnable test slice.
  - **Acceptance Criteria**:
    - `ConfigurationService` is covered for sentinel creation, label normalization, flattening flow, external service serialization, optional additional configuration seeding, and retry behavior.
    - `LocalSeederService` is covered for lifecycle behavior, argument forwarding, stop-on-success, and stop-on-failure behavior.
    - `Program` receives best-effort tests for command-line mode, basic validation helpers, and non-local no-op behavior, limited to directly accessible behavior.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for the whole `test/UKHO.Aspire.Configuration.Seeder.Tests` project
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`
  - [ ] Task 1: Add `ConfigurationService` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/ConfigurationServiceTests.cs`.
    - [ ] Step 2: Add tests for sentinel ordering, label normalization, JSON preprocessing, flattened config writes, external service definition writes, and additional configuration conditional behavior.
    - [ ] Step 3: Add transient retry tests for `TaskCanceledException`, retryable `RequestFailedException`, and `HttpRequestException`.
    - [ ] Step 4: Add failure-path tests for max-attempt exhaustion.
    - [ ] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Add `LocalSeederService` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/LocalSeederServiceTests.cs`.
    - [ ] Step 2: Add tests for local environment forwarding, host stop behavior on success, host stop behavior on failure, exception rethrowing, and `StopAsync` behavior.
    - [ ] Step 3: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add best-effort `Program` coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramTests.cs`.
    - [ ] Step 2: Add tests for command-line mode selection, invalid argument failure, invalid file/URI validation, endpoint resolution precedence, and non-local early return behavior where directly accessible.
    - [ ] Step 3: Keep the coverage focused on directly accessible behavior and avoid forcing host-level integration if production seams do not allow it cleanly.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/ConfigurationServiceTests.cs`: New orchestration and retry tests.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/LocalSeederServiceTests.cs`: New hosted service lifecycle tests.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramTests.cs`: Best-effort entry-point and helper behavior tests.
  - **Work Item Dependencies**: Work Item 3 should be completed first because it establishes reusable seeder test fixtures and file/data patterns.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`
  - **User Instructions**: None.

## Emulator test slices

- [ ] Work Item 5: Add emulator models, authentication, and common utility coverage
  - **Purpose**: Create a broad, unit-only first-pass safety net across the emulator's custom models, auth helpers, JSON utilities, and HTTP client wrapper behavior without introducing host-level or SQLite-backed integration tests.
  - **Acceptance Criteria**:
    - `ConfigurationSettingFactory`, `FeatureFlagConfigurationSetting`, and `FeatureFlagFilter` behaviors are covered.
    - HMAC helper coverage exists for request validation, challenge behavior, outgoing auth header generation, option binding, and extension registration.
    - Common utility coverage exists for `StringExtensions`, JSON decoder/encoder behavior, `LinkHeaderValue`, `SelectJsonTypeInfoModifier`, and `ConfigurationClient` request/pagination behavior.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests` no longer relies on placeholder-only coverage.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for the emulator auth/utility/model slice and then the whole emulator project
    - Placeholder smoke tests removed or superseded
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
  - [ ] Task 1: Add configuration model and factory coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingFactoryTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/FeatureFlagConfigurationSettingTests.cs`.
    - [ ] Step 3: Add tests for standard vs feature-flag creation, invalid content type fallback, round-tripping JSON payloads, and nested filter parameter preservation.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Add HMAC coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacHandlerTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacAuthenticatingHttpMessageHandlerTests.cs`.
    - [ ] Step 3: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacConfigureOptionsTests.cs` and `HmacExtensionsTests.cs` as separate files unless merging improves clarity.
    - [ ] Step 4: Add tests for invalid headers, missing parameters, expired tokens, invalid credential, invalid signature, invalid content hash, valid request success, challenge output, outgoing auth header generation, and options binding behavior.
    - [ ] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Add common utility and client coverage
    - [ ] Step 1: Create `Common/StringExtensionsTests.cs`, `Common/KeyValuePairJsonDecoderTests.cs`, `Common/KeyValuePairJsonEncoderTests.cs`, `Common/LinkHeaderValueTests.cs`, `Common/SelectJsonTypeInfoModifierTests.cs`, and `Common/ConfigurationClientTests.cs` under the emulator test project.
    - [ ] Step 2: Add tests for JSON flatten/reconstruct behavior, prefix stripping, representative round-trips, link parsing/formatting, property filtering, and client-side pagination/request shaping.
    - [ ] Step 3: Use handwritten HTTP handlers and fakes rather than adding new mocking packages.
    - [ ] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 4: Remove placeholder-only coverage and verify the slice
    - [ ] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Emulator.Tests` once real coverage is present.
    - [ ] Step 2: Run the target test project only.
    - [ ] Step 3: Stabilize any flaky assertions by improving test doubles, not by changing production code.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingFactoryTests.cs`: New factory coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/FeatureFlagConfigurationSettingTests.cs`: New feature-flag model coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacHandlerTests.cs`: New incoming auth validation coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacAuthenticatingHttpMessageHandlerTests.cs`: New outgoing auth header coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacConfigureOptionsTests.cs`: New options binding coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacExtensionsTests.cs`: New registration coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/StringExtensionsTests.cs`: New escape-handling coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/KeyValuePairJsonDecoderTests.cs`: New decoder coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/KeyValuePairJsonEncoderTests.cs`: New encoder coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/LinkHeaderValueTests.cs`: New link parsing/formatting coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/SelectJsonTypeInfoModifierTests.cs`: New property-filtering coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/ConfigurationClientTests.cs`: New pagination/request shaping coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/PlaceholderSmokeTests.cs`: Remove after replacement coverage exists.
  - **Work Item Dependencies**: Work Items 1 through 4 are recommended first because they establish the preferred style, helper patterns, and deterministic test conventions.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
  - **User Instructions**: None.

- [ ] Work Item 6: Add emulator repository, decorator, and minimal API handler coverage
  - **Purpose**: Complete the best-effort emulator pass by covering repository-facing logic, Event Grid decorator behavior, and minimal API handlers through unit-focused seams and handwritten fakes.
  - **Acceptance Criteria**:
    - `ConfigurationSettingRepository` receives unit-focused coverage for add, get, update, remove, filtering, materialization, and tag handling behaviors that can be exercised without SQLite-backed integration.
    - `EventGridMessagingConfigurationSettingRepository` is covered for add, update, remove, get, and event payload creation behavior.
    - `ConfigurationSettingHandler`, `KeyHandler`, `LabelHandler`, and `LockHandler` are covered for preconditions, not-found flows, validation, lock behavior, and projection behavior.
    - Startup/database behavior remains explicitly de-prioritized unless isolated helper behavior can be exercised as a true unit test.
    - All new or updated test code complies with `./.github/instructions/documentation-pass.instructions.md`.
  - **Definition of Done**:
    - Code implemented in the existing test project only
    - Tests passing for the whole emulator test project
    - Documentation-pass requirements applied to all created or modified test files
    - Can execute end-to-end via: `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
  - [ ] Task 1: Add repository and decorator coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingRepositoryTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/EventGridMessagingConfigurationSettingRepositoryTests.cs`.
    - [ ] Step 3: Add tests for add/get/update/remove flows, filtering behavior, tags, materialization, and decorator event publication behavior using handwritten repository, command, and event fakes.
    - [ ] Step 4: Keep the tests unit-only and do not add SQLite-backed integration harnesses.
    - [ ] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 2: Add minimal API handler coverage
    - [ ] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingHandlerTests.cs`.
    - [ ] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Keys/KeyHandlerTests.cs`, `Labels/LabelHandlerTests.cs`, and `Locks/LockHandlerTests.cs`.
    - [ ] Step 3: Add tests for not-found behavior, add/update/delete behavior, lock behavior, precondition handling, invalid wildcard/comma combinations, too-many-values validation, and distinct projection behavior.
    - [ ] Step 4: Use repository fakes and `HttpContext` setup only as needed to exercise the accessible handler contracts.
    - [ ] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [ ] Task 3: Verify the completed emulator slice
    - [ ] Step 1: Run the target emulator project tests only.
    - [ ] Step 2: Confirm the suite remains unit-only and does not depend on live services, full host startup, or SQLite-backed integration setup.
    - [ ] Step 3: Remove or simplify any test helper that duplicates framework behavior unnecessarily.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingRepositoryTests.cs`: New repository-focused unit coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/EventGridMessagingConfigurationSettingRepositoryTests.cs`: New decorator coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingHandlerTests.cs`: New minimal API handler coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Keys/KeyHandlerTests.cs`: New key-listing coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Labels/LabelHandlerTests.cs`: New label-listing coverage.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/Locks/LockHandlerTests.cs`: New lock/unlock coverage.
  - **Work Item Dependencies**: Work Item 5 should be completed first because it establishes emulator-specific helpers, request builders, and model fixtures.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
  - **User Instructions**: None.

## 4. Cross-cutting completion checks

- [ ] Task: Validate that each modified test project runs independently
  - [ ] Step 1: Run each of the four target test projects individually.
  - [ ] Step 2: Do not run the full solution test suite.
  - [ ] Step 3: Record any pre-existing unrelated failures separately if discovered.

- [ ] Task: Validate documentation-pass compliance for all touched test files
  - [ ] Step 1: Check every new or updated test class for explicit type-level documentation comments.
  - [ ] Step 2: Check every test method and helper method for explanatory comments covering scenario intent and logical flow.
  - [ ] Step 3: Check constructors and any public members in helper code for required documentation.
  - [ ] Step 4: Treat failures against `./.github/instructions/documentation-pass.instructions.md` as incomplete work.

## 5. Summary

This plan delivers the work package as six small, project-runnable test slices. The early work items replace placeholder coverage in the smallest projects first, the middle work items broaden the seeder suite across utilities and orchestration, and the final work items add best-effort emulator unit coverage across the highest-value custom seams.

Key considerations for implementation:

- keep all changes inside the existing test projects
- preserve production behavior and avoid refactoring for testability
- prefer handwritten fakes and deterministic setup over new test dependencies
- remove placeholder tests once substantive coverage exists
- treat `./.github/instructions/documentation-pass.instructions.md` as a mandatory completion gate for every code-writing task
- verify each target project independently rather than running the full suite
