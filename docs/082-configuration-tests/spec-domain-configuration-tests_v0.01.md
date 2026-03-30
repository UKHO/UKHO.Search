# Specification: Configuration Project Test Coverage Expansion

Target output path: `docs/082-configuration-tests/spec-domain-configuration-tests_v0.01.md`

Version: `v0.01`  
Status: `Draft`  
Date: `2026-03-30`  
Work Package: `docs/082-configuration-tests/`  
Based on: `spec-template_v1.1.md`  
Source Inputs:
- `./.github/prompts/spec.research.prompt.md`
- `./.github/copilot-instructions.md`
- `./.github/instructions/documentation.instructions.md`
- `configuration/UKHO.Aspire.Configuration/`
- `configuration/UKHO.Aspire.Configuration.Emulator/`
- `configuration/UKHO.Aspire.Configuration.Hosting/`
- `configuration/UKHO.Aspire.Configuration.Seeder/`
- `test/UKHO.Aspire.Configuration.Tests/`
- `test/UKHO.Aspire.Configuration.Emulator.Tests/`
- `test/UKHO.Aspire.Configuration.Hosting.Tests/`
- `test/UKHO.Aspire.Configuration.Seeder.Tests/`

## 1. Objective

### 1.1 Purpose
Define a comprehensive xUnit test specification for the four configuration-related projects and their corresponding test projects:

1. `configuration/UKHO.Aspire.Configuration` -> `test/UKHO.Aspire.Configuration.Tests`
2. `configuration/UKHO.Aspire.Configuration.Emulator` -> `test/UKHO.Aspire.Configuration.Emulator.Tests`
3. `configuration/UKHO.Aspire.Configuration.Hosting` -> `test/UKHO.Aspire.Configuration.Hosting.Tests`
4. `configuration/UKHO.Aspire.Configuration.Seeder` -> `test/UKHO.Aspire.Configuration.Seeder.Tests`

This specification is intended to replace placeholder or minimal test coverage with a deliberate set of unit-focused and targeted integration-focused tests that validate behavior, edge cases, failure paths, and key wiring assumptions.

### 1.2 Current baseline
Current discovered state in the workspace:

- `UKHO.Aspire.Configuration.Tests` contains only `PlaceholderSmokeTests.cs`.
- `UKHO.Aspire.Configuration.Emulator.Tests` contains only `PlaceholderSmokeTests.cs`.
- `UKHO.Aspire.Configuration.Hosting.Tests` contains only `PlaceholderSmokeTests.cs`.
- `UKHO.Aspire.Configuration.Seeder.Tests` contains only:
  - `AdditionalConfigurationKeyBuilderTests.cs`
  - `AdditionalConfigurationFileEnumeratorTests.cs`

This means the configuration family currently has little or no trustworthy regression coverage for the majority of its implementation.

### 1.3 Scope intent
The implementation pass derived from this specification should:

- add xUnit tests in the existing four test projects only
- prefer deterministic tests over environment-dependent tests
- use mocks/fakes for Azure and external dependencies where practical
- keep changes focused on test coverage and avoid production refactoring
- treat `UKHO.Aspire.Configuration.Emulator` as best-effort because it is a large ASP.NET application and much of the behavior appears to come from third-party or framework-style code
- keep `UKHO.Aspire.Configuration.Emulator.Tests` strictly unit-test-only

### 1.4 Assumptions
- Placeholder smoke tests should be removed once meaningful coverage exists.
- Tests should work against the production code as it currently exists, without production refactoring for testability.
- Tests should stay aligned with current package choices already present in the test projects (`xUnit`, `Shouldly` where available, and existing coverage packages).
- Small test-only helpers and fakes may be added inside each test project where they improve clarity and stability.
- The first implementation pass should prioritize breadth across the identified files and types before pursuing exhaustive branch-depth on a smaller subset.
- Prefer no new test packages; use handwritten fakes, helpers, and existing test dependencies where practical.
- The seeder and hosting projects are good candidates for mostly unit tests with a few lightweight integration tests.
- The emulator test strategy should prioritize high-value seams with low setup cost rather than attempting exhaustive end-to-end coverage.

### 1.5 Risks and constraints
- Several code paths depend on environment variables, current time, file system state, or Azure SDK types, so over-specified tests may become brittle.
- Some hosting and Aspire APIs may be awkward to assert directly without introducing substantial test harness complexity.
- The emulator contains a broad HTTP, auth, JSON, persistence, and eventing surface area, so the spec must prioritize core behavior rather than aiming for full parity coverage.

## 2. In-scope system and component view

### 2.1 `UKHO.Aspire.Configuration`
This project provides:

- environment parsing and validation via `AddsEnvironment`
- App Configuration wiring via `ConfigurationExtensions`
- external service endpoint resolution via `ExternalServiceRegistry`
- endpoint models and well-known configuration names

### 2.2 `UKHO.Aspire.Configuration.Hosting`
This project provides Aspire host extensions for:

- creating an Azure App Configuration resource
- wiring environment values into configuration-aware projects
- starting the local emulator and seeder process for local development
- copying configuration seed files to temporary locations before process startup

### 2.3 `UKHO.Aspire.Configuration.Seeder`
This project provides:

- command-line and Aspire-hosted seeding entry points
- configuration flattening and comment stripping
- external service definition parsing and endpoint resolution
- retry-aware writes to Azure App Configuration
- optional additional configuration file ingestion

### 2.4 `UKHO.Aspire.Configuration.Emulator`
This project provides a local App Configuration emulator including:

- HMAC authentication helpers and handler logic
- minimal API handlers for key-values, labels, keys, and locks
- SQLite-backed configuration repositories
- Event Grid decorator behavior
- JSON conversion helpers and HTTP client wrapper behavior
- app startup and database initialization

## 3. High-level test strategy

### 3.1 Test philosophy
The tests should be organised by production type and behavior, not as broad smoke coverage. Each test suite should verify:

- nominal behavior
- validation and error handling
- branch-heavy conditional behavior
- serialization or transformation correctness
- environment-driven behavior
- repository or handler side effects where those are part of the contract

For this work item, breadth is prioritized first: the implementation should cover more of the identified custom types and files in the initial pass, while still including representative edge cases for the highest-risk behaviors.

Tests should be specified against directly accessible public and internal behavior. Private or static helper behavior should only be covered indirectly through those accessible contracts, not as a standalone test objective.

### 3.2 Preferred test levels
- `UKHO.Aspire.Configuration`: unit tests only, except for very small DI registration assertions if needed.
- `UKHO.Aspire.Configuration.Hosting`: mostly unit tests, with lightweight integration-style assertions if Aspire abstractions require real builders.
- `UKHO.Aspire.Configuration.Seeder`: mainly unit tests, plus small file-system-backed tests and selective retry or startup tests.
- `UKHO.Aspire.Configuration.Emulator`: unit tests only, focused on factories, handlers, utilities, auth helpers, repository-facing logic, and other custom seams that can be exercised without broader host or HTTP integration harnesses.

### 3.3 Test naming and layout
Proposed test files should be placed in the corresponding test project and grouped by production type, for example:

- `test/UKHO.Aspire.Configuration.Tests/AddsEnvironmentTests.cs`
- `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs`
- `test/UKHO.Aspire.Configuration.Seeder.Tests/JsonFlattenerTests.cs`
- `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingHandlerTests.cs`

### 3.4 Proposed test file inventory

| Source project | Source file / type | Proposed target test file | Notes |
| --- | --- | --- | --- |
| `UKHO.Aspire.Configuration` | `AddsEnvironment.cs` | `test/UKHO.Aspire.Configuration.Tests/AddsEnvironmentTests.cs` | Parse, equality, environment variable behavior |
| `UKHO.Aspire.Configuration` | `ConfigurationExtensions.cs` | `test/UKHO.Aspire.Configuration.Tests/ConfigurationExtensionsTests.cs` | Local vs non-local registration behavior |
| `UKHO.Aspire.Configuration` | `Remote/ExternalServiceRegistry.cs` | `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalServiceRegistryTests.cs` | Resolution, missing definitions, host substitution |
| `UKHO.Aspire.Configuration` | `Remote/ExternalEndpoint.cs` | `test/UKHO.Aspire.Configuration.Tests/Remote/ExternalEndpointTests.cs` | Scope generation |
| `UKHO.Aspire.Configuration.Hosting` | `DistributedApplicationBuilderExtensions.cs` | `test/UKHO.Aspire.Configuration.Hosting.Tests/DistributedApplicationBuilderExtensionsTests.cs` | Main builder extension coverage |
| `UKHO.Aspire.Configuration.Seeder` | `AdditionalConfiguration/AdditionalConfigurationKeyBuilder.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationKeyBuilderTests.cs` | Extend existing tests |
| `UKHO.Aspire.Configuration.Seeder` | `AdditionalConfiguration/AdditionalConfigurationFileEnumerator.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationFileEnumeratorTests.cs` | Extend existing tests |
| `UKHO.Aspire.Configuration.Seeder` | `AdditionalConfiguration/AdditionalConfigurationSeeder.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/AdditionalConfigurationSeederTests.cs` | File enumeration, writes, cancellation |
| `UKHO.Aspire.Configuration.Seeder` | `Json/JsonStripper.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonStripperTests.cs` | Comment stripping correctness |
| `UKHO.Aspire.Configuration.Seeder` | `Json/JsonFlattener.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/JsonFlattenerTests.cs` | Flattening and content type behavior |
| `UKHO.Aspire.Configuration.Seeder` | `Json/ExternalServiceDefinitionParser.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/Json/ExternalServiceDefinitionParserTests.cs` | Parsing, placeholders, environment resolution |
| `UKHO.Aspire.Configuration.Seeder` | `Services/ConfigurationService.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/ConfigurationServiceTests.cs` | Sentinel, flattening, retries, additional config |
| `UKHO.Aspire.Configuration.Seeder` | `Services/LocalSeederService.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/Services/LocalSeederServiceTests.cs` | Hosted-service behavior |
| `UKHO.Aspire.Configuration.Seeder` | `Program.cs` | `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramTests.cs` | Best-effort command-line and helper behavior |
| `UKHO.Aspire.Configuration.Emulator` | `ConfigurationSettings/ConfigurationSettingFactory.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingFactoryTests.cs` | Standard vs feature flag materialization |
| `UKHO.Aspire.Configuration.Emulator` | `ConfigurationSettings/FeatureFlagConfigurationSetting.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/FeatureFlagConfigurationSettingTests.cs` | Round-trip JSON and filter behavior |
| `UKHO.Aspire.Configuration.Emulator` | `ConfigurationSettings/ConfigurationSettingRepository.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingRepositoryTests.cs` | Repository-facing logic via unit seams and fakes |
| `UKHO.Aspire.Configuration.Emulator` | `ConfigurationSettings/EventGridMessagingConfigurationSettingRepository.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/EventGridMessagingConfigurationSettingRepositoryTests.cs` | Decorator and publish behavior |
| `UKHO.Aspire.Configuration.Emulator` | `ConfigurationSettings/ConfigurationSettingHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/ConfigurationSettings/ConfigurationSettingHandlerTests.cs` | Minimal API handler behavior |
| `UKHO.Aspire.Configuration.Emulator` | `Keys/KeyHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Keys/KeyHandlerTests.cs` | Validation and distinct projection |
| `UKHO.Aspire.Configuration.Emulator` | `Labels/LabelHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Labels/LabelHandlerTests.cs` | Validation and distinct projection |
| `UKHO.Aspire.Configuration.Emulator` | `Locks/LockHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Locks/LockHandlerTests.cs` | Lock/unlock and preconditions |
| `UKHO.Aspire.Configuration.Emulator` | `Authentication/Hmac/HmacHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacHandlerTests.cs` | Request validation and challenge behavior |
| `UKHO.Aspire.Configuration.Emulator` | `Authentication/Hmac/HmacAuthenticatingHttpMessageHandler.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacAuthenticatingHttpMessageHandlerTests.cs` | Outgoing auth header generation |
| `UKHO.Aspire.Configuration.Emulator` | `Authentication/Hmac/HmacConfigureOptions.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacConfigureOptionsTests.cs` | Options binding behavior |
| `UKHO.Aspire.Configuration.Emulator` | `Authentication/Hmac/HmacExtensions.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Authentication/Hmac/HmacExtensionsTests.cs` | Registration assertions |
| `UKHO.Aspire.Configuration.Emulator` | `Common/StringExtensions.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/StringExtensionsTests.cs` | Escape handling |
| `UKHO.Aspire.Configuration.Emulator` | `Common/KeyValuePairJsonDecoder.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/KeyValuePairJsonDecoderTests.cs` | Flattened pair generation |
| `UKHO.Aspire.Configuration.Emulator` | `Common/KeyValuePairJsonEncoder.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/KeyValuePairJsonEncoderTests.cs` | Reconstruction behavior |
| `UKHO.Aspire.Configuration.Emulator` | `Common/LinkHeaderValue.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/LinkHeaderValueTests.cs` | Parsing and formatting |
| `UKHO.Aspire.Configuration.Emulator` | `Common/SelectJsonTypeInfoModifier.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/SelectJsonTypeInfoModifierTests.cs` | Select filtering |
| `UKHO.Aspire.Configuration.Emulator` | `Common/ConfigurationClient.cs` | `test/UKHO.Aspire.Configuration.Emulator.Tests/Common/ConfigurationClientTests.cs` | Pagination and request shaping |

This inventory is intended as guidance for the concrete first-pass file plan, not a rigid contract. It is acceptable to merge a small number of closely related source types into one test file where that clearly improves readability, or to adjust file boundaries slightly during implementation, but the default should be one obvious test file per production type or behavior area.

### 3.5 De-prioritized and out-of-scope tests
The following are explicitly de-prioritized or out of scope for this work item:

- full end-to-end or browser-style tests
- emulator host-level integration coverage using full application startup harnesses
- tests that require live Azure services, live Event Grid, or live App Configuration instances
- tests that require production refactoring solely to make code easier to test
- exhaustive framework-behavior verification where the code under test is mostly third-party or ASP.NET/Aspire plumbing
- private helper methods as standalone test targets

These may be considered in later work only if clear value remains after the specified unit-focused coverage has been delivered.

## 4. Project-specific specification

### 4.1 `UKHO.Aspire.Configuration.Tests`

#### 4.1.1 `AddsEnvironment`
Required tests should cover:

- `TryParse` returning the known static instance for each supported value
- case-insensitive parsing
- invalid input returning `false` and `null`
- `Parse` returning the expected instance for valid inputs
- `Parse` throwing for invalid inputs
- `GetEnvironment` returning the parsed environment when the environment variable is present and valid
- `GetEnvironment` throwing when the environment variable is missing
- `GetEnvironment` throwing when the environment variable is invalid
- equality and inequality behavior across casing variants
- `IsLocal`, `IsDev`, `ToString`, and `GetHashCode` consistency

#### 4.1.2 `ConfigurationExtensions`
Required tests should cover:

- local environment path selects the local endpoint resolution logic
- local endpoint resolution prefers the HTTPS configuration key before HTTP
- local endpoint resolution falls back from configuration values to environment variables
- resolved local endpoints are trimmed of trailing slashes
- missing local endpoints throw `InvalidOperationException`
- non-local environment path invokes host builder App Configuration wiring path
- `ExternalServiceRegistry` is registered as a singleton service
- refresh registration uses `WellKnownConfigurationName.ReloadSentinelKey`
- refresh interval value is passed through as configured
- service labels are normalized to lowercase
- component names are normalized to lowercase on non-local registration

#### 4.1.3 `ExternalServiceRegistry` and remote models
Required tests should cover:

- missing service definition key throws `KeyNotFoundException`
- empty service definition value throws `KeyNotFoundException`
- JSON definitions deserialize and the default-tag endpoint is selected when `tag` is empty
- specific endpoint tags resolve correctly
- missing requested tag throws `KeyNotFoundException`
- `EndpointHostSubstitution.None` leaves the URL unchanged
- `EndpointHostSubstitution.Docker` rewrites the host to `host.docker.internal`
- unsupported enum values throw `ArgumentOutOfRangeException`
- `ExternalEndpoint.GetDefaultScope()` returns `{ClientId}/.default`

### 4.2 `UKHO.Aspire.Configuration.Hosting.Tests`

#### 4.2.1 `DistributedApplicationBuilderExtensions.AddConfiguration`
Required tests should cover:

- creating an Azure App Configuration resource with the supplied resource name
- applying a reference from each configuration-aware project to the created App Configuration resource
- applying the `adds-environment` environment variable to each configuration-aware project
- returning the created App Configuration resource builder
- handling an empty project enumeration without failure

#### 4.2.2 `DistributedApplicationBuilderExtensions.AddConfigurationEmulator`
Required tests should cover:

- resolving both JSON file paths relative to the builder content root
- copying both input files to distinct temporary files
- creating the emulator project with external HTTP endpoints and health check
- setting the local adds-environment value on the emulator
- creating the seeder project and wiring it to reference and wait for the emulator
- propagating service name, configuration path, external service path, additional configuration path, and additional configuration prefix into seeder environment variables
- adding each external mock as a reference to the seeder service
- making each configuration-aware project reference and wait for the emulator
- making each configuration-aware project wait for the seeder service
- applying the local adds-environment value to each configuration-aware project
- returning the emulator resource builder

#### 4.2.3 `CopyToTempFile`
Required tests should cover:

- copied file contents match the source file exactly
- returned file path is in the system temporary path
- separate calls create distinct temp files
- missing source file fails fast with the underlying file exception

### 4.3 `UKHO.Aspire.Configuration.Seeder.Tests`

#### 4.3.1 Existing coverage to retain and extend
Existing tests for:

- `AdditionalConfigurationKeyBuilder`
- `AdditionalConfigurationFileEnumerator.GetRelativePathSegments`

should be retained and extended rather than replaced.

#### 4.3.2 `AdditionalConfigurationKeyBuilder`
Additional tests should cover:

- null or whitespace prefix throws
- null or whitespace filename throws
- multiple empty path segments are ignored
- path segments preserve order

#### 4.3.3 `AdditionalConfigurationFileEnumerator`
Additional tests should cover:

- null or whitespace root path throws in `EnumerateFiles`
- `EnumerateFiles` returns all nested files
- null or whitespace inputs throw in `GetRelativePathSegments`
- alternative directory separators are handled correctly

#### 4.3.4 `AdditionalConfigurationSeeder`
Required tests should cover:

- null or invalid arguments throw
- missing root path logs a warning and performs no writes
- existing root path enumerates all files under the tree
- generated keys combine prefix, relative path segments, and file name without extension
- file contents are written as plain text values
- label is applied to all written settings
- cancellation is honored before processing each file
- multiple files result in multiple writes in deterministic key order when enumeration order is controlled by the test

#### 4.3.5 `JsonStripper`
Required tests should cover:

- removing `//` line comments
- removing `/* ... */` block comments
- preserving content that looks like comments inside string literals
- preserving escape sequences inside strings
- mixed commented and uncommented content
- null input throws

#### 4.3.6 `JsonFlattener`
Required tests should cover:

- missing environment section throws
- nested objects flatten to colon-delimited keys
- arrays flatten to indexed keys
- strings, numbers, booleans, and null values are converted as expected
- generated settings carry the requested label
- key vault reference values receive the key-vault reference content type
- normal string values receive plain text content type
- unsupported token kinds fail clearly if encountered

#### 4.3.7 `ExternalServiceDefinitionParser`
Required tests should cover:

- missing environment section throws
- missing `clientId` throws
- missing or empty endpoint list throws
- missing default tag endpoint throws
- invalid or missing URL scheme throws
- non-local environments leave endpoint URLs unresolved and unchanged
- local environment resolves a single placeholder from environment variables
- local environment rejects more than one placeholder in a template
- local environment throws when the referenced service endpoint environment variable is missing
- parsed results preserve service name, client ID, original template, resolved URL, scheme, tag, and placeholder metadata

#### 4.3.8 `ConfigurationService`
Required tests should cover:

- writing the reload sentinel before flattened settings and external services
- lowercasing the label from service name
- reading and stripping comments from both input JSON files
- flattening configuration settings for the requested environment
- serializing external service definitions under the `externalservice:<service>` key
- invoking additional configuration seeding only when both path and prefix are supplied
- skipping additional configuration seeding when either path or prefix is blank
- retrying transient failures from `TaskCanceledException` caused by the per-attempt timeout
- retrying retryable `RequestFailedException` status codes
- retrying `HttpRequestException`
- succeeding after transient failures resolve
- rethrowing when the maximum retry count is exhausted
- preserving `ContentType` for sentinel and generated settings

#### 4.3.9 `LocalSeederService`
Required tests should cover:

- calling `ConfigurationService.SeedConfigurationAsync` with the local environment
- forwarding all constructor-supplied configuration values
- stopping the host when seeding succeeds
- stopping the host when seeding fails
- rethrowing seeding exceptions after logging
- `StopAsync` returning a completed task

#### 4.3.10 `Program`
Best-value tests should cover:

- command-line mode is selected when the sentinel environment variable is absent
- invalid command-line parse returns `-1`
- invalid file paths fail validation
- invalid URI fails validation
- valid command-line inputs invoke `ConfigurationService.SeedConfigurationAsync`
- local Aspire mode builds a host and registers the expected singleton and hosted services
- non-local Aspire mode exits with success and does nothing
- `ResolveAppConfigurationEndpoint` prefers HTTPS then HTTP and trims trailing slash
- `ResolveAppConfigurationEndpoint` throws when neither environment variable exists

### 4.4 `UKHO.Aspire.Configuration.Emulator.Tests`

### 4.4.1 Test scope note
This project is large and includes framework-style ASP.NET application code. Test design should therefore focus on the most valuable custom logic and avoid deep duplication of third-party behavior.

For this work item, emulator coverage is explicitly constrained to unit tests only. Avoid adding end-to-end, in-process host, WebApplicationFactory, or SQLite-backed integration coverage in this pass.

#### 4.4.2 Configuration setting models and factories
Required or high-value tests should cover:

- `ConfigurationSettingFactory.Create` returns a standard `ConfigurationSetting` for ordinary content types
- feature flag content type returns `FeatureFlagConfigurationSetting`
- invalid content type parsing falls back to regular `ConfigurationSetting`
- generated settings have non-empty ETags
- `FeatureFlagConfigurationSetting` can round-trip supported JSON payloads through `Value`
- `FeatureFlagFilter` preserves nested parameter values across parse and write operations

#### 4.4.3 Repository behavior
Required tests should cover:

- `ConfigurationSettingRepository.Add` inserts expected values
- `Get` returns inserted records
- key wildcard filtering works
- comma-separated key filtering works with escaped commas
- label wildcard filtering works
- null label filtering works
- memento/history queries return historical values when supported by the SQLite schema
- `Update` updates the current record
- `Remove` deletes the expected record
- tags are serialized and deserialized correctly
- feature flag settings and standard settings are materialized correctly from repository reads

#### 4.4.4 Event Grid decorator behavior
Required tests should cover:

- `Add` delegates to inner repository then publishes a modified event
- `Update` delegates to inner repository then publishes a modified event
- `Remove` delegates to inner repository then publishes a deleted event
- event payload includes key, label, and ETag
- `Get` delegates without publishing

#### 4.4.5 Minimal API handlers
Required tests should cover:

- `ConfigurationSettingHandler.Get` returns not found when no setting exists
- `Get` enforces `If-Match`
- `Get` returns not modified for matching `If-None-Match`
- `Set` adds when the setting does not exist
- `Set` updates when the setting exists
- `Set` rejects precondition failures correctly
- `Set` rejects writes to locked settings
- `Delete` returns no content when the target does not exist and preconditions allow it
- `Delete` returns precondition failed for invalid headers when the target does not exist
- `Delete` deletes existing unlocked settings
- `Delete` rejects locked settings
- `List` validates invalid wildcard/comma combinations
- `List` validates too many values
- `KeyHandler.List` and `LabelHandler.List` apply the same validation behavior and distinct projection behavior
- `LockHandler.Lock` and `Unlock` update lock state and ETag
- `LockHandler` returns not found and precondition failures correctly

#### 4.4.6 HMAC authentication
High-value tests should cover:

- non-HMAC authorization header returns `NoResult`
- missing required HMAC parameters fails
- missing required signed headers fails
- invalid request date fails
- expired date fails
- invalid credential fails
- invalid signature fails
- invalid content hash fails
- valid signed request succeeds
- challenge response returns `401` and a `WWW-Authenticate` header
- `HmacAuthenticatingHttpMessageHandler` adds authorization, date, and content hash headers
- handler computes content hash for empty and non-empty request content
- `HmacConfigureOptions` leaves defaults unchanged when no configuration exists
- `HmacConfigureOptions` applies configured credential and secret values
- `HmacExtensions.AddHmac` registers the scheme and configure options consistently

#### 4.4.7 Common utilities and client behavior
High-value tests should cover:

- `StringExtensions.Unescape` removes escape backslashes correctly
- `KeyValuePairJsonDecoder` flattens objects and arrays to key-value pairs
- `KeyValuePairJsonEncoder` reconstructs objects and arrays from flattened pairs
- encoder prefix stripping works for exact and partial prefix cases
- decoder and encoder round-trip representative payloads
- `SelectJsonTypeInfoModifier` filters JSON properties correctly
- `LinkHeaderValue.Parse` handles empty, single, and multiple link headers
- `LinkHeaderValue.ToString` reproduces stored links in header format
- `ConfigurationClient.GetConfigurationSettings` follows pagination links and materializes items
- `ConfigurationClient.GetKeys` follows pagination links
- `ConfigurationClient.GetLabels` follows pagination links
- `ConfigurationClient.SetConfigurationSetting` emits the expected route, label encoding, and JSON payload

#### 4.4.8 Application/database setup
Best-effort unit tests should cover:

- extraction of testable database-initialization behavior only if it can be exercised without introducing integration harnesses
- startup-related helper logic only where it can be isolated as unit-testable custom behavior
- endpoint mapping and full host wiring are out of scope for this work item

## 5. Proposed implementation order

No strict project-first priority has been mandated for implementation sequencing. The order below is a recommended execution order only.

1. Replace placeholder tests in `UKHO.Aspire.Configuration.Tests` with real unit coverage.
2. Replace placeholder tests in `UKHO.Aspire.Configuration.Hosting.Tests` with unit-focused builder extension coverage.
3. Expand `UKHO.Aspire.Configuration.Seeder.Tests` to cover parser, JSON, retry, and startup logic.
4. Replace placeholder tests in `UKHO.Aspire.Configuration.Emulator.Tests` with focused unit coverage for handlers, auth, utilities, repository behavior, and other isolated custom logic.

## 6. Acceptance criteria

- Each of the four target test projects contains meaningful authored tests instead of placeholder-only coverage.
- The new tests exercise the major custom logic branches identified in this specification.
- The seeder and core configuration projects have broad unit coverage across their public and internal behavior.
- The hosting project has meaningful coverage of environment propagation and emulator/seeder wiring behavior.
- The emulator project has best-effort unit coverage concentrated on custom logic rather than framework plumbing.
- The resulting test design remains maintainable and does not rely on unstable external services.

## 7. Clarified decisions

- `UKHO.Aspire.Configuration.Emulator.Tests` SHALL remain strictly unit-test-only for this work item.
- Placeholder smoke tests SHOULD be removed once real tests exist in the corresponding test project.
- Production refactoring is out of scope; tests SHOULD be implemented against the code as-is.
- Small test-only helpers and fakes MAY be added within each test project where needed.
- The initial implementation pass SHOULD prioritize breadth of coverage across the configuration projects before deeper branch expansion on a smaller subset of types.
- No explicit numeric coverage target is required; success is measured against the specified test inventory and meaningful coverage improvement across the four target projects.
- Prefer no new test packages; use existing dependencies plus handwritten fakes and helpers where possible.
- The specification SHOULD target directly accessible public and internal behavior rather than private helper methods as standalone test targets.
- No strict project-first implementation order is required; any sequencing in this document is guidance only.
- The spec SHOULD explicitly identify de-prioritized and out-of-scope test categories.
- The proposed test file inventory is guidance only and does not need to be followed rigidly file-for-file.
