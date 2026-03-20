# UKHO Search Service

`UKHO.Search` is a modern search platform built around a strong architectural core: a provider-driven ingestion pipeline, a provider-independent canonical discovery model, and a local-first developer experience powered by .NET Aspire.

This repository demonstrates a very deliberate engineering approach:

- **clear architectural boundaries** through Onion Architecture
- **high-quality local orchestration** through Aspire
- **extensible ingestion** through provider abstractions
- **rich search indexing** through a canonical document model rather than source-specific schemas
- **serious developer tooling** through the File Share emulator, data-image workflow, and RulesWorkbench

The result is a solution that is not just "a search service", but a well-structured platform for taking messy upstream source data, enriching it in a disciplined way, and projecting it into a coherent search experience.

The primary search backend is **Elasticsearch**.

## Learn more in the wiki

This front page is intentionally short.

Detailed documentation for architecture, setup, tools, ingestion, rules, `CanonicalDocument`, the provider mechanism, and the File Share provider now lives in the project wiki:

- [GitHub wiki](https://github.com/UKHO/UKHO.Search/wiki)
- [Wiki home](https://github.com/UKHO/UKHO.Search/wiki/Home)

Recommended starting points:

- [Home](https://github.com/UKHO/UKHO.Search/wiki/Home)
- [Solution architecture](https://github.com/UKHO/UKHO.Search/wiki/Solution-Architecture)
- [Project setup](https://github.com/UKHO/UKHO.Search/wiki/Project-Setup)
- [Ingestion pipeline](https://github.com/UKHO/UKHO.Search/wiki/Ingestion-Pipeline)
- [Ingestion rules](https://github.com/UKHO/UKHO.Search/wiki/Ingestion-Rules)
- [Tools: RulesWorkbench](https://github.com/UKHO/UKHO.Search/wiki/Tools-RulesWorkbench)

## This repository is an Aspire project

This repo is built as a **.NET Aspire** distributed application.

Aspire advantages for this solution:

- **Completely local development**: run the full stack and its dependencies on a development machine.
- **Consistent orchestration**: Elasticsearch, Keycloak, Azurite, SQL Server, and the supporting tools come up in a known-good configuration.
- **Operational visibility by default**: the Aspire dashboard gives a single place to inspect resource health, logs, endpoints, and metrics.
- **Repeatable developer workflows**: import seeded File Share data, run the services, and iterate using the same AppHost-driven experience.
- **A strong platform story**: the same orchestration model supports the ingestion host, query host, emulator, RulesWorkbench, and data-image tooling as one coherent local system.
