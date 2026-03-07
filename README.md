# UKHO Search Service

UKHO Search is a .NET-based service that provides **lexical** and **semantic** search capabilities, foremost for the **File Share Service** (FSS), but designed so it can be adapted to index and search other sources.

The primary search backend is **Elasticsearch**.

## This repository is an Aspire project

This repo is built as a **.NET Aspire** distributed application.

Aspire advantages for this solution:

- **Completely local development**: run the full stack (services + dependencies) on a dev machine.
- **Consistent dependency orchestration**: Elasticsearch, Keycloak, Azurite (Azure Storage emulator), SQL Server, etc. are started in a known-good configuration.
- **Single place to observe the system**: the Aspire dashboard shows resource health, logs, endpoints, and wiring.
- **Repeatable run modes**: import seed data locally, then run the services using the same AppHost.

## Repository structure (high level)

This solution follows an **Onion Architecture** approach:

- **Dependency direction points inward**: Hosts/UI depend on Infrastructure, which depends on Services, which depends on Domain.
- The **Domain** is the center: it contains core models and business rules and should not depend on infrastructure concerns (databases, Elasticsearch clients, HTTP, etc.).
- Outer layers provide implementations/adapters (e.g., Elasticsearch integrations) and are wired up by the Hosts.

- `src/Hosts/AppHost/` – Aspire **AppHost** that orchestrates the distributed application and its dependencies.
- `src/Hosts/IngestionServiceHost/` – host for the ingestion API/service (pulls from sources and indexes into Elasticsearch).
- `src/Hosts/QueryServiceHost/` – host for the query API/service (search endpoints backed by Elasticsearch).
- `src/UKHO.Search*/` – Domain and supporting libraries (pipeline/playground, query and ingestion domain).
- `src/UKHO.Search.Services*/` – service-layer projects.
- `src/UKHO.Search.Infrastructure*/` – infrastructure-layer projects (Elasticsearch integration, persistence, etc.).
- `tools/` – developer tooling for the File Share emulator and data images:
  - `tools/FileShareEmulator/` – local emulator for File Share.
  - `tools/FileShareImageLoader/` – imports a File Share “data image” into the local emulator/storage.
  - `tools/FileShareImageBuilder/` – builds a new File Share “data image”.
- `docs/` – design and operational documentation.

## Local setup

### Prerequisites

- Docker Desktop (or compatible Docker engine)
- .NET SDK (matching the repo targets)
- Azure CLI (`az`) with access to the UKHO ACR (`searchacr`)

### 1) Get the File Share Data Image from Azure Container Registry

The AppHost import workflow expects a local Docker image named `fss-data-<environment>:latest`.

For the default environment (`vnext-e2e`), pull and retag the image as follows:

```bash
az login
# When `az login` lists available subscriptions, select `AbzuUTL`.
az acr login --name searchacr

docker pull searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker tag searchacr.azurecr.io/fss-data-vnext-e2e:latest fss-data-vnext-e2e:latest
docker rmi searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

If you change `Parameters:environment` in the AppHost configuration, update the image name accordingly.

### 2) Set the AppHost RunMode to `Import` and run the FileShareImporter

1. Open `src/Hosts/AppHost/appsettings.json`.
2. Set:
   - `Parameters:environment` to the environment you have an image for (for example `vnext-e2e`).
   - `Parameters:runmode` to `import`.
3. Run the AppHost:

```bash
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

4. In the Aspire dashboard, start the **explicit start** resource `tools-fileshare-loader` (the File Share importer).
   - This will seed the local named volume (if needed) and import the File Share data image into the local emulator/storage.

### 3) Set the AppHost RunMode to `Services` and run

1. Update `src/Hosts/AppHost/appsettings.json` and set `Parameters:runmode` to `services`.
2. Run the AppHost again:

```bash
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

This starts the full local stack (including Elasticsearch) and exposes service endpoints through Aspire.

## Building a new File Share Data Image

To build a new File Share data image, see `tools/FileShareImageBuilder/README.md`.
