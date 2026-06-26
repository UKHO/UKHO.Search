# Specification: Developer Wiki for `UKHO.Search`

Version: `v0.01`  
Status: `Draft`  
Work Package: `dev/work-packages/mvp/051-wiki/`  
Based on: `dev/work-packages/mvp/026-s57-parser/spec-template_v1.1.md`

## 1. Overview

### 1.1 Purpose
Create a developer-oriented repository wiki that consolidates the fragmented design history in `dev/work-packages/mvp/`, explains the current codebase shape, and gives contributors a practical guide for running and operating the solution locally with Aspire.

### 1.2 Scope
This work package covers markdown-only deliverables:

- a new top-level `wiki/` directory
- a `Home.md` landing page
- linked deep-dive pages covering architecture, setup, tooling, ingestion, rules, canonical models, provider extensibility, and the File Share provider
- a source-map page that explains how the wiki draws from the historical markdown corpus under `dev/work-packages/mvp/`

Out of scope:

- production deployment changes
- source-code changes
- restructuring the existing historical work-package documents

### 1.3 Stakeholders
- developers onboarding to `UKHO.Search`
- maintainers of ingestion, query, and tooling projects
- engineers operating the solution locally with Aspire
- developers authoring ingestion rules and File Share emulator data images

### 1.4 Definitions
- **Aspire AppHost**: the orchestration entry point in `src/Hosts/AppHost`
- **Services mode**: Aspire run mode that starts the local search stack
- **Import mode**: Aspire run mode that seeds the local emulator database/blob content from a previously built Docker data image
- **Export mode**: Aspire run mode that starts the image-building workflow used to create a File Share data image
- **CanonicalDocument**: provider-independent ingestion output model used for indexing
- **Provider**: an implementation of the ingestion provider abstraction responsible for deserializing queue messages and owning a processing graph

## 2. System context

### 2.1 Current state
The repository already contains a large body of design history under `dev/work-packages/mvp/`, including work packages for onion architecture, ingestion pipeline design, canonical document evolution, rules DSL and storage, emulator capabilities, best-effort ingestion, dead-letter diagnostics, geo ingestion, S-57/S-101 parsing, and File Share tooling.

That information is valuable but dispersed across many specifications, plans, architecture notes, and supporting guides. A new developer currently has to reconstruct the system by reading many separate work-package documents and then correlating them with the code.

### 2.2 Proposed state
The repository will include a consolidated `wiki/` that presents the current system as a coherent developer guide while still acknowledging the historical work-package lineage in `dev/work-packages/mvp/`.

The wiki will:

- explain the current architecture in onion terms
- describe how to run the local stack in Aspire
- explain the `runmode` model (`services`, `import`, `export`)
- document the local data-image workflow end to end
- describe File Share tooling and current emulator capabilities
- explain the ingestion pipeline runtime, rules system, dead-lettering, and metrics
- describe the provider mechanism and the File Share provider implementation
- present `CanonicalDocument` as the provider-independent discovery contract

### 2.3 Assumptions
- markdown is the required output format
- repository-relative links are preferred
- developers will use the wiki together with the codebase, not as a replacement for reading code
- historical documents in `dev/work-packages/mvp/` remain the source corpus and are not replaced by this work

### 2.4 Constraints
- documentation only; no code or runtime behavior changes
- the wiki must remain aligned with the current implementation in the open workspace
- mermaid diagrams should be included where they materially improve comprehension, especially where earlier work packages introduced architecture diagrams
- the local-development guidance must emphasize Aspire and the current AppHost run-mode model
- the source corpus for the wiki is the full markdown tree under `dev/work-packages/mvp/`, plus top-level guidance such as `docs/metrics.md` and `wiki/Ingestion-Rules.md`

## 3. Component / service design (high level)

### 3.1 Documentation components
The wiki deliverables are:

1. `wiki/Home.md`
2. `wiki/Solution-Architecture.md`
3. `wiki/Project-Setup.md`
4. `wiki/Tools-FileShareImageLoader-and-FileShareEmulator.md`
5. `wiki/Tools-Advanced-FileShareImageBuilder.md`
6. `wiki/Ingestion-Pipeline.md`
7. `wiki/Ingestion-Rules.md`
8. `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
9. `wiki/Ingestion-Service-Provider-Mechanism.md`
10. `wiki/FileShare-Provider.md`
11. `wiki/Documentation-Source-Map.md`
12. `wiki/Tools-RulesWorkbench.md`

### 3.2 Data flows
The wiki itself documents these major flows:

- local developer startup via Aspire
- data-image acquisition/import/export
- ingestion queue message flow through validation, dispatch, enrichment, batching, indexing, acknowledgement, and dead-letter sinks
- rule authoring flow from repository rule files into local configuration/runtime consumption
- provider-specific enrichment flowing into a provider-agnostic `CanonicalDocument`

### 3.3 Key decisions
- Create a small number of durable wiki pages rather than copying every historical document.
- Use the historical work packages as evidence and source material, but explain the system in terms of the current implementation.
- Add a source-map page so future contributors can trace the wiki back to the underlying work packages.
- Keep the setup guidance centered on `AppHost`, `runmode`, Docker data images, Azurite, SQL Server, Elasticsearch, and the File Share emulator.

## 4. Functional requirements

1. The wiki shall provide a single landing page with navigable links to all deep-dive topics.
2. The wiki shall explain onion architecture and map the current projects into domain, services, infrastructure, hosts, configuration, test, and tooling concerns.
3. The wiki shall explain local setup, including Docker image acquisition from ACR and the distinction between `runmode=import`, `runmode=services`, and `runmode=export`.
4. The wiki shall document `FileShareImageLoader` and `FileShareEmulator`, including the practical features exposed by the emulator UI and API.
5. The wiki shall document `FileShareImageBuilder`, including required local configuration and the export sequence.
6. The wiki shall provide a full ingestion-pipeline overview covering channels, lanes, ordering, batching, retries, metrics, and dead-letter persistence.
7. The wiki shall explain how to author ingestion rules and how rule storage/runtime consumption works locally.
8. The wiki shall explain `CanonicalDocument` as the provider-independent discovery contract.
9. The wiki shall explain the ingestion provider mechanism and why it exists.
10. The wiki shall provide a detailed File Share provider page covering enrichers, ZIP handling, geo extraction, S-57/S-101 enrichment, and Kreuzberg content extraction.
11. The wiki shall provide a source-map page that traces the documentation lineage across the historical `dev/work-packages/mvp/` corpus.
12. The wiki shall provide a dedicated `RulesWorkbench` page covering the `Rules` and `Checker` pages only, and shall explicitly avoid documenting the changing `Evaluate` page.

## 5. Non-functional requirements
- The wiki should be written for developers, not end users.
- Each page should be independently readable but linked from `Home.md`.
- Page titles and filenames should be stable and descriptive.
- Mermaid diagrams should render cleanly in GitHub markdown viewers.
- Links should be repository-relative and avoid external dependencies except where the existing docs already rely on them.

## 6. Data model
Not applicable beyond markdown content and repository-relative navigation.

## 7. Interfaces & integration
The wiki integrates with the repository by linking to:

- source directories under `src/`, `tools/`, `configuration/`, and `test/`
- top-level operational docs in `docs/`
- work-package histories from `dev/work-packages/mvp/000-ingestion-model/` through `dev/work-packages/mvp/050-geo-polygon-fixes/`

## 8. Observability (logging/metrics/tracing)
The wiki must explain the existing observability model:

- Aspire dashboard metrics and health endpoints
- custom ingestion pipeline meter `UKHO.Search.Ingestion.Pipeline`
- dead-letter blob outputs and their diagnostic payloads
- structured ingestion logs and diagnostics sinks

## 9. Security & compliance
The wiki should:

- avoid embedding secrets
- treat `configuration.override.json` as local-only and non-committed
- describe ACR login and data-image retrieval without persisting credentials
- distinguish local emulator behavior from production/remote service behavior

## 10. Testing strategy
Documentation validation for this work item is manual and structural:

- verify all required wiki files exist
- verify `Home.md` links to all pages
- verify setup steps are consistent with the current `AppHost` and tool implementations
- verify diagrams and terminology are consistent with the current codebase

## 11. Rollout / migration
- Add the wiki pages directly to the repository.
- Keep historical work-package docs unchanged.
- Future updates should amend the wiki as the implementation evolves and add new source references to the source-map page.

## 12. Open questions
1. None for this draft; the requested page set and focus areas are sufficiently defined.

## Target output paths
- `dev/work-packages/mvp/051-wiki/spec-overview-developer-wiki_v0.01.md`
- `wiki/Home.md`
- `wiki/Solution-Architecture.md`
- `wiki/Project-Setup.md`
- `wiki/Tools-FileShareImageLoader-and-FileShareEmulator.md`
- `wiki/Tools-Advanced-FileShareImageBuilder.md`
- `wiki/Ingestion-Pipeline.md`
- `wiki/Ingestion-Rules.md`
- `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
- `wiki/Ingestion-Service-Provider-Mechanism.md`
- `wiki/FileShare-Provider.md`
- `wiki/Documentation-Source-Map.md`
- `wiki/Tools-RulesWorkbench.md`
