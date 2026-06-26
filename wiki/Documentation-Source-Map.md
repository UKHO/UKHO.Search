# Documentation source map

This wiki was synthesized from the historical markdown corpus under `dev/work-packages/mvp/`.

The goal of this page is not to duplicate every work package, but to help developers trace each major topic back to the source material that shaped it.

## How to use this page

- Start with the wiki pages for the current implementation view.
- Use this source map when you want the historical rationale, earlier plans, or superseded design discussions.
- Many directories contain specs, plans, architecture notes, and archives; read the current/superseding file first where applicable.

Studio and Theia work packages remain in `dev/work-packages/mvp/` as historical design records only. They describe a discontinued workflow and should not be treated as setup, runtime, or verification guidance for the current repository baseline.

## Foundational architecture and ingestion history

### `dev/work-packages/mvp/000-ingestion-model`
Early ingestion model framing.

### `dev/work-packages/mvp/001-onion-architecture`
Repository onion-architecture rule set and dependency direction.

### `dev/work-packages/mvp/002-pipeline-playground`
Early pipeline experimentation and architecture thinking.

### `dev/work-packages/mvp/004-ingestion-model-uplift`
Evolution of ingestion request shapes and related domain contracts.

### `dev/work-packages/mvp/006-ingestion-service`
Core ingestion-service runbook and architecture, including pipeline topology.

### `dev/work-packages/mvp/007-enrichment`
Major enrichment design history across core and File Share enrichment work.

### `dev/work-packages/mvp/008-provider-refactor`
Provider/infrastructure boundary refactor and queue ownership model.

### `dev/work-packages/mvp/009-metric-integration`
Pipeline metrics and Aspire/OpenTelemetry integration.

### `dev/work-packages/mvp/010-ingestion-uplift`
Ingestion request file metadata uplift.

### `dev/work-packages/mvp/011-canonical-document`
Canonical document model and index-mapping evolution.

### `dev/work-packages/mvp/012-ingestion-rules`
Rules-engine architecture and provider-scoped enrichment design.

## File Share content, parsing, and geo work

### `dev/work-packages/mvp/014-kreuzberg-extraction`
Kreuzberg-based content extraction design.

### `dev/work-packages/mvp/015-canonical-document-uplift`
Further canonical-document and enrichment-model evolution.

### `dev/work-packages/mvp/016-DSL-facets`
Rules DSL support for facets.

### `dev/work-packages/mvp/020-batch-enrich`
Batch content enrichment work package.

### `dev/work-packages/mvp/021-specific-batch`
Targeted/specific batch support in tooling/workflows.

### `dev/work-packages/mvp/022-nested-zip`
Nested ZIP extraction behavior.

### `dev/work-packages/mvp/023-geo-ingestion`
Geo polygon support in `CanonicalDocument`.

### `dev/work-packages/mvp/024-s101-parsing`
S-101 parsing behavior and design.

### `dev/work-packages/mvp/025-s101-parser`
Dedicated S-101 parser work package.

### `dev/work-packages/mvp/026-s57-parser`
S-57 parsing, dataset detection, and the spec template used for newer docs.

### `dev/work-packages/mvp/027-parser-refactor`
Parser refactor work.

### `dev/work-packages/mvp/050-geo-polygon-fixes`
Recent fixes for Elasticsearch-facing geo polygon serialization.

### `dev/work-packages/mvp/052-provider-canonical-field`
Addition of the system-managed `Provider` field to `CanonicalDocument` and its propagation through the ingestion pipeline and index mapping.

## Canonical discovery taxonomy and rule evolution

### `dev/work-packages/mvp/028-consolidate-insert`
Envelope/index-item consolidation work.

### `dev/work-packages/mvp/029-new-canonical-fields`
Universal discovery taxonomy fields for `CanonicalDocument`.

### `dev/work-packages/mvp/030-rule-engine-additions`
Rules-engine additions beyond the first rules-engine cut.

### `dev/work-packages/mvp/031-remove-canonical-fields`
Removal/simplification of obsolete canonical fields.

### `dev/work-packages/mvp/032-rule-workbench`
RulesWorkbench feature design.

### `dev/work-packages/mvp/033-rule-storage`
Per-rule JSON storage design and migration thinking.

### `dev/work-packages/mvp/034-ingestion-rule-parsing-operators`
Typed parsing operators such as `toInt(...)`.

### `dev/work-packages/mvp/036-initial-test-rules`
Early rule-authoring examples.

### `dev/work-packages/mvp/037-rule-engine-case`
Case-sensitivity/normalization behavior for rule evaluation.

### `dev/work-packages/mvp/038-path-parsing-fix`
Path parsing fixes, especially around `$path:` handling.

### `dev/work-packages/mvp/039-ingestion-mode`
Ingestion mode and FileShareImageBuilder pruning behavior.

### `dev/work-packages/mvp/040-load-additional-config`
Loading additional configuration (including rules) into local config/runtime flows.

### `dev/work-packages/mvp/041-ingestion-workbench-config-rules`
RulesWorkbench/IngestionServiceHost config-backed rules work.

### `dev/work-packages/mvp/042-best-effort-ingestion`
Best-effort ingestion and missing ZIP behavior.

### `dev/work-packages/mvp/043-remove-document-id`
Elasticsearch `documentId` cleanup.

### `dev/work-packages/mvp/044-rule-discovery`
Rule discovery and mapping proposals.

### `dev/work-packages/mvp/045-token-normalization`
Token normalization and canonical mutator rationalization.

### `dev/work-packages/mvp/046-rule-checker`
RulesWorkbench checker, candidate-rule identification, and rule-definition uplift.

### `dev/work-packages/mvp/056-rules-workbench-scan-all`
RulesWorkbench checker scan workflow uplift, including bounded `Scan` and unbounded `Scan All` business-unit actions.

### `dev/work-packages/mvp/054-rule-title`
Canonical document title, mandatory `rule.title`, post-enrichment title validation, and repository/tooling alignment work.

### `dev/work-packages/mvp/055-rule-exists-semantics`
Boolean `exists` operator semantics, including support for both `exists: true` and `exists: false` in runtime evaluation, tests, tooling, and docs.

## Emulator and local-tooling history

### `dev/work-packages/mvp/005-emulator-security`
Emulator security history.

### `dev/work-packages/mvp/017-emulator-stats`
Statistics features for FileShareEmulator.

### `dev/work-packages/mvp/018-emulator-download`
Batch-download capability in the emulator.

### `dev/work-packages/mvp/019-queue-clear`
Queue clearing behavior and UX.

### `dev/work-packages/mvp/035-fsemualator-common`
Shared emulator/common abstraction work.

### `dev/work-packages/mvp/048-emulator-index-bu`
Business-unit indexing in FileShareEmulator.

## Diagnostics and operations

### `dev/work-packages/mvp/049-deadletter-enhancement`
Richer dead-letter diagnostic payloads, including runtime payload snapshots.

## Documentation and quality work

### `dev/work-packages/mvp/051-wiki`
Developer wiki creation and consolidation of the historical `dev/work-packages/mvp/` corpus into the current `wiki/` guidance set.

### `dev/work-packages/mvp/053-test-coverage-gaps`
Repository-wide test coverage baseline assessment and identification of important subsystem and behavior coverage gaps.

### `dev/work-packages/mvp/059-test-refactor`
Implementation planning and delivery tracking for the project-aligned test-estate refactor, including shared sample-data consolidation, matching test-project creation, provider/infrastructure/integration ownership cleanup, and the final solution-wide test audit.

## Historical Studio / Theia design lineage (retained for reference only)

### `dev/work-packages/mvp/057-studio-shell`
Initial Studio shell planning for the now-discontinued Theia-based developer workflow.

### `dev/work-packages/mvp/058-studio-config`
Historical design package for propagating a studio API endpoint into the discontinued Theia shell.

### `dev/work-packages/mvp/060-studio-host-rename`
Historical rename work for the detached studio API host lineage.

### `dev/work-packages/mvp/061-provider-metadata`
Provider metadata model and split-registration design history, including earlier detached studio/tooling composition.

### `dev/work-packages/mvp/062-studio-provider`
Historical Studio provider extraction and tandem provider registration work for the discontinued studio workflow.

### `dev/work-packages/mvp/063-provider-metadata-rule-loading`
Historical provider-aware rules-loading work, including read-only rule discovery for detached studio tooling.

Several later Theia- and PrimeReact-only Studio work packages were intentionally deleted during cleanup because they described a discontinued client direction and no longer belong in the retained documentation baseline.

### Top-level operational docs

| Path | Topic |
|---|---|
| `docs/README.md` | General docs/prompt asset overview. |
| `docs/mcp-setup.md` | MCP setup guidance for repo tooling. |
| `docs/reuse-docs-folder.md` | Reuse guidance for docs template assets. |
| `docs/reuse-github-folder.md` | Reuse guidance for `.github` prompt assets. |

## Working rule of thumb

Use the wiki for the **current implementation view**.
Use the historical `dev/work-packages/mvp/` work packages for the **why, when, and how the design evolved**.

## Main wiki pages

- [Home](Home)
- [Solution architecture](Solution-Architecture)
- [Project setup](Project-Setup)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Ingestion pipeline](Ingestion-Pipeline)
- [Ingestion rules](Ingestion-Rules)
- [CanonicalDocument and discovery taxonomy](CanonicalDocument-and-Discovery-Taxonomy)
- [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism)
- [Provider metadata and split registration](Provider-Metadata-and-Split-Registration)
- [File Share provider](FileShare-Provider)
