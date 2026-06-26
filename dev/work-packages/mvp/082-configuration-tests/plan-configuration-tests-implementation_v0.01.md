# Implementation Plan: Configuration Project Test Coverage Expansion

Target output path: `dev/work-packages/mvp/082-configuration-tests/plan-configuration-tests-implementation_v0.01.md`

Version: `v0.01`  
Status: `Draft`  
Date: `2026-03-30`  
Work Package: `dev/work-packages/mvp/082-configuration-tests/`  
Based on: `dev/work-packages/mvp/082-configuration-tests/spec-domain-configuration-tests_v0.01.md`  
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

- [x] Work Item 1: Replace placeholder coverage in `UKHO.Aspire.Configuration.Tests` - Completed
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
  - [x] Task 1: Add `AddsEnvironment` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/AddsEnvironmentTests.cs`.
    - [x] Step 2: Add tests for `TryParse`, `Parse`, equality, `IsLocal`, `IsDev`, `ToString`, and `GetHashCode`.
    - [x] Step 3: Add environment-variable-driven tests for `GetEnvironment`, including missing and invalid values.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md` to the test file.
  - [x] Task 2: Add `ConfigurationExtensions` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/ConfigurationExtensionsTests.cs`.
    - [x] Step 2: Add tests covering local endpoint lookup precedence, trimming, fallback to environment variables, and missing endpoint failure.
    - [x] Step 3: Add tests covering non-local registration expectations, singleton registration of `IExternalServiceRegistry`, lowercase label usage, and refresh sentinel behavior.
    - [x] Step 4: Use handwritten test doubles or lightweight host-builder setup only; do not refactor production code.
    - [x] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Add remote endpoint resolution coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalServiceRegistryTests.cs`.
    - [x] Step 2: Create `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalEndpointTests.cs` if needed, or merge it only if readability improves.
    - [x] Step 3: Add tests for missing definitions, missing tags, default tag selection, specific tag selection, Docker host substitution, unsupported substitution values, and default scope generation.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 4: Remove placeholder-only coverage and verify the slice - Completed
    - [x] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Tests` once real coverage is present.
    - [x] Step 2: Run the target test project only.
    - [x] Step 3: Fix any deterministic test failures caused by setup issues without changing production behavior.
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
  - **Completion Summary**: Added documented test coverage for `AddsEnvironment`, `ConfigurationExtensions`, `ExternalServiceRegistry`, and `ExternalEndpoint`; introduced lightweight host-builder test helpers in the test project; removed `PlaceholderSmokeTests.cs`; updated `test/UKHO.Aspire.Configuration.Tests/README.md`; verified with `dotnet test test\UKHO.Aspire.Configuration.Tests\UKHO.Aspire.Configuration.Tests.csproj --no-restore`.

- [x] Work Item 2: Replace placeholder coverage in `UKHO.Aspire.Configuration.Hosting.Tests` - Completed
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
  - [x] Task 1: Add `AddConfiguration` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs`.
    - [x] Step 2: Add tests for App Configuration resource creation, configuration-aware project reference wiring, and environment propagation.
    - [x] Step 3: Keep the assertions focused on accessible behavior rather than private implementation details.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2: Add `AddConfigurationEmulator` coverage - Completed
    - [x] Step 1: Add tests for content-root-relative path resolution and temporary file copy behavior.
    - [x] Step 2: Add tests for emulator resource creation, external HTTP endpoint setup, health check wiring, and local environment values.
    - [x] Step 3: Add tests for seeder creation, references, wait conditions, and propagated environment variables.
    - [x] Step 4: Add tests for configuration-aware project references and waits.
    - [x] Step 5: Use temporary files and deterministic cleanup where required by the test setup.
    - [x] Step 6: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Remove placeholder-only coverage and verify the slice - Completed
    - [x] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Hosting.Tests` once real coverage is present.
    - [x] Step 2: Run the target test project only.
    - [x] Step 3: Adjust only test setup and helper code if any assertions are unstable.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs`: New unit-focused Aspire builder extension tests.
    - `test/UKHO.Aspire.Configuration.Hosting.Tests/PlaceholderSmokeTests.cs`: Remove after replacement coverage exists.
  - **Work Item Dependencies**: Work Item 1 is recommended first because it establishes the basic style and helper patterns for the configuration test projects.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Hosting.Tests\UKHO.Aspire.Configuration.Hosting.Tests.csproj --no-restore`
  - **User Instructions**: None.
  - **Completion Summary**: Added documented Aspire application-model tests for `AddConfiguration` and `AddConfigurationEmulator`; covered App Configuration resource creation, emulator endpoint and health-check wiring, seeder file-copy behavior, mock references, wait dependencies, and environment propagation using deterministic temporary files and cleanup; removed `PlaceholderSmokeTests.cs`; verified with `dotnet test test\UKHO.Aspire.Configuration.Hosting.Tests\UKHO.Aspire.Configuration.Hosting.Tests.csproj --no-restore`.

## Seeder test slices

- [x] Work Item 3: Expand seeder utility and parser coverage in `UKHO.Aspire.Configuration.Seeder.Tests` - Completed
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
  - [x] Task 1: Extend existing additional-configuration helper tests - Completed
    - [x] Step 1: Extend `AdditionalConfigurationKeyBuilderTests.cs` with validation and ordering cases.
    - [x] Step 2: Extend `AdditionalConfigurationFileEnumeratorTests.cs` with `EnumerateFiles` and invalid-input cases.
    - [x] Step 3: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md` to updated test files.
    - **Summary**: Expanded helper coverage for validation guards, ordering, recursive file enumeration, and alternate directory separator handling; upgraded both existing test files to the required developer-comment standard.
  - [x] Task 2: Add `AdditionalConfigurationSeeder` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationSeederTests.cs`.
    - [x] Step 2: Add tests for missing root path, label propagation, key generation, plain-text value writes, cancellation, and multi-file write behavior.
    - [x] Step 3: Use handwritten fakes for logging and configuration client interactions where practical.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented seeder coverage for argument validation, missing-root logging, plain-text writes, cancellation, and write ordering using handwritten configuration-client and logger test doubles plus deterministic temporary directories.
  - [x] Task 3: Add JSON utility coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonStripperTests.cs`.
    - [x] Step 2: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonFlattenerTests.cs`.
    - [x] Step 3: Add tests for line comments, block comments, escaped strings, mixed content, flattening across objects and arrays, labels, nulls, booleans, numeric values, and key-vault reference content types.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented JSON utility suites covering comment stripping, escape preservation, mixed-content parsing, environment-scoped flattening, primitive conversion, array indexing, label propagation, and key-vault reference content types.
  - [x] Task 4: Add external service definition parser coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/ExternalServiceDefinitionParserTests.cs`.
    - [x] Step 2: Add tests for missing environment sections, missing client IDs, empty endpoints, missing default tags, invalid schemes, local placeholder resolution, multi-placeholder rejection, and missing environment variable failures.
    - [x] Step 3: Add happy-path tests for preserving service, client ID, scheme, tag, original template, placeholder, and resolved URL values.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented parser coverage for validation failures, non-local pass-through behavior, local placeholder resolution from environment variables, unsupported multi-placeholder templates, and endpoint metadata preservation.
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
  - **Completion Summary**: Expanded `UKHO.Aspire.Configuration.Seeder.Tests` with documented helper, seeder, JSON utility, and external service parser coverage; added reusable handwritten test doubles and deterministic temporary-directory/environment-variable helpers; added `test/UKHO.Aspire.Configuration.Seeder.Tests/README.md`; verified with `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`.

- [x] Work Item 4: Expand seeder orchestration and startup coverage in `UKHO.Aspire.Configuration.Seeder.Tests` - Completed
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
  - [x] Task 1: Add `ConfigurationService` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/ConfigurationServiceTests.cs`.
    - [x] Step 2: Add tests for sentinel ordering, label normalization, JSON preprocessing, flattened config writes, external service definition writes, and additional configuration conditional behavior.
    - [x] Step 3: Add transient retry tests for `TaskCanceledException`, retryable `RequestFailedException`, and `HttpRequestException`.
    - [x] Step 4: Add failure-path tests for max-attempt exhaustion.
    - [x] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented `ConfigurationService` orchestration tests covering reload sentinel ordering, lowercase labels, JSON comment preprocessing, flattened configuration writes, external service serialization, optional additional configuration seeding, transient retry recovery, and retry-budget exhaustion.
  - [x] Task 2: Add `LocalSeederService` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/LocalSeederServiceTests.cs`.
    - [x] Step 2: Add tests for local environment forwarding, host stop behavior on success, host stop behavior on failure, exception rethrowing, and `StopAsync` behavior.
    - [x] Step 3: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented hosted-service tests for local placeholder resolution, forwarded additional-configuration arguments, host shutdown on success and failure, error logging, exception rethrowing, and synchronous `StopAsync` completion using a handwritten host-lifetime fake.
  - [x] Task 3: Add best-effort `Program` coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramTests.cs`.
    - [x] Step 2: Add tests for command-line mode selection, invalid argument failure, invalid file/URI validation, endpoint resolution precedence, and non-local early return behavior where directly accessible.
    - [x] Step 3: Keep the coverage focused on directly accessible behavior and avoid forcing host-level integration if production seams do not allow it cleanly.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented best-effort entry-point coverage by reflecting the private `Main` and helper methods to verify command-line failure handling, file/URI validation, App Configuration endpoint precedence, and non-local Aspire no-op behavior without widening production seams.
  - **Files**:
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/ConfigurationServiceTests.cs`: New orchestration and retry tests.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/LocalSeederServiceTests.cs`: New hosted service lifecycle tests.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramTests.cs`: Best-effort entry-point and helper behavior tests.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/TestSupport/TestHostApplicationLifetime.cs`: New handwritten host-lifetime fake for hosted-service assertions.
  - **Work Item Dependencies**: Work Item 3 should be completed first because it establishes reusable seeder test fixtures and file/data patterns.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Seeder.Tests\UKHO.Aspire.Configuration.Seeder.Tests.csproj --no-restore`
  - **User Instructions**: None.
  - **Completion Summary**: Added documented `ConfigurationService`, `LocalSeederService`, and `Program` coverage to `UKHO.Aspire.Configuration.Seeder.Tests`; introduced a handwritten `TestHostApplicationLifetime` helper; updated `test/UKHO.Aspire.Configuration.Seeder.Tests/README.md`; verified the target seeder test project passes and the workspace build succeeds.

## Emulator test slices

- [x] Work Item 5: Add emulator models, authentication, and common utility coverage - Completed
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
  - [x] Task 1: Add configuration model and factory coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingFactoryTests.cs`.
    - [x] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/FeatureFlagConfigurationSettingTests.cs`.
    - [x] Step 3: Add tests for standard vs feature-flag creation, invalid content type fallback, round-tripping JSON payloads, and nested filter parameter preservation.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented factory and feature-flag tests covering standard-setting creation, feature-flag materialization, malformed content-type fallback, JSON round-tripping, and nested client-filter parameter preservation.
  - [x] Task 2: Add HMAC coverage - Completed
    - [x] Step 1: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacHandlerTests.cs`.
    - [x] Step 2: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacAuthenticatingHttpMessageHandlerTests.cs`.
    - [x] Step 3: Create `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacConfigureOptionsTests.cs` and `HmacExtensionsTests.cs` as separate files unless merging improves clarity.
    - [x] Step 4: Add tests for invalid headers, missing parameters, expired tokens, invalid credential, invalid signature, invalid content hash, valid request success, challenge output, outgoing auth header generation, and options binding behavior.
    - [x] Step 5: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented HMAC unit coverage for inbound validation and challenge flows, outbound request signing, named-option binding, and authentication-service registration using handwritten options/configuration test doubles.
  - [x] Task 3: Add common utility and client coverage - Completed
    - [x] Step 1: Create `Common/StringExtensionsTests.cs`, `Common/KeyValuePairJsonDecoderTests.cs`, `Common/KeyValuePairJsonEncoderTests.cs`, `Common/LinkHeaderValueTests.cs`, `Common/SelectJsonTypeInfoModifierTests.cs`, and `Common/ConfigurationClientTests.cs` under the emulator test project.
    - [x] Step 2: Add tests for JSON flatten/reconstruct behavior, prefix stripping, representative round-trips, link parsing/formatting, property filtering, and client-side pagination/request shaping.
    - [x] Step 3: Use handwritten HTTP handlers and fakes rather than adding new mocking packages.
    - [x] Step 4: Apply the full developer-comment standard from `./.github/instructions/documentation-pass.instructions.md`.
    - **Summary**: Added documented utility and client coverage for string unescaping, JSON flattening/encoding, link parsing, JSON projection filtering, and paged configuration-client request shaping using handwritten HTTP transport fakes.
  - [x] Task 4: Remove placeholder-only coverage and verify the slice - Completed
    - [x] Step 1: Remove `PlaceholderSmokeTests.cs` from `test/UKHO.Aspire.Configuration.Emulator.Tests` once real coverage is present.
    - [x] Step 2: Run the target test project only.
    - [x] Step 3: Stabilize any flaky assertions by improving test doubles, not by changing production code.
    - **Summary**: Removed the placeholder smoke test, updated `test/UKHO.Aspire.Configuration.Emulator.Tests/README.md`, and verified the target emulator test project passes after stabilizing HMAC and pagination assertions through deterministic test-support helpers.
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
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/TestSupport/TestOptionsMonitor.cs`: New options-monitor fake for authentication tests.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/TestSupport/TestAuthenticationConfigurationProvider.cs`: New authentication-configuration fake for option-binding tests.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/TestSupport/TestHttpMessageHandler.cs`: New HTTP transport fake for outbound-client and signing tests.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/TestSupport/RecordedHttpRequest.cs`: Captured outbound-request model for HTTP assertions.
    - `test/UKHO.Aspire.Configuration.Emulator.Tests/PlaceholderSmokeTests.cs`: Removed after replacement coverage was added.
  - **Work Item Dependencies**: Work Items 1 through 4 are recommended first because they establish the preferred style, helper patterns, and deterministic test conventions.
  - **Run / Verification Instructions**:
    - `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`
  - **User Instructions**: None.
  - **Completion Summary**: Added documented emulator model, HMAC, utility, and configuration-client coverage to `test/UKHO.Aspire.Configuration.Emulator.Tests`; introduced handwritten test-support helpers for options, authentication configuration, and HTTP transport assertions; removed `PlaceholderSmokeTests.cs`; updated `test/UKHO.Aspire.Configuration.Emulator.Tests/README.md`; verified with `dotnet test test\UKHO.Aspire.Configuration.Emulator.Tests\UKHO.Aspire.Configuration.Emulator.Tests.csproj --no-restore`.

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
