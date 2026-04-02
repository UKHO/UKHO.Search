# UKHO Search Service

> [!IMPORTANT]
> ## STOP - READ THE WIKI
> This `README.md` is intentionally only a front door.
>
> **If you want to understand this repository, you must go to the wiki.**
> The real documentation for architecture, setup, ingestion, rules, Workbench, tooling, and repository reading paths now lives there.
>
> - **Start here:** [Wiki home](https://github.com/UKHO/UKHO.Search/wiki/Home)
> - **Browse everything:** [GitHub wiki](https://github.com/UKHO/UKHO.Search/wiki)

`UKHO.Search` is a modern search platform built around a strong architectural core: a provider-driven ingestion pipeline, a provider-independent canonical discovery model, and a local-first developer experience powered by .NET Aspire.

This repository demonstrates a very deliberate engineering approach:

- **clear architectural boundaries** through Onion Architecture
- **high-quality local orchestration** through Aspire
- **extensible ingestion** through provider abstractions
- **rich search indexing** through a canonical document model rather than source-specific schemas
- **serious developer tooling** through the File Share emulator, data-image workflow, and RulesWorkbench

The result is a solution that is not just "a search service", but a well-structured platform for taking messy upstream source data, enriching it in a disciplined way, and projecting it into a coherent search experience.

The primary search backend is **Elasticsearch**.

## Do not stop at this README

This front page is intentionally short and deliberately incomplete.

If you are exploring this repository, debugging it, setting it up locally, or trying to make a change, go to the wiki before going deeper into the codebase.

Detailed documentation for architecture, setup, tools, ingestion, rules, `CanonicalDocument`, the provider mechanism, the File Share provider, and the Workbench now lives in the project wiki:

- [Wiki home](https://github.com/UKHO/UKHO.Search/wiki/Home)
- [GitHub wiki](https://github.com/UKHO/UKHO.Search/wiki)

## Recommended wiki reading routes

### Start here first

- [Home](https://github.com/UKHO/UKHO.Search/wiki/Home)
- [Glossary](https://github.com/UKHO/UKHO.Search/wiki/Glossary)
- [Solution architecture](https://github.com/UKHO/UKHO.Search/wiki/Solution-Architecture)

### Then follow the route that matches your task

- **Local setup:** [Project setup](https://github.com/UKHO/UKHO.Search/wiki/Project-Setup) -> [Setup walkthrough](https://github.com/UKHO/UKHO.Search/wiki/Setup-Walkthrough) -> [Setup troubleshooting](https://github.com/UKHO/UKHO.Search/wiki/Setup-Troubleshooting)
- **Architecture:** [Solution architecture](https://github.com/UKHO/UKHO.Search/wiki/Solution-Architecture) -> [Architecture walkthrough](https://github.com/UKHO/UKHO.Search/wiki/Architecture-Walkthrough)
- **Ingestion:** [Ingestion pipeline](https://github.com/UKHO/UKHO.Search/wiki/Ingestion-Pipeline) -> [Ingestion walkthrough](https://github.com/UKHO/UKHO.Search/wiki/Ingestion-Walkthrough) -> [Ingestion rules](https://github.com/UKHO/UKHO.Search/wiki/Ingestion-Rules)
- **Workbench:** [Workbench introduction](https://github.com/UKHO/UKHO.Search/wiki/Workbench-Introduction) -> [Workbench architecture](https://github.com/UKHO/UKHO.Search/wiki/Workbench-Architecture)
- **Tooling:** [Tools: `RulesWorkbench`](https://github.com/UKHO/UKHO.Search/wiki/Tools-RulesWorkbench) -> [Tools: `FileShareImageLoader` and `FileShareEmulator`](https://github.com/UKHO/UKHO.Search/wiki/Tools-FileShareImageLoader-and-FileShareEmulator)

## Quick instruction for new readers

If you read only one thing after opening this repository, read the [wiki home page](https://github.com/UKHO/UKHO.Search/wiki/Home). It is the curated entry point for the rest of the documentation.

## This repository is an Aspire project

This repo is built as a **.NET Aspire** distributed application.

Aspire advantages for this solution:

- **Completely local development**: run the full stack and its dependencies on a development machine.
- **Consistent orchestration**: Elasticsearch, Keycloak, Azurite, SQL Server, and the supporting tools come up in a known-good configuration.
- **Operational visibility by default**: the Aspire dashboard gives a single place to inspect resource health, logs, endpoints, and metrics.
- **Repeatable developer workflows**: import seeded File Share data, run the services, and iterate using the same AppHost-driven experience.
- **A strong platform story**: the same orchestration model supports the ingestion host, query host, emulator, RulesWorkbench, and data-image tooling as one coherent local system.
