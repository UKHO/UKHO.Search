# Specification: Ingestion rule storage path changes

- **Work package**: `094-rule-storage-changes`
- **Document**: `docs/094-rule-storage-changes/spec-platform-rule-storage-changes_v0.01.md`
- **Version**: `v0.01`
- **Date**: `2026-04-15`

## 1. Overview

### 1.1 Purpose

This work package moves the checked-in file-share ingestion rule assets from `./rules/file-share/**` to `./rules/ingestion/file-share/**`, preserves correct loading into Azure App Configuration through the Aspire configuration seeder, and aligns all App Configuration-backed ingestion rule consumers with the new key space `rules:ingestion:file-share:*`.

The repository already uses the `UKHO.Aspire.Configuration.Seeder` additional-configuration path to load the `./rules` directory into App Configuration. That seeder currently enumerates files recursively and derives configuration keys from the relative subfolder structure. As a result, the requested physical move is not only a repository file move; it changes the logical configuration contract seen by ingestion services and RulesWorkbench.

### 1.2 Scope

This specification covers:

- moving the repository rule files from `./rules/file-share/**` to `./rules/ingestion/file-share/**`;
- verifying and, if required, amending the local Aspire configuration seeding path so nested directories continue to map to nested App Configuration keys;
- updating all App Configuration-backed ingestion rule readers, writers, discovery surfaces, and tests that currently assume `rules:file-share:*`;
- updating repository documentation that still instructs contributors to use the old rule path.

This specification does not cover:

- changing the rule JSON schema;
- changing the logical provider identifier from `file-share` to anything else;
- introducing a new rule engine or a new non-App-Configuration storage mechanism;
- restructuring non-ingestion rule namespaces beyond the requested `ingestion/file-share` nesting.

### 1.3 Stakeholders

- ingestion platform maintainers;
- file-share ingestion provider maintainers;
- RulesWorkbench maintainers;
- AppHost and Aspire configuration maintainers;
- developers authoring or testing ingestion rules.

### 1.4 Definitions

- **Rule asset path**: the checked-in repository file path for authored rule JSON.
- **Configuration key**: the Azure App Configuration key generated from a rule file path.
- **Additional configuration seeding**: the `UKHO.Aspire.Configuration.Seeder` flow that reads files from a directory tree and writes them to App Configuration with a configured prefix.
- **Logical provider**: the ingestion provider identity used by the ingestion rules engine and related discovery APIs. For this work package the logical provider remains `file-share`.
- **Namespace segment**: a configuration key segment between `rules` and the logical provider. For this work package that segment becomes `ingestion`.

## 2. System context

### 2.1 Current state

The current repository structure stores file-share ingestion rule files directly under `./rules/file-share/**`.

In local AppHost run mode, `src/Hosts/AppHost/AppHost.cs` points the configuration emulator seeder at the repository `./rules` root with an additional configuration prefix of `rules`. The seeder implementation in `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration/*` already:

- enumerates files recursively using `SearchOption.AllDirectories`;
- derives relative directory segments from the file path; and
- appends those segments to the configured prefix when building the final key.

That means the current physical rule path `rules/file-share/<rule-id>.json` becomes the App Configuration key shape `rules:file-share:<rule-id>`.

The App Configuration-backed ingestion rule runtime currently assumes that shape in multiple places. Repository evidence includes:

- `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRulesSource.cs` enumerates `configuration.GetSection("rules")` and treats each first-level child as a provider section.
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRuleConfigurationWriter.cs` writes keys using `rules:{provider}:{ruleId}`.
- `tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs` enumerates the `rules` root and likewise assumes the first child below `rules` is the provider.
- multiple tests in `test/StudioServiceHost.Tests/*` and `test/RulesWorkbench.Tests/*` seed in-memory configuration using keys such as `rules:file-share:rule-1`.
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/RuleKeyParser.cs` only accepts exactly three key segments.
- several wiki pages still instruct users to edit `rules/file-share/...` and refer to keys such as `rules:file-share:*`.

### 2.2 Proposed state

After this change:

- the repository rule assets live under `./rules/ingestion/file-share/**`;
- the local Aspire seeding path continues to point at the repository `./rules` root with prefix `rules`;
- the resulting App Configuration key shape for a file-share rule becomes `rules:ingestion:file-share:<rule-id>`;
- all App Configuration-backed ingestion rule readers and writers treat `ingestion` as a namespace segment and `file-share` as the provider;
- all external behavior that identifies the provider continues to expose `file-share`, not `ingestion`.

### 2.3 Assumptions

- file-share remains the provider name used by the ingestion engine, provider catalog, and discovery endpoints.
- only ingestion rules are being moved under the new `rules/ingestion/*` namespace in this work package.
- the AppHost local configuration seeder should remain generic and path-driven rather than acquiring special-case rule logic.
- existing rule JSON content remains valid and is not being semantically rewritten as part of this work package.

### 2.4 Constraints

- the implementation must preserve Onion Architecture boundaries.
- the change must continue to work in the local Aspire/App Configuration emulator setup.
- the change must not rely on dual-write or hidden compatibility behavior unless explicitly documented.
- the work must keep the rule-provider identity canonicalized to lowercase `file-share`.

## 3. Component / service design (high level)

### 3.1 Components

The work package impacts the following high-level components:

1. **Repository rule assets**
   - `./rules/file-share/**` -> `./rules/ingestion/file-share/**`

2. **AppHost local configuration seeding**
   - `src/Hosts/AppHost/AppHost.cs`
   - `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration/*`

3. **App Configuration-backed ingestion rule runtime**
   - `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRulesSource.cs`
   - `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRuleConfigurationWriter.cs`
   - any helper or parser retained for rule-key interpretation

4. **RulesWorkbench App Configuration rule snapshotting and editing**
   - `tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs`
   - associated RulesWorkbench tests

5. **Host- and API-level tests/documentation**
   - `test/StudioServiceHost.Tests/*`
   - `test/RulesWorkbench.Tests/*`
   - `test/UKHO.Aspire.Configuration.Seeder.Tests/*`
   - repository wiki pages and documentation referencing the old rule path

### 3.2 Data flows

Target data flow after the change:

1. rule JSON files are authored under `./rules/ingestion/file-share/**`;
2. AppHost points the configuration seeder at the `./rules` root with prefix `rules`;
3. the seeder converts relative folders into configuration key segments, producing keys shaped like `rules:ingestion:file-share:<rule-id>`;
4. App Configuration-backed ingestion rule loaders enumerate beneath `rules:ingestion`, derive the logical provider as `file-share`, and load rule JSON values;
5. RulesWorkbench edit/save operations persist updated JSON back to the same `rules:ingestion:file-share:<rule-id>` key space.

### 3.3 Key decisions

- **Decision 1**: Keep the seeder generic. The existing recursive file enumeration and key-builder behavior is the correct mechanism for handling the new nested path.
- **Decision 2**: Treat `ingestion` as a namespace segment, not as the logical provider name.
- **Decision 3**: Centralize or consistently share the ingestion rule App Configuration key shape so readers, writers, and tests do not drift again.
- **Decision 4**: Update consumers to the new path rather than continuing to read from the legacy `rules:file-share:*` path.

## 4. Functional requirements

- **FR1**: All rule files currently under `./rules/file-share/**` MUST be moved to `./rules/ingestion/file-share/**`, preserving filenames and any nested structure if present.
- **FR2**: The local Aspire configuration seeding flow MUST continue to load the moved files from the repository `./rules` root and MUST generate keys shaped as `rules:ingestion:file-share:<rule-id>`.
- **FR3**: All App Configuration-backed ingestion rule readers MUST load rules from the new `rules:ingestion:file-share:*` path.
- **FR4**: All App Configuration-backed ingestion rule writers or save-back flows MUST persist rules to `rules:ingestion:file-share:<rule-id>`.
- **FR5**: Runtime and UI-facing consumers MUST continue to expose the logical provider as `file-share` rather than `ingestion`.
- **FR6**: Tests that currently seed in-memory configuration with `rules:file-share:*` MUST be updated to the new path.
- **FR7**: Repository documentation that instructs contributors to edit `rules/file-share/...` or refers to `rules:file-share:*` MUST be updated to the new path.
- **FR8**: Legacy keys under `rules:file-share:*` MUST not be required for correct runtime behavior after the change.

## 5. Non-functional requirements

- **NFR1**: The change MUST preserve current rule JSON payloads and rule-engine semantics.
- **NFR2**: The change MUST minimize future path drift by avoiding duplicated string literals for the ingestion rule configuration root wherever practical.
- **NFR3**: The change MUST preserve case-insensitive handling of provider names and configuration keys where the current implementation already does so.
- **NFR4**: The implementation MUST keep local developer setup understandable by maintaining a direct, path-based mapping from the repository `./rules` tree to App Configuration keys.
- **NFR5**: Logging and diagnostics MUST make the new effective key space clear enough that future issues can distinguish `rules:file-share:*` from `rules:ingestion:file-share:*`.

## 6. Data model

### 6.1 Path-to-key mapping

| Physical rule file | Configuration prefix | Resulting App Configuration key | Logical provider |
| --- | --- | --- | --- |
| `rules/ingestion/file-share/bu-adds-1.json` | `rules` | `rules:ingestion:file-share:bu-adds-1` | `file-share` |
| `rules/ingestion/file-share/subset/example.json` | `rules` | `rules:ingestion:file-share:subset:example` | `file-share` |

### 6.2 Provider identity model

The additional `ingestion` segment is a namespace boundary, not a provider identifier. Consumers that currently infer the provider from the first child below `rules` must be updated so that:

- `rules` is the top-level configuration root;
- `ingestion` is the ingestion-rules namespace;
- `file-share` is the provider;
- remaining segments describe the rule identity beneath that provider.

### 6.3 Key-shape implication

Any production or test code that currently validates only three-part keys (`rules:{provider}:{ruleId}`) is no longer aligned with the target contract and must be updated or retired.

## 7. Interfaces & integration

### 7.1 AppHost and Aspire configuration seeding

`src/Hosts/AppHost/AppHost.cs` currently points the additional-configuration seeder at the repository `./rules` directory with prefix `rules`.

The specification requires that this flow continue to work without special-case file-share logic. The expected result after moving the files is:

- `additionalConfigurationPath` remains the repository `./rules` root;
- `additionalConfigurationPrefix` remains `rules`;
- the generated keys gain the extra `ingestion` path segment automatically because the files now sit under `rules/ingestion/file-share/**`.

The existing seeder implementation already provides the needed recursive path-segment behavior, but test coverage must explicitly protect the target rule path shape.

### 7.2 Ingestion rule loading from App Configuration

`AppConfigRulesSource` must no longer treat the immediate children of `rules` as providers. It must instead enumerate the ingestion namespace beneath `rules:ingestion` and then treat the next segment as the provider.

The resulting runtime behavior must:

- load all providers under `rules:ingestion`;
- continue to return `file-share` as the provider name for the current rule set;
- produce canonical rule-entry keys that reflect the new full path.

### 7.3 Ingestion rule save-back to App Configuration

`AppConfigRuleConfigurationWriter` must compose keys using the new namespace-aware contract:

- previous: `rules:{provider}:{ruleId}`
- target: `rules:ingestion:{provider}:{ruleId}`

Any save-back feature in RulesWorkbench or related tools must use the same contract.

### 7.4 RulesWorkbench App Configuration snapshots

`tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs` must be updated so it does not misinterpret `ingestion` as the provider and `file-share` as the rule id. The snapshot store must enumerate from the ingestion namespace and return entries whose:

- provider is `file-share`;
- key is `rules:ingestion:file-share:<rule-id>`;
- rule id remains the actual rule id.

### 7.5 Documentation and contributor workflow integration

Contributor-facing documentation and wiki content must be updated so the documented authoring path and the effective configuration key path match the implemented behavior.

## 8. Observability (logging/metrics/tracing)

- Logging in App Configuration-backed rule readers and writers should include the effective rule key or rule namespace being processed.
- Where logs currently mention only provider and rule count, they should remain readable after the introduction of the `ingestion` namespace.
- If practical, warnings or diagnostics should make it obvious when a caller is still using or expecting the legacy `rules:file-share:*` path.

No new metrics or distributed tracing requirements are introduced by this work package.

## 9. Security & compliance

- No new secrets, credentials, or identities are introduced.
- Rule JSON remains plain-text configuration data in App Configuration as today.
- The change must not broaden access to unrelated configuration namespaces.
- Any cleanup of legacy keys must be performed using existing operational permissions and processes.

## 10. Testing strategy

The implementation must add or update automated tests covering the following areas.

### 10.1 Seeder and path-mapping tests

- verify that nested rule files under `rules/ingestion/file-share/**` are mapped to keys under `rules:ingestion:file-share:*`;
- retain or extend the existing recursive path-segment tests in `UKHO.Aspire.Configuration.Seeder.Tests`;
- verify AppHost still passes the repository `./rules` root and the `rules` prefix into the seeder configuration.

### 10.2 App Configuration runtime rule loading tests

- verify `AppConfigRulesSource` loads file-share rules from `rules:ingestion:file-share:*`;
- verify provider identity remains `file-share`;
- verify legacy `rules:file-share:*` keys are not the required source for successful loading.

### 10.3 App Configuration rule writer tests

- verify `AppConfigRuleConfigurationWriter` writes keys using `rules:ingestion:{provider}:{ruleId}`;
- verify save-back from UI or service surfaces continues to target the same key space.

### 10.4 RulesWorkbench and host tests

- update RulesWorkbench tests that currently use `rules:file-share:*`;
- update StudioServiceHost tests that currently seed `rules:file-share:*` in-memory configuration;
- update or remove any rule-key parser tests that only validate the three-segment legacy format.

### 10.5 Documentation verification

- verify contributor-facing documentation no longer points to `rules/file-share/...`;
- verify examples and prose reference `rules/ingestion/file-share/...` and `rules:ingestion:file-share:*` consistently.

## 11. Rollout / migration

### 11.1 Repository migration

1. move the checked-in rule files from `./rules/file-share/**` to `./rules/ingestion/file-share/**`;
2. update App Configuration-backed readers and writers to the new namespace-aware key contract;
3. update tests and documentation;
4. reseed local or shared App Configuration instances from the updated repository state.

### 11.2 Configuration-store migration note

Moving the files and reseeding creates new keys under `rules:ingestion:file-share:*`, but it does not automatically remove old keys under `rules:file-share:*` from any existing App Configuration store.

This work package therefore requires a deliberate migration note for operators and developers:

- stale legacy keys may remain present after reseeding;
- updated consumers must no longer depend on those keys;
- environments may require explicit cleanup of obsolete `rules:file-share:*` keys to avoid confusion.

### 11.3 Acceptance criteria

This work package is complete when all of the following are true:

1. the repository rule files exist only under `./rules/ingestion/file-share/**`;
2. local Aspire/AppHost seeding produces keys under `rules:ingestion:file-share:*`;
3. App Configuration-backed ingestion rule loading succeeds using the new path;
4. App Configuration-backed rule save-back writes to the new path;
5. provider identity exposed to runtime and UI consumers remains `file-share`;
6. automated tests no longer depend on the old `rules:file-share:*` path;
7. repository documentation has been updated to the new authoring path and key shape.

## 12. Open questions

1. Should the implementation include an explicit helper/constant set for the ingestion rule configuration root (for example `rules:ingestion`) so readers, writers, and tests all compose keys through one shared path contract rather than repeating string literals?
2. Should legacy `rules:file-share:*` keys be actively deleted as part of rollout tooling, or is documented manual cleanup sufficient for this work package?
3. If future providers gain ingestion rules, should they also live under `./rules/ingestion/<provider>/**` from the outset so the namespace becomes the permanent repository convention rather than a one-off file-share migration?
