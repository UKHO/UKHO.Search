# Next-Gen Arc 06 Work Packages: Provider Tooling, Ingestion Rules, And Repair APIs

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 06 builds provider-neutral developer APIs and service boundaries over ingestion rules, providers, journaled inputs, dead letters, outcomes, replay eligibility, diagnostic replay, and guarded repair replay. It also rationalizes file-share-specific duplication without moving FileShareEmulator's local UI or destructive controls into the React application.

## Numbering

Arc 06 work packages use WP200-WP209.

Reserved buffer before Arc 07: WP210-WP219.

## Evidence Checked

- Ingestion rules engine and App Configuration writer are registered in [../../src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs). Relevant interfaces include [../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IProviderRulesReader.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IProviderRulesReader.cs), [../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IRuleConfigurationWriter.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IRuleConfigurationWriter.cs), and [../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IIngestionRulesEngine.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Rules/IIngestionRulesEngine.cs).
- RulesWorkbench directly wires rule loading, validation, save-back, evaluation, checker, SQL batch loading, and business-unit scan in [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs).
- File Share reconstruction is duplicated in [../../tools/FileShareEmulator/Services/IndexService.cs](../../tools/FileShareEmulator/Services/IndexService.cs), [../../tools/RulesWorkbench/Services/BatchPayloadLoader.cs](../../tools/RulesWorkbench/Services/BatchPayloadLoader.cs), and [../../src/Providers/UKHO.Search.Studio.Providers.FileShare/FileShareStudioIngestionRequestFactory.cs](../../src/Providers/UKHO.Search.Studio.Providers.FileShare/FileShareStudioIngestionRequestFactory.cs).
- Retained Studio API/provider code remains on disk as retirement-bound historical source, not future platform direction. It is relevant here only as evidence of duplicated file-share-specific behavior that later backend and cleanup work must absorb or remove: [../../src/Studio/StudioServiceHost/Api/IngestionApi.cs](../../src/Studio/StudioServiceHost/Api/IngestionApi.cs), [../../src/Studio/StudioServiceHost/Api/OperationsApi.cs](../../src/Studio/StudioServiceHost/Api/OperationsApi.cs), and [../../src/Providers/UKHO.Search.Studio.Providers.FileShare/FileShareStudioProvider.cs](../../src/Providers/UKHO.Search.Studio.Providers.FileShare/FileShareStudioProvider.cs).

## WP200: Define Provider-Neutral Developer API Surface

Scope:
- Define provider metadata, contexts, payload, journal, failure, rules, replay, and operation APIs using Arc 02 host/security decisions.

Requirements carried:
- List providers and provider metadata.
- List provider contexts and payload lookup where appropriate.
- List journal entries and fetch shadow input metadata/payloads by `ShadowId`.
- Show outcomes, dead-letter records, supersession, replay eligibility, queue status, dead-letter status, indexing status, and operation status where intentionally supported.
- Do not expose provider SQL tables, storage partition keys, blob names, or emulator internals.

Validation anchors:
- Contract/OpenAPI tests for provider, journal, failure, and operation endpoints.

## WP201: Extract Ingestion Rule Management APIs From RulesWorkbench

Scope:
- Move rule list/get/validate/save/refresh behavior behind backend APIs.

Requirements carried:
- List rule-capable providers and rules by provider/context.
- Get, validate, save, and refresh rule documents.
- Use backend primitives such as `IRuleConfigurationWriter`, `IProviderRulesReader`, and validation services.
- Save only valid rules, enforce Arc 02 authorization/audit/conflict handling, and trigger refresh.
- React calls rule-focused APIs, not the configuration emulator explorer.

Validation anchors:
- API tests mirroring RulesWorkbench service tests and App Configuration writer tests.

## WP202: Add Rule Evaluation APIs For Supplied And Journaled Inputs

Scope:
- Expose backend rule evaluation for supplied payloads and journaled `ShadowId` inputs.

Requirements carried:
- Evaluate supplied payloads for a selected provider.
- Evaluate a shadowed ingestion input by `ShadowId`.
- Run current and draft rules against a shadowed input without contacting the provider.
- Return matched rules, candidate-but-unmatched rules, missing required fields, warnings, validation errors, action summaries, canonical output, and links back to failure/dead-letter context.

Validation anchors:
- Tests using existing rules engine and journaled input fixtures.

## WP203: Define Rule Checker And Context Scan APIs

Scope:
- Generalize RulesWorkbench checker and business-unit scan workflows behind provider-oriented APIs.

Requirements carried:
- Check payloads or batches against candidate rules.
- Scan provider contexts for problematic payloads.
- Return candidate-but-unmatched rules and missing required fields.
- Provide rule schema and builder metadata.
- First implementation may be file-share-only if explicitly named, but the React API shape must not be SQL-shaped.
- Preserve the file-share checker convention `bu-{businessunitname}-*` in lowercase where relevant.

Validation anchors:
- RulesWorkbench checker/scan tests migrated or mirrored as API/service tests.

## WP204: Consolidate File-Share Payload Reconstruction Ownership

Scope:
- Choose one backend owner for file-share batch lookup, payload construction, security-token calculation if upstream, queue submission, indexing status updates, reset operations, and business-unit lookup.

Requirements carried:
- Current duplication across FileShareEmulator, RulesWorkbench, and retirement-bound retained Studio provider code must be addressed without treating retained Studio as a future API or provider direction.
- FileShareEmulator UI remains local-only and unchanged except for safe internal service reuse.
- React must not call file-share SQL-shaped APIs.
- Token derivation direction remains explicit; moving it into ingestion/provider runtime is a later contract change.

Validation anchors:
- Golden payload JSON parity tests and regression tests for emulator indexing behavior.

## WP205: Build Failure Workflow APIs Over Journal, Outcomes, And Dead Letters

Scope:
- Expose task-oriented failure APIs for the Arc 08 repair workspace.

Requirements carried:
- List failures by provider, time range, request type, error category, failed node, document id, and supersession status.
- Get failure detail with node, error, breadcrumbs, timestamps, provider, document id, request type, queue metadata, `ShadowId`, dead-letter link, payload pointers, taxonomy, and replay eligibility.
- Distinguish provider handoff/ingress-gate failures from post-ingress ingestion-owned failures.

Validation anchors:
- API tests using journal/outcome/dead-letter fixtures.

## WP206: Implement Diagnostic Replay APIs

Scope:
- Add non-live replay APIs that run rules, canonicalization, mapping, or selected processing against a shadowed input.

Requirements carried:
- Diagnostic replay remains available for superseded inputs.
- It does not update live index state.
- It reports provider-dependent artifact limitations, such as source ZIP availability.

Validation anchors:
- Tests proving diagnostic replay does not mutate live state.

## WP207: Implement Guarded Repair Replay APIs

Scope:
- Add guarded live repair replay for failed accepted inputs when freshness checks pass.

Requirements carried:
- Repair replay is blocked by default for superseded inputs.
- Replay creates a new attempt/outcome and `ReplayOfShadowId` lineage.
- Stale `IndexItem`, `DeleteItem`, and `UpdateAcl` are all unsafe.

Validation anchors:
- Tests for allowed replay, stale replay block, missing artifact, lineage, and audit emission.

## WP208: Define Forced Replay Governance Or Defer It

Scope:
- Decide whether forced replay exists; if so, define strict authorization, audit, and UI restrictions.

Requirements carried:
- Forced replay bypasses normal freshness guards and can overwrite newer provider updates.
- It must not be a casual developer workflow.

Validation anchors:
- Authorization/audit tests if implemented; route inventory proving absence if deferred.

## WP209: Document API-To-UI Workflow Contracts

Scope:
- Document how Arc 08 consumes provider, failure, journal, rule evaluation, replay eligibility, and replay APIs.

Requirements carried:
- UI consumes task-oriented graph concepts: `ShadowInput`, `IngestionOutcome`, `DeadLetterRecord`, `RuleEvaluationReport`, `ReplayEligibility`, and `ReplayAttempt`.
- UI does not read storage blobs/tables or provider SQL directly.

Validation anchors:
- Documentation cross-check against Arc 05 and Arc 08.

## Arc Requirement Cross-Check

- Provider metadata, contexts, payload lookup, operation status/events, queue/dead-letter/index status: WP200.
- Ingestion rule list/get/validate/save/refresh APIs: WP201.
- Current/draft rule evaluation against supplied and journaled inputs: WP202.
- Checker, scan, candidate rules, missing fields, schema/builder metadata: WP203.
- File-share duplication consolidated while keeping emulator local: WP204.
- Failure APIs with `ShadowId`, payload pointers, taxonomy, and replay eligibility: WP205.
- Diagnostic replay and guarded repair replay: WP206-WP207.
- Forced replay authorization/audit or deferral: WP208.
- React does not see storage/provider internals: WP200-WP209.

## Handoff To Arc 08

Arc 08 builds the React ingestion repair workspace over these APIs.