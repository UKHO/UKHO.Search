# Implementation Plan: Ingestion rule storage path changes

- **Work package**: `094-rule-storage-changes`
- **Document**: `docs/094-rule-storage-changes/plan-platform-rule-storage-changes_v0.01.md`
- **Version**: `v0.01`
- **Date**: `2026-04-15`
- **Based on**: `docs/094-rule-storage-changes/spec-platform-rule-storage-changes_v0.01.md`
- **Mandatory instruction files**:
  - `./.github/instructions/documentation-pass.instructions.md`
  - `./.github/instructions/wiki.instructions.md`

## Overall delivery approach

This work package should be delivered in a small set of vertical slices that keep the repository runnable while progressively shifting the ingestion-rule source-of-truth from `./rules/file-share/**` and `rules:file-share:*` to `./rules/ingestion/file-share/**` and `rules:ingestion:file-share:*`.

The first slice should establish the new repository asset location and prove that the local Aspire configuration seeding path already produces the required nested App Configuration keys from the repository `./rules` root. The second slice should make the runtime rule readers and writers namespace-aware so the application can load, enumerate, and persist ingestion rules through the new key path while still exposing `file-share` as the logical provider. The third slice should complete the host-, tool-, and contributor-facing alignment by updating tests, documentation, and developer guidance that still encode the old path.

Because this work changes source code, **every code-writing task in this plan must follow `./.github/instructions/documentation-pass.instructions.md` in full as a mandatory Definition of Done gate**. That means execution must add or improve developer-level documentation comments for every changed class, constructor, method, and relevant property, including internal and other non-public types, and must add enough inline comments that the control flow and rationale remain understandable to future contributors. The plan treats that standard as mandatory completion criteria rather than optional polish.

Because this work changes developer-facing repository guidance, terminology, runtime configuration paths, and contributor workflows for authored ingestion rules, **the wiki review defined by `./.github/instructions/wiki.instructions.md` is also mandatory**. The implementation must review the relevant wiki and repository guidance, update any page that has become stale, and record the outcome explicitly in the final work package close-out. For conceptually dense topics such as rule storage layout, App Configuration key mapping, and local seeding workflow, any updated guidance must retain book-like narrative depth, define technical terms when first introduced, and include examples or walkthrough material where that materially improves comprehension.

## Work package structure and expected outputs

This work package uses a single documentation folder:

- `docs/094-rule-storage-changes/`

Planned documents for this work package:

- `docs/094-rule-storage-changes/spec-platform-rule-storage-changes_v0.01.md`
- `docs/094-rule-storage-changes/plan-platform-rule-storage-changes_v0.01.md`

## Planned implementation areas

The execution work is expected to touch or verify the following repository areas:

- `rules/file-share/**`
- `rules/ingestion/file-share/**`
- `src/Hosts/AppHost/AppHost.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration/*`
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`
- `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs` if rule-key path helpers or registrations need shared wiring
- `tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs`
- `tools/RulesWorkbench/RulesWorkbench.csproj`
- `test/UKHO.Aspire.Configuration.Seeder.Tests/*`
- `test/UKHO.Aspire.Configuration.Hosting.Tests/*`
- `test/RulesWorkbench.Tests/*`
- `test/StudioServiceHost.Tests/*`
- relevant wiki and repository guidance pages that still refer to `rules/file-share/...` or `rules:file-share:*`

## Feature Slice 1: Repository rule layout and Aspire seeding contract

- [x] **Work Item 1: Move the rule assets and pin the nested seeding contract end to end - Completed**
  - **Purpose**: Deliver the smallest demonstrable slice by moving the authored rule files into the new repository layout and proving that the existing Aspire configuration seeder still produces the correct nested App Configuration keys from the repository `./rules` root.
  - **Acceptance Criteria**:
    - Rule files are moved from `./rules/file-share/**` to `./rules/ingestion/file-share/**`.
    - The local AppHost configuration-emulator wiring still points at the repository `./rules` root with prefix `rules`.
    - Automated tests prove that files under `rules/ingestion/file-share/**` seed to keys under `rules:ingestion:file-share:*`.
    - No special-case seeder logic is introduced unless tests show the generic recursive path mapping is insufficient.
  - **Definition of Done**:
    - Repository rule assets moved and any impacted project file folder declarations updated.
    - Targeted seeder and hosting tests added or updated and passing.
    - Logging and seeding behavior remain understandable and reviewable.
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for all changed code and test files, including developer-level comments on every changed class, constructor, method, and relevant property, including internal and non-public members.
    - Wiki review obligation considered for this slice; if contributor guidance about rule-file location or local seeding workflow is changed materially, the relevant guidance is updated before completion or explicitly carried into the final wiki review work item.
    - Can execute end-to-end via the AppHost local configuration-emulator path that seeds repository rule files into App Configuration.
  - [x] **Task 1.1: Move the repository rule assets into the new ingestion namespace - Completed**
    - [x] **Step 1**: Moved all checked-in rule JSON files from `rules/file-share/` to `rules/ingestion/file-share/`, preserving filenames.
    - [x] **Step 2**: Updated `tools/RulesWorkbench/RulesWorkbench.csproj` and `src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj` folder declarations to match the new physical rules folder.
    - [x] **Step 3**: Confirmed there are no remaining active source or project references that require the rules to exist physically under `rules/file-share/`.
    - [x] **Step 4**: Applied the mandatory developer-comment standard to the new and changed test files touched by this task.
  - [x] **Task 1.2: Verify and pin the Aspire configuration seeder nested-path behavior - Completed**
    - [x] **Step 1**: Reviewed `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration/AdditionalConfigurationFileEnumerator.cs`, `AdditionalConfigurationKeyBuilder.cs`, and `AdditionalConfigurationSeeder.cs` against the new rule path shape.
    - [x] **Step 2**: Kept the seeder implementation unchanged because the existing generic recursive path-segment behavior already satisfies the new nested rule path.
    - [x] **Step 3**: Added and updated tests in `test/UKHO.Aspire.Configuration.Seeder.Tests/` to pin the key shape `rules:ingestion:file-share:<rule-id>` explicitly.
    - [x] **Step 4**: Added an AppHost source-level regression test and retained the existing hosting-extension coverage so the `rules` root and prefix remain pinned.
    - [x] **Step 5**: Applied the full documentation-pass standard to the changed production-adjacent and test code, including comments explaining the namespace-preserving behavior.
  - [x] **Task 1.3: Validate the new asset layout through the local seeding path - Completed**
    - [x] **Step 1**: Ran targeted seeder, hosting, and AppHost tests only; did not run the full test suite for this work package.
    - [x] **Step 2**: Validated that the moved rule files are discoverable from the repository `./rules` root and seed through the unchanged recursive enumerator path.
    - [x] **Step 3**: Recorded the operational note that existing App Configuration stores may retain stale `rules:file-share:*` keys until explicitly cleaned up.
  - **Files**:
    - `rules/file-share/**`: remove or relocate current rule assets.
    - `rules/ingestion/file-share/**`: new canonical repository location for authored file-share ingestion rules.
    - `tools/RulesWorkbench/RulesWorkbench.csproj`: update folder declarations if required by the new on-disk structure.
    - `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration/*`: verify or amend nested-key seeding behavior.
    - `src/Hosts/AppHost/AppHost.cs`: verify the AppHost still seeds from the repository `./rules` root with prefix `rules`.
    - `test/UKHO.Aspire.Configuration.Seeder.Tests/*`: pin nested-path-to-key behavior.
    - `test/UKHO.Aspire.Configuration.Hosting.Tests/*`: pin AppHost propagation of the additional configuration path and prefix where necessary.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - Run the targeted seeder and hosting tests covering additional configuration path enumeration and AppHost propagation.
    - Start the AppHost local services-mode environment if manual seeding verification is needed.
    - Verify that the repository rule files are loaded from `rules/ingestion/file-share/**` through the existing `./rules` root seeding path.
  - **User Instructions**:
    - If a local App Configuration emulator already contains stale `rules:file-share:*` keys from previous runs, be aware that reseeding adds the new keys but may not remove the old ones automatically.
  - **Summary**: Moved the checked-in file-share rule assets to `rules/ingestion/file-share/`, updated the affected project folder declarations, and proved that the existing recursive Aspire seeder still produces namespace-aware keys without special-case logic.
  - **Validation**: `run_build` succeeded. Targeted tests passed for `UKHO.Aspire.Configuration.Seeder.Tests.AdditionalConfigurationKeyBuilderTests.Build_WhenRuleFileUsesIngestionNamespace_ShouldReturnNamespaceAwareRuleKey`, `UKHO.Aspire.Configuration.Seeder.Tests.AdditionalConfigurationFileEnumeratorTests.GetRelativePathSegments_WhenRuleFileStoredUnderIngestionNamespace_ShouldReturnNamespaceThenProvider`, `UKHO.Aspire.Configuration.Seeder.Tests.AdditionalConfigurationSeederTests.SeedAsync_WhenRuleFileStoredUnderIngestionNamespace_ShouldWriteNamespaceAwareRuleKey`, `UKHO.Aspire.Configuration.Hosting.Tests.DistributedApplicationBuilderExtensionsTests.AddConfigurationEmulator_WhenCalled_ShouldCreateSeederWithCopiedInputsAndMockReferences`, and `AppHost.Tests.RuleConfigurationSeederPathTests.AppHost_services_mode_seeds_the_repository_rules_root_with_the_rules_prefix`.
  - **Wiki review result**: Updated `wiki/Ingestion-Rules.md`, `wiki/Ingestion-Walkthrough.md`, `wiki/Project-Setup.md`, and `wiki/Setup-Walkthrough.md` so the contributor guidance now explains the `rules/ingestion/file-share/...` repository layout, the `rules:ingestion:file-share:*` App Configuration key shape, and the namespace-versus-provider distinction.

## Feature Slice 2: App Configuration-backed ingestion rule runtime and save-back alignment

- [x] **Work Item 2: Make ingestion rule readers and writers namespace-aware while preserving provider identity - Completed**
  - **Purpose**: Deliver the first full runtime slice that can load, enumerate, and save file-share ingestion rules from the new `rules:ingestion:file-share:*` path without changing the logical provider name exposed to the application.
  - **Acceptance Criteria**:
    - App Configuration-backed ingestion rule loading reads from `rules:ingestion:<provider>:*` rather than from `rules:<provider>:*`.
    - App Configuration-backed rule save-back writes to `rules:ingestion:{provider}:{ruleId}`.
    - Runtime consumers continue to expose `file-share` as the provider identity.
    - Any parser or helper that assumes the old three-segment key shape is updated, replaced, or retired.
  - **Definition of Done**:
    - Runtime readers and writers updated to the new key contract.
    - Shared key composition or parsing logic centralized where practical so future drift is reduced.
    - Targeted unit/integration-style tests for rule loading and writing added or updated and passing.
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for all changed code and test files, including comments on all changed classes, methods, constructors, parameters, and relevant properties, plus inline comments explaining the namespace-versus-provider distinction.
    - Wiki review obligation considered for the contributor mental model of rule namespaces and provider identity; if the repository explanation must change, the update is made before completion or explicitly carried into the final wiki review item.
    - Can execute end-to-end via a host or service path that loads rules from App Configuration and can persist an updated rule to the same new namespace.
  - [x] **Task 2.1: Introduce or align a shared ingestion rule App Configuration path contract - Completed**
    - [x] **Step 1**: Reviewed the runtime reader, writer, and parser surfaces and confirmed that a shared helper defining the canonical `rules:ingestion` contract would reduce string-literal drift.
    - [x] **Step 2**: Added `src/UKHO.Search.Infrastructure.Ingestion/Rules/IngestionRuleConfigurationPath.cs` as the shared helper for the ingestion namespace root, provider normalization, key composition, and key parsing.
    - [x] **Step 3**: Kept the helper in `UKHO.Search.Infrastructure.Ingestion` so the path contract remains owned by the runtime infrastructure layer rather than by hosts.
    - [x] **Step 4**: Applied the mandatory documentation-pass standard to the shared contract code, including comments that explain the namespace-versus-provider distinction.
  - [x] **Task 2.2: Update App Configuration-backed ingestion rule loading - Completed**
    - [x] **Step 1**: Updated `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRulesSource.cs` so it enumerates beneath `rules:ingestion` and treats the next segment as the provider.
    - [x] **Step 2**: Ensured canonical keys emitted by the source include the full `rules:ingestion:file-share:<rule-id>` path, including nested rule identifiers such as `subset:rule-2`.
    - [x] **Step 3**: Updated the shared parser logic through `RuleKeyParser` and `IngestionRuleConfigurationPath` so the old three-segment key assumption is retired.
    - [x] **Step 4**: Preserved case-insensitive handling by normalizing provider keys through the shared helper while keeping runtime provider identity canonicalized to lowercase `file-share`.
    - [x] **Step 5**: Added and updated targeted tests in `test/UKHO.Search.Infrastructure.Ingestion.Tests/` and `test/RulesWorkbench.Tests/` so the new path, nested rule-id handling, and provider identity are pinned explicitly.
    - [x] **Step 6**: Followed `./.github/instructions/documentation-pass.instructions.md` for every changed production and test file in scope.
  - [x] **Task 2.3: Update App Configuration-backed rule save-back - Completed**
    - [x] **Step 1**: Updated `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRuleConfigurationWriter.cs` to compose keys using `rules:ingestion:{provider}:{ruleId}`.
    - [x] **Step 2**: Verified the save-back workflow still persists JSON with the correct label and content type by factoring the setting construction into directly testable helper methods.
    - [x] **Step 3**: Added targeted tests that pin the written key, label normalization, and JSON content type.
    - [x] **Step 4**: Applied the mandatory developer-comment and inline-explanation requirements to all changed writer code.
  - [x] **Task 2.4: Validate the runtime rule path end to end - Completed**
    - [x] **Step 1**: Ran targeted ingestion-infrastructure and affected RulesWorkbench parser tests only.
    - [x] **Step 2**: Validated through the normal provider-rules-reader service path that the application can discover file-share rules from `rules:ingestion:file-share:*` while ignoring legacy `rules:file-share:*` keys.
    - [x] **Step 3**: Recorded the migration note that stale legacy keys may remain in persistent App Configuration stores even though the runtime no longer depends on them.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRulesSource.cs`: update App Configuration rule enumeration to the `rules:ingestion` namespace.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRuleConfigurationWriter.cs`: update save-back key composition.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/RuleKeyParser.cs`: update or retire any legacy three-part key assumption.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`: add shared key-path helper code if required.
    - `test/UKHO.Search.Infrastructure.Ingestion.Tests/*`: pin the new key shape, provider identity, and save-back behavior.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - Run the targeted ingestion infrastructure tests related to App Configuration rules.
    - Reseed the local configuration store if needed.
    - Exercise a host or runtime path that loads and, where supported, persists file-share rules through App Configuration.
  - **User Instructions**:
    - Validate against a freshly reseeded local environment when possible so stale legacy keys do not hide incorrect reader behavior.
  - **Summary**: Introduced a shared `IngestionRuleConfigurationPath` helper, updated the App Configuration rules source to enumerate `rules:ingestion` recursively, updated the writer to save to `rules:ingestion:{provider}:{ruleId}`, and retired the legacy three-segment parser assumption.
  - **Validation**: `run_build` succeeded. Targeted tests passed for `UKHO.Search.Ingestion.Tests.Rules.IngestionRuleConfigurationPathTests`, `UKHO.Search.Ingestion.Tests.Rules.AppConfigRulesSourceTests`, `UKHO.Search.Ingestion.Tests.Rules.AppConfigRuleConfigurationWriterTests`, `UKHO.Search.Ingestion.Tests.Rules.AppConfigIngestionRulesRuntimeTests`, and `RulesWorkbench.Tests.RuleKeyParserTests`.
  - **Wiki review result**: Updated `wiki/Ingestion-Rules.md` to document the namespace-aware App Configuration save-back path. Reviewed `wiki/Tools-RulesWorkbench.md` and intentionally left it unchanged for this work item because the tool-side App Configuration snapshot alignment is implemented in Work Item 3 and the page would otherwise describe a partially updated tool flow out of sequence.

## Feature Slice 3: Tooling, host-facing discovery, and contributor guidance alignment

- [x] **Work Item 3: Align RulesWorkbench, Studio-facing tests, and repository guidance with the new rule namespace - Completed**
  - **Purpose**: Deliver the contributor-facing slice so developer tools, host-level discovery surfaces, and repository guidance all reflect the new rule storage and configuration path consistently.
  - **Acceptance Criteria**:
    - RulesWorkbench App Configuration snapshotting and editing reads and writes the new `rules:ingestion:file-share:*` keys correctly.
    - Studio- and host-level tests that seed in-memory rules use the new path.
    - Repository guidance no longer instructs contributors to edit `rules/file-share/...` or to expect `rules:file-share:*`.
    - Any updated guidance explains the new namespace and provider distinction clearly.
  - **Definition of Done**:
    - RulesWorkbench rule snapshot and editing paths aligned to the new key space.
    - Studio- or host-facing rule discovery tests updated and passing.
    - Repository guidance reviewed and updated where needed.
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for all changed code and tests.
    - Wiki review completed for any developer-facing workflow, architecture, terminology, or contributor-guidance impact introduced by this slice.
    - Updated wiki or repository documentation retains book-like narrative depth, explains technical terms when first introduced, and includes examples or walkthrough support where that materially improves understanding.
    - Can execute end-to-end via RulesWorkbench or another host/tool path that discovers or edits rules through the new namespace.
  - [x] **Task 3.1: Update RulesWorkbench App Configuration rule snapshotting and editing - Completed**
    - [x] **Step 1**: Updated `tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs` so it enumerates from the ingestion namespace, parses canonical provider and rule-id metadata, and emits normalized full keys.
    - [x] **Step 2**: Updated the in-memory override path to cache edited JSON beneath the correct `rules:ingestion:{provider}:{ruleId}` key.
    - [x] **Step 3**: Added and updated targeted tests in `test/RulesWorkbench.Tests/` to pin namespace-aware key projection, filtering behavior, and override/update behavior.
    - [x] **Step 4**: Applied the documentation-pass comment standard to the changed tool and test code in scope.
  - [x] **Task 3.2: Update host- and API-facing tests that still seed the old path - Completed**
    - [x] **Step 1**: Updated `test/StudioServiceHost.Tests/*` and affected RulesWorkbench tests so in-memory configuration uses `rules:ingestion:file-share:*`.
    - [x] **Step 2**: Verified through targeted RulesWorkbench and StudioServiceHost test runs that provider discovery and rule projections still present `file-share` rather than `ingestion`.
    - [x] **Step 3**: Kept the host changes source-focused by updating only the affected in-memory rule configuration setup in the existing tests.
    - [x] **Step 4**: Followed `./.github/instructions/documentation-pass.instructions.md` for the changed test files in scope.
  - [x] **Task 3.3: Refresh repository guidance and examples - Completed**
    - [x] **Step 1**: Reviewed the current-state wiki and repository guidance pages that still referred to the old rule path or legacy key shape.
    - [x] **Step 2**: Updated the affected guidance to describe `rules/ingestion/file-share/...` and `rules:ingestion:file-share:*` for RulesWorkbench operations.
    - [x] **Step 3**: Preserved narrative explanation by clarifying that `ingestion` is the namespace segment while `file-share` remains the logical provider.
    - [x] **Step 4**: Included the repository-file to App Configuration-key mapping example in the RulesWorkbench guidance so contributors can reason about the path contract during editing and save-back.
    - [x] **Step 5**: Recorded that historical work-package documents under `docs/` were reviewed but intentionally left unchanged because they describe earlier work-package context rather than current-state contributor guidance.
  - **Files**:
    - `tools/RulesWorkbench/Services/AppConfigRulesSnapshotStore.cs`: align tool-side App Configuration enumeration and override keys.
    - `test/RulesWorkbench.Tests/*`: update RulesWorkbench coverage to the new key path.
    - `test/StudioServiceHost.Tests/*`: update host-facing in-memory rule configuration to the new namespace.
    - `wiki/*.md`: update contributor-facing guidance where the old rule location or key path is still documented.
    - other repository guidance pages reviewed during the mandatory wiki review.
  - **Work Item Dependencies**: Work Item 1 and Work Item 2.
  - **Run / Verification Instructions**:
    - Run the targeted RulesWorkbench and Studio/host tests affected by the new rule namespace.
    - Start the relevant local host or tool path if manual verification is needed.
    - Confirm rule discovery or editing still presents the provider as `file-share` while storing under `rules:ingestion:file-share:*`.
  - **User Instructions**:
    - When manually reviewing updated guidance, confirm that the documented repository path, seeded key path, and tool/runtime behavior all tell the same story.
  - **Summary**: Updated RulesWorkbench snapshot loading and in-memory editing to use namespace-aware App Configuration keys, refreshed the affected RulesWorkbench and StudioServiceHost tests to seed `rules:ingestion:file-share:*`, and aligned the RulesWorkbench wiki guidance with the new path contract.
  - **Validation**: `run_build` succeeded. Targeted tests passed for `RulesWorkbench.Tests.AppConfigRulesSnapshotStoreTests`, `RulesWorkbench.Tests.RuleCheckerServiceTests`, `RulesWorkbench.Tests.RuleKeyParserTests`, and `test/StudioServiceHost.Tests/StudioServiceHost.Tests.csproj` via `dotnet test`.
  - **Wiki review result**: Updated `wiki/Tools-RulesWorkbench.md` to describe the namespace-aware RulesWorkbench read/save path. Reviewed `wiki/Ingestion-Rules.md` and left it unchanged because Work Item 2 already captured the runtime save-back contract accurately. Reviewed historical work-package documents under `docs/` and intentionally left them unchanged because they preserve historical implementation context rather than current-state contributor guidance.

## Feature Slice 4: Final validation and mandatory wiki review close-out

- [ ] **Work Item 4: Record validation outcomes and complete the mandatory wiki review for the full work package**
  - **Purpose**: Close the work package with explicit validation evidence and the mandatory wiki-maintenance record required by `./.github/instructions/wiki.instructions.md`.
  - **Acceptance Criteria**:
    - The final execution record identifies the targeted tests and any manual verification paths used for this work package.
    - The final execution record explicitly states which wiki or repository guidance pages were updated, created, retired, or why no update was needed.
    - The recorded documentation outcome reflects the new current-state rule storage and App Configuration key contract accurately.
  - **Definition of Done**:
    - Targeted validation completed and recorded.
    - Mandatory wiki review completed and recorded explicitly.
    - If wiki changes are required, they are made before this work item is marked complete.
    - If no wiki changes are required for any reviewed page, the recorded no-change result names the pages reviewed and explains why they remain sufficient.
    - The work package close-out summary references `./.github/instructions/documentation-pass.instructions.md` and `./.github/instructions/wiki.instructions.md` as completion gates that were satisfied.
  - [ ] **Task 4.1: Record targeted validation outcomes**
    - [ ] **Step 1**: List the targeted test projects, test classes, or test filters that were executed for seeder, ingestion infrastructure, RulesWorkbench, and affected host surfaces.
    - [ ] **Step 2**: Record any manual validation performed through AppHost, RulesWorkbench, or other relevant tooling.
    - [ ] **Step 3**: Record whether reseeding or manual cleanup of stale legacy keys was required during verification.
  - [ ] **Task 4.2: Perform and record the mandatory wiki review**
    - [ ] **Step 1**: Review the wiki and repository guidance pages most likely to be affected by rule storage path and configuration-key terminology changes.
    - [ ] **Step 2**: Decide which pages must be updated, created, retired, or left unchanged.
    - [ ] **Step 3**: Ensure any updated foundational guidance uses longer, book-like narrative prose, explains technical terms when first introduced, and includes relevant examples or walkthrough support where useful.
    - [ ] **Step 4**: Record the final wiki review result in the explicit format required by `./.github/instructions/wiki.instructions.md`.
  - [ ] **Task 4.3: Produce the final work package close-out note**
    - [ ] **Step 1**: Summarize the implementation approach used to separate the ingestion namespace from the logical provider identity.
    - [ ] **Step 2**: Summarize the key operational considerations, especially nested seeding behavior, stale legacy key cleanup, and the importance of keeping repository path and configuration key path guidance synchronized.
    - [ ] **Step 3**: Record the final documentation and wiki outcome clearly so future contributors understand the current source of truth.
  - **Files**:
    - `docs/094-rule-storage-changes/plan-platform-rule-storage-changes_v0.01.md`: execution tracking against the planned work items if the repository keeps completion state in-plan.
    - reviewed `wiki/*.md` or repository guidance pages as required by the final review outcome.
  - **Work Item Dependencies**: Work Item 1, Work Item 2, and Work Item 3.
  - **Run / Verification Instructions**:
    - Confirm the targeted tests for all affected slices passed.
    - Confirm any required manual reseeding or host/tool verification completed.
    - Confirm the wiki review result is explicitly recorded before closing the work package.
  - **User Instructions**:
    - None beyond the normal targeted-validation and wiki-review expectations for this repository.

## Suggested execution summary

The recommended implementation order is:

1. move the rule assets and pin the nested seeding contract from the repository `./rules` root;
2. update the App Configuration-backed ingestion runtime and save-back path to `rules:ingestion:{provider}:{ruleId}`;
3. align RulesWorkbench, host-facing tests, and contributor guidance to the new namespace;
4. run targeted validation only, not the full test suite for this work package; and
5. finish with the mandatory wiki review and explicit close-out record.

## Key implementation considerations

- **Keep the seeder generic**: the current Aspire configuration seeder already derives nested keys from subfolders, so avoid special-case rule logic unless targeted tests prove a real gap.
- **Separate namespace from provider identity**: `ingestion` is a configuration namespace segment, while `file-share` remains the logical provider exposed by runtime and tooling.
- **Reduce future drift**: where practical, centralize the ingestion rule App Configuration path contract so readers, writers, tools, and tests compose the same keys consistently.
- **Treat stale keys as a migration concern**: reseeding creates new `rules:ingestion:file-share:*` keys but does not automatically remove old `rules:file-share:*` keys from persistent configuration stores.
- **Update both code and guidance**: this work changes contributor-facing rule authoring paths and terminology, so documentation and wiki updates are part of the implementation, not an afterthought.
- **Do not run the full test suite for this work package**: use targeted tests only, in line with repository guidance.

## Overall approach summary

The implementation should first prove that the repository path move and the local seeding path still work together, then switch the application's App Configuration readers and writers to the new namespace-aware contract, and finally bring tooling, tests, and contributor guidance into alignment. The main technical care points are preserving `file-share` as the provider identity, making the `ingestion` namespace explicit but not semantically overloaded, and preventing future drift by keeping the rule-key contract consistently defined across production code, tests, and documentation.