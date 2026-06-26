# Next-Gen Arc 08 Work Packages: React Developer Ingestion Repair Workspace

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 08 builds the failure-driven ingestion repair workspace in the React app. It consumes Arc 03 frontend foundations, Arc 05 journal/failure model, and Arc 06 provider/rules/repair APIs.

## Numbering

Arc 08 work packages use WP240-WP248.

Reserved buffer before Arc 09: WP249-WP259.

## Evidence Checked

- Current ingestion runtime has dead-letter and poison paths but no task-oriented repair UI or API: [../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs), [../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs), and [../../src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Terminal/DeadLetterPersistAndAckSinkNode.cs](../../src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Terminal/DeadLetterPersistAndAckSinkNode.cs).
- Current RulesWorkbench evaluates file-share SQL-loaded payloads rather than journaled accepted inputs: [../../tools/RulesWorkbench/Services/RuleEvaluationService.cs](../../tools/RulesWorkbench/Services/RuleEvaluationService.cs), [../../tools/RulesWorkbench/Services/RuleCheckerService.cs](../../tools/RulesWorkbench/Services/RuleCheckerService.cs), and [../../tools/RulesWorkbench/Services/BatchPayloadLoader.cs](../../tools/RulesWorkbench/Services/BatchPayloadLoader.cs).
- FileShareEmulator remains local-only and is not a React migration target: [../../tools/FileShareEmulator/Program.cs](../../tools/FileShareEmulator/Program.cs).

## WP240: Build Failure Work Queue

Scope:
- Add ingestion repair workspace route and failure work queue UI.

Requirements carried:
- The developer opens a failures work queue rather than a generic tool list.
- The list groups/filters by provider, time range, request type, error category/code, failed node, failure type, document id, supersession status, and repair eligibility.
- The list distinguishes provider handoff/ingress-gate failures from post-ingress ingestion-owned failures.

Validation anchors:
- Component and Playwright tests with mocked failure API data.

## WP241: Build Failure Detail And Dead-Letter View

Scope:
- Build failure detail UI around the Arc 06 failure graph.

Requirements carried:
- Show failed node, error category/code/message, breadcrumbs, timestamps, provider, document id, request type, queue metadata, `ShadowId`, dead-letter reference, payload pointers, replay eligibility, and likely failure class.
- Provider handoff and ingress-gate failures are generally not repairable by ingestion.

Validation anchors:
- Tests for failure classes, missing dead-letter, missing shadow input, and unauthorized detail access.

## WP242: Build Shadow Input Inspector

Scope:
- Build view over the journaled provider-normalized `IngestionRequest` that ingestion accepted.

Requirements carried:
- Show normalized request JSON and raw queue JSON when captured.
- Do not reconstruct File Share batch data from SQL.
- Show provider, document id, request type, source timestamp, received timestamp, payload hash, status, supersession state, and replay lineage.

Validation anchors:
- Tests for normalized-only, raw-and-normalized, missing raw, missing payload, superseded, and replayed states.

## WP243: Build Rule Diagnostic Evaluation Flow

Scope:
- Let users run current or draft ingestion rules against the pinned shadow input.

Requirements carried:
- Show matched rules, candidate-but-unmatched rules, runtime warnings, validation errors, missing required canonical fields, action summaries, and canonical document output.
- Use backend APIs over journaled input, not UI-local mapping.
- Rule/debug evaluation is separate from full pipeline/live replay.

Validation anchors:
- Tests for matched, no-match, missing fields, validation errors, warnings, and backend failures.

## WP244: Build Rule Authoring With Pinned ShadowId Test Case

Scope:
- Integrate ingestion rule authoring so a failing shadow input remains pinned through edit/validate/rerun cycles.

Requirements carried:
- Rule editor opens with the failing `ShadowId` as the active test case.
- User does not copy payload JSON between screens.
- Saving valid rules is available only if backend APIs and policies allow it.

Validation anchors:
- Tests for pinned context retention, rerun after edit, disabled save, validation errors, and navigation back to failure.

## WP245: Build Supersession And Replay Eligibility UI

Scope:
- Present backend replay eligibility before live mutation.

Requirements carried:
- Show whether the input is still the latest known accepted/successful input for provider/document.
- Superseded inputs are blocked from normal live repair replay by default; diagnostic replay remains available.
- Stale `IndexItem`, `DeleteItem`, and `UpdateAcl` requests can all be unsafe.
- Forced replay, if present, is explicit, authorized, audited, and visually distinct.

Validation anchors:
- Tests for eligible, superseded, diagnostic-only, forced-unavailable, forced-unauthorized, and artifact-warning states.

## WP246: Build Diagnostic Replay And Guarded Repair Replay Actions

Scope:
- Implement UI actions for diagnostic replay and guarded live repair replay.

Requirements carried:
- Diagnostic replay is safe and repeatable.
- Guarded repair replay mutates live state and uses backend freshness checks.
- Replay creates a new attempt/lineage entry linked to original `ShadowId` and shows success, dead-letter-again, blocked, and failed outcomes.

Validation anchors:
- Tests for diagnostic replay, guarded replay success, blocked replay, dead-letter-again, and API failure states.

## WP247: Build Repair Navigation Graph And History

Scope:
- Connect original failure, shadow input, rule report, replay attempts, and outcomes into one navigable workflow.

Requirements carried:
- User can navigate repair history without separate log/blob/storage inspection.
- Replay lineage uses `ReplayOfShadowId`.

Validation anchors:
- Tests for original-to-replay-to-outcome navigation and missing related records.

## WP248: Document Ingestion Repair Workspace Workflow

Scope:
- Document inspect failure, inspect shadow input, run rules/debug, fix, verify, check supersession, guarded repair, and outcome tracking.

Requirements carried:
- Explain diagnostic replay, guarded repair replay, and forced replay differences.
- State that FileShareEmulator local destructive operations remain outside React.
- State that UI consumes task-oriented APIs rather than storage/provider internals.

Validation anchors:
- Documentation cross-check against Arc 05 and Arc 06 contracts.

## Arc Requirement Cross-Check

- Failure/dead-letter-driven primary journey: WP240-WP248.
- Failure detail with node, error, breadcrumbs, queue metadata, `ShadowId`, dead-letter link, payload pointers, and replay eligibility: WP241.
- Exact accepted `ShadowInput` inspection without File Share SQL reconstruction: WP242.
- Current/draft rule diagnostics and pinned rule authoring: WP243-WP244.
- Supersession and replay eligibility before live mutation: WP245.
- Diagnostic replay, guarded repair replay, forced replay distinction: WP245-WP246.
- Replay lineage and repair outcome tracking: WP246-WP247.
- FileShareEmulator local destructive operations not moved into React: WP248.

## Handoff To Arc 10

Arc 10 retires replaced Blazor/Workbench surfaces only after this workspace and its APIs cover the operational repair loop.