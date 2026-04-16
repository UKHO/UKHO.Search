# Glossary

This glossary defines the repository terms that appear repeatedly across the wiki. Read it before the architecture pages if the project vocabulary is new, or return to it when a term is used in a more specific ingestion or Workbench context.

## Use this page with the main reading paths

- [Home](Home) for the repository start-here path
- [Solution architecture](Solution-Architecture) for the current repository map
- [Architecture walkthrough](Architecture-Walkthrough) for code-oriented runtime flows
- [Project setup](Project-Setup) for local environment setup
- [Setup walkthrough](Setup-Walkthrough) for the practical local verification path
- [Query pipeline](Query-Pipeline) for the query-side conceptual reading path
- [Query walkthrough](Query-Walkthrough) for the code-oriented query runtime trace
- [Query signal extraction rules](Query-Signal-Extraction-Rules) for the deep-dive query rules guide
- [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping) for the contract and request-mapping explanation
- [Ingestion pipeline](Ingestion-Pipeline) for message-processing detail
- [Workbench introduction](Workbench-Introduction) for the current Workbench guide

## Repository and runtime terms

### AppHost

The Aspire-based host at `src/Hosts/AppHost` that starts and coordinates the local developer environment, including storage, search, authentication, service hosts, and tool processes. See [Project setup](Project-Setup).

### Aspire dashboard

The local Aspire UI used to inspect resources, parameters, logs, and links for the AppHost-managed environment.

### CanonicalDocument

The shared discovery contract produced by ingestion and indexed into Elasticsearch. Providers transform source-specific data into this provider-agnostic search model. See [CanonicalDocument and discovery taxonomy](CanonicalDocument-and-Discovery-Taxonomy).

### Canonical index projection

The Elasticsearch-facing representation derived from `CanonicalDocument`. Query and diagnostics features use this indexed form rather than reading provider payloads directly.

### ITypedQuerySignalExtractor

The inward query-side abstraction that hides Microsoft Recognizers behind a repository-owned contract. The query planner calls this abstraction after normalization so recognizer-derived temporal and numeric signals can be projected into the repository-owned extracted-signal model without leaking third-party object graphs into the rest of the solution.

### Host

An executable outer-layer project that wires together services, infrastructure, configuration, and user-facing or operational entry points. Examples include `AppHost`, `IngestionServiceHost`, `QueryServiceHost`, and `WorkbenchHost`.

### Onion Architecture

The repository architecture rule that keeps dependencies moving inward: `Hosts / UI -> Infrastructure -> Services -> Domain`. The inner layers define contracts and behaviour, while outer layers provide integrations and composition. See [Solution architecture](Solution-Architecture).

### Provider

A source-specific ingestion implementation that understands one upstream system and converts its messages into repository runtime operations. The current concrete provider is File Share.

### Provider model

The shared metadata, identity, and registration surface in `src/UKHO.Search.ProviderModel`. It is not the provider implementation itself; it is the common layer that hosts and tools use to understand available providers.

### Run mode

The AppHost execution mode that selects which local workflow to start, such as `import`, `services`, or `export`. See [Project setup](Project-Setup).

## Ingestion terms

### Dead-letter

The persisted failure path used when an ingestion request or index operation reaches a terminal failure outcome. In this repository, blob-backed dead-letter persistence captures diagnostics for later investigation.

### Enrichment

The stage where additional data or derived fields are added to a canonical upsert operation before it is indexed.

### Envelope

The runtime wrapper around an ingestion message. It carries the payload plus metadata such as key, timing, acknowledgement context, and provider-scoped values needed later in the pipeline.

### Index operation

The normalized runtime action produced after dispatch, such as upsert, delete, or ACL update. These operations become bulk indexing work for Elasticsearch.

### Ingestion request

The provider-facing runtime contract representing one message to be processed by ingestion.

### Lane

One ordered partition of the ingestion pipeline. Messages with the same key are routed to the same lane so that per-key ordering is preserved.

### Rule

A repository-managed mapping or enrichment rule evaluated during ingestion to derive canonical document fields without hard-coding every mapping in C#. See [Ingestion rules](Ingestion-Rules).

## Query terms

### Query plan

The repository-owned contract in `src/UKHO.Search.Query` that captures normalized input, typed extracted signals, canonical query intent, residual default contributions, execution directives, and diagnostics before Elasticsearch execution. The query plan is the boundary between query interpretation and query execution.

### Generated-plan baseline

The most recent repository-owned `QueryPlan` produced from the raw-query path and then projected back into the Monaco editor as formatted JSON. In the current Query UI workspace, this baseline is kept separately from the current editable Monaco contents so contributors can experiment with plan changes and still reset the editor back to the last raw-query-generated starting point.

### Edited-plan execution

The developer workflow where the Query UI validates the current Monaco JSON as a repository-owned `QueryPlan` and then asks the application service to execute that supplied plan directly without regenerating it from raw query text first. This term is useful because it distinguishes “run the current plan as written” from the raw-query path, which still begins with free-form text and planning.

### Normalized query input

The repository-owned input snapshot produced by the query normalizer before typed extraction and rule evaluation continue. In current runtime terms this is the `QueryInputSnapshot`, which preserves raw text, a normalized lowercase form, a cleaned text form, deterministic tokens, and the residual text surfaces later used by rule consumption and default matching.

### Residual defaults

The fallback default query contributions built from the residual query content that remains after rule evaluation has consumed any phrases or tokens it explicitly handled. In current behavior, residual tokens become exact-match `keywords` clauses, while residual cleaned text becomes analyzed matches against `searchText` and `content`.

### Canonical query model

The repository-owned model inside the `QueryPlan` that mirrors the discovery-facing half of the canonical index in query terms. It holds exact-match canonical intent such as `keywords`, `category`, `series`, and `majorVersion`, and it stays separate from execution-only directives such as filters, boosts, and sorts.

### Execution directives

The execution-time part of the query plan that changes how Elasticsearch should run the search without itself becoming canonical subject-matter intent. In current runtime terms this includes explicit filters, explicit boosts, and explicit sort directives carried on `QueryExecutionDirectives`.

### Query rule

A flat global search-interpretation rule loaded from `rules/query/*.json` into the `rules:query:*` configuration namespace. Query rules inspect normalized input and extracted query signals, can mutate canonical query intent, can emit concepts, sort hints, filters, and boosts, and can consume phrases or tokens so default matching only sees the true residual query text.

### Query signal extraction rule

The contributor-facing name for a query rule when the emphasis is on interpreting free-form user text into typed or canonical search meaning. In practice this repository stores those rules as flat JSON files under `rules/query/*.json` and loads them into the `rules:query:*` configuration namespace.

### Query rules catalog

The refresh-aware runtime service that loads, validates, caches, and exposes the current flat query-rule snapshot for query planning. It gives the planner one stable rules snapshot per request while still allowing configuration refresh to replace the cached snapshot for later requests.

### Matched rule diagnostics

The developer-facing diagnostics retained on `QueryPlanDiagnostics` that record which query rules matched and which filters, boosts, sorts, and rule-catalog timestamp shaped the final query plan. These diagnostics help contributors explain why the planner produced a particular request shape.

### Transformation trace

The compact staged explanation shown in the current Query UI insight column that walks a contributor from raw query input through normalization, extracted signals, planning diagnostics, and request execution. The transformation trace is not a second repository-owned contract; it is a host-side narrative projection built from existing repository-owned artifacts such as `QueryInputSnapshot`, `QueryExtractedSignals`, `QueryPlanDiagnostics`, and the enriched `QuerySearchResult`.

### Exact-match field

On the query side, a canonical field that is matched as a discrete term or numeric value rather than as analyzed free text. Fields such as `keywords`, `category`, `series`, `majorVersion`, and `minorVersion` are treated this way by the current query mapper and rule validator.

### Analyzed field

On the query side, a text field that is matched through analyzed search text rather than exact-term equality. In current runtime terms, `searchText` and `content` are the analyzed fields used by residual defaults and analyzed-text boosts.

### RulesWorkbench

The dedicated local tool for inspecting, evaluating, and checking ingestion rules. See [Tools: `RulesWorkbench`](Tools-RulesWorkbench).

## Workbench terms

### Command

The shared action abstraction used across Workbench explorer items, menus, toolbars, and hosted tool interactions.

### Contribution

A bounded piece of Workbench UI or behaviour supplied by the host or an active tool, such as a menu item, toolbar button, status element, or explorer item.

### Explorer item

A Workbench navigation item that represents a tool or activation target in the left-side explorer surface.

### Module

A loadable Workbench assembly, usually named `UKHO.Workbench.Modules.*`, that contributes tools and services through the bounded Workbench module contract.

### Output panel

The shell-owned Workbench surface that keeps a session-scoped history of startup, notification, and diagnostic entries.

### Singleton tool activation

The Workbench behaviour where reopening the same logical tool target focuses the existing tab instead of creating a duplicate tab.

### Tool

A host-owned or module-contributed Workbench capability that can be activated in the shell and participate in command, menu, toolbar, or output flows.

### ToolContext

The bounded runtime surface exposed to an active Workbench tool so it can request activation, publish shell contributions, update metadata, and raise notifications without taking ownership of the shell.

### Workbench

The repository's desktop-like Blazor Server shell and module composition model, hosted in `src/workbench/server/WorkbenchHost` with shared contracts and services in `UKHO.Workbench*` projects.

## Tooling and support terms

### FileShareEmulator

The local tool that emulates File Share behaviour for repository development, data inspection, and queueing workflows.

### FileShareImageBuilder

The advanced tool that builds a Docker data image from a remote File Share environment.

### FileShareImageLoader

The local tool that imports a prepared File Share data image into SQL and blob storage for local development.

### Keycloak

The authentication service started as part of the local AppHost environment for the repository's secured development workflows.

### Mermaid diagram

A text-based diagram block rendered by GitHub markdown viewers. This wiki uses Mermaid for repository maps, runtime flows, and reading-path diagrams.

## Where to go next

- Return to [Home](Home) for the full reading-path overview.
- Continue to [Solution architecture](Solution-Architecture) for the repository-wide structure.
- Continue to [Architecture walkthrough](Architecture-Walkthrough) if you want to trace the main code paths rather than just the project map.
- Continue to [Query pipeline](Query-Pipeline), [Ingestion pipeline](Ingestion-Pipeline), or [Workbench introduction](Workbench-Introduction) when you are ready to move into a specific subsystem.
