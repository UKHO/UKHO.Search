# Documentation source map

This wiki was synthesized from the historical markdown corpus under `docs/`.

The goal of this page is not to duplicate every work package, but to help developers trace each major topic back to the source material that shaped it.

## How to use this page

- Start with the wiki pages for the current implementation view.
- Use this source map when you want the historical rationale, earlier plans, or superseded design discussions.
- Many directories contain specs, plans, architecture notes, and archives; read the current/superseding file first where applicable.

## Foundational architecture and ingestion history

### `docs/000-ingestion-model`
Early ingestion model framing.

### `docs/001-onion-architecture`
Repository onion-architecture rule set and dependency direction.

### `docs/002-pipeline-playground`
Early pipeline experimentation and architecture thinking.

### `docs/004-ingestion-model-uplift`
Evolution of ingestion request shapes and related domain contracts.

### `docs/006-ingestion-service`
Core ingestion-service runbook and architecture, including pipeline topology.

### `docs/007-enrichment`
Major enrichment design history across core and File Share enrichment work.

### `docs/008-provider-refactor`
Provider/infrastructure boundary refactor and queue ownership model.

### `docs/009-metric-integration`
Pipeline metrics and Aspire/OpenTelemetry integration.

### `docs/010-ingestion-uplift`
Ingestion request file metadata uplift.

### `docs/011-canonical-document`
Canonical document model and index-mapping evolution.

### `docs/012-ingestion-rules`
Rules-engine architecture and provider-scoped enrichment design.

## File Share content, parsing, and geo work

### `docs/014-kreuzberg-extraction`
Kreuzberg-based content extraction design.

### `docs/015-canonical-document-uplift`
Further canonical-document and enrichment-model evolution.

### `docs/016-DSL-facets`
Rules DSL support for facets.

### `docs/020-batch-enrich`
Batch content enrichment work package.

### `docs/021-specific-batch`
Targeted/specific batch support in tooling/workflows.

### `docs/022-nested-zip`
Nested ZIP extraction behavior.

### `docs/023-geo-ingestion`
Geo polygon support in `CanonicalDocument`.

### `docs/024-s101-parsing`
S-101 parsing behavior and design.

### `docs/025-s101-parser`
Dedicated S-101 parser work package.

### `docs/026-s57-parser`
S-57 parsing, dataset detection, and the spec template used for newer docs.

### `docs/027-parser-refactor`
Parser refactor work.

### `docs/050-geo-polygon-fixes`
Recent fixes for Elasticsearch-facing geo polygon serialization.

## Canonical discovery taxonomy and rule evolution

### `docs/028-consolidate-insert`
Envelope/index-item consolidation work.

### `docs/029-new-canonical-fields`
Universal discovery taxonomy fields for `CanonicalDocument`.

### `docs/030-rule-engine-additions`
Rules-engine additions beyond the first rules-engine cut.

### `docs/031-remove-canonical-fields`
Removal/simplification of obsolete canonical fields.

### `docs/032-rule-workbench`
RulesWorkbench feature design.

### `docs/033-rule-storage`
Per-rule JSON storage design and migration thinking.

### `docs/034-ingestion-rule-parsing-operators`
Typed parsing operators such as `toInt(...)`.

### `docs/036-initial-test-rules`
Early rule-authoring examples.

### `docs/037-rule-engine-case`
Case-sensitivity/normalization behavior for rule evaluation.

### `docs/038-path-parsing-fix`
Path parsing fixes, especially around `$path:` handling.

### `docs/039-ingestion-mode`
Ingestion mode and FileShareImageBuilder pruning behavior.

### `docs/040-load-additional-config`
Loading additional configuration (including rules) into local config/runtime flows.

### `docs/041-ingestion-workbench-config-rules`
RulesWorkbench/IngestionServiceHost config-backed rules work.

### `docs/042-best-effort-ingestion`
Best-effort ingestion and missing ZIP behavior.

### `docs/043-remove-document-id`
Elasticsearch `documentId` cleanup.

### `docs/044-rule-discovery`
Rule discovery and mapping proposals.

### `docs/045-token-normalization`
Token normalization and canonical mutator rationalization.

### `docs/046-rule-checker`
RulesWorkbench checker, candidate-rule identification, and rule-definition uplift.

### `docs/054-rule-title`
Canonical document title, mandatory `rule.title`, post-enrichment title validation, and repository/tooling alignment work.

## Emulator and local-tooling history

### `docs/005-emulator-security`
Emulator security history.

### `docs/017-emulator-stats`
Statistics features for FileShareEmulator.

### `docs/018-emulator-download`
Batch-download capability in the emulator.

### `docs/019-queue-clear`
Queue clearing behavior and UX.

### `docs/035-fsemualator-common`
Shared emulator/common abstraction work.

### `docs/048-emulator-index-bu`
Business-unit indexing in FileShareEmulator.

## Diagnostics and operations

### `docs/049-deadletter-enhancement`
Richer dead-letter diagnostic payloads, including runtime payload snapshots.

### Top-level operational docs

| Path | Topic |
|---|---|
| `docs/azureacr.md` | Pulling/pushing the shared File Share data image via ACR. |
| `docs/ingestion-rules.md` | Current practical rule-authoring guide and schema semantics. |
| `docs/metrics.md` | Aspire metrics for the ingestion pipeline. |
| `docs/README.md` | General docs/prompt asset overview. |
| `docs/mcp-setup.md` | MCP setup guidance for repo tooling. |
| `docs/reuse-docs-folder.md` | Reuse guidance for docs template assets. |
| `docs/reuse-github-folder.md` | Reuse guidance for `.github` prompt assets. |

## Working rule of thumb

Use the wiki for the **current implementation view**.
Use the historical `docs/` work packages for the **why, when, and how the design evolved**.

## Main wiki pages

- [Home](Home)
- [Solution architecture](Solution-Architecture)
- [Project setup](Project-Setup)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Ingestion pipeline](Ingestion-Pipeline)
- [Ingestion rules](Ingestion-Rules)
- [CanonicalDocument and discovery taxonomy](CanonicalDocument-and-Discovery-Taxonomy)
- [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism)
- [File Share provider](FileShare-Provider)
