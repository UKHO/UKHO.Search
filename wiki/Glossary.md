# Glossary

This glossary defines the repository terms that appear repeatedly across the wiki. Read it before the architecture pages if the project vocabulary is new, or return to it when a term is used in a more specific ingestion or Workbench context.

## Use this page with the main reading paths

- [Home](Home) for the repository start-here path
- [Solution architecture](Solution-Architecture) for the current repository map
- [Architecture walkthrough](Architecture-Walkthrough) for code-oriented runtime flows
- [Project setup](Project-Setup) for local environment setup
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
- Continue to [Ingestion pipeline](Ingestion-Pipeline) or [Workbench introduction](Workbench-Introduction) when you are ready to move into a specific subsystem.
