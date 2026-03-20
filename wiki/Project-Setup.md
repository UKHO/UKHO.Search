# Project setup

This page explains how to get the local stack running with Aspire, how the data-image workflow works, and how `runmode=import`, `runmode=services`, and `runmode=export` fit together.

## Prerequisites

You need:

- .NET SDKs compatible with the repository (`.NET 10` and ` .NET 9` projects are present)
- Docker Desktop
- local container support for SQL Server, Azurite, Elasticsearch/Kibana, and Keycloak
- Azure CLI for pulling the shared File Share data image from ACR when you are not building your own image
- access to the `AbzuUTL` subscription when using the shared ACR workflow described in `docs/azureacr.md`

Optional but commonly useful:

- Visual Studio with Aspire support
- Elastic/Kibana familiarity for index inspection
- Azure Storage Explorer or similar tools for blobs/queues

## Local orchestration entry point

The local stack is started from:

- `src/Hosts/AppHost/AppHost.csproj`

Default parameters live in:

- `src/Hosts/AppHost/appsettings.json`

Important parameters:

| Parameter | Meaning | Current default |
|---|---|---|
| `environment` | environment label used for data-image naming and blob container naming | `vnext-e2e` |
| `azure-storage` | host path mounted into Azurite | `d:\file-share-emulator` |
| `runmode` | which AppHost workflow to start | `services` |
| `ingestionMode` | ingestion behavior for missing ZIPs | `bestEffort` |

## Understanding `runmode`

`AppHost` supports three run modes.

```mermaid
flowchart LR
    Start[Start AppHost] --> Mode{runmode}
    Mode -->|import| Import[Seed local emulator from data image]
    Mode -->|services| Services[Run local search stack]
    Mode -->|export| Export[Build a new data image]
```

### `runmode=services`

Starts the main developer stack:

- Azurite (queues, tables, blobs)
- SQL Server
- Keycloak
- Elasticsearch + Kibana
- `IngestionServiceHost`
- `QueryServiceHost`
- `FileShareEmulator`
- `RulesWorkbench`
- local configuration emulation that also loads repository rules from `rules/`

Use this mode for day-to-day development and debugging.

### `runmode=import`

Starts the data-image import workflow:

- Azurite
- SQL Server
- a one-shot data seeder container that copies the local Docker image contents into a named volume
- `FileShareImageLoader` as an explicit-start resource

Use this when you already have a local Docker image named `fss-data-<environment>` and want to seed the emulator database/blob content.

### `runmode=export`

Starts the advanced data-image build workflow:

- SQL Server
- `FileShareImageBuilder` as an explicit-start resource

Use this only when creating a new data image from a remote File Share environment. See [Tools (advanced): `FileShareImageBuilder`](Tools-Advanced-FileShareImageBuilder).

## Getting the shared data image from ACR

The repository includes `docs/azureacr.md` for pulling the shared image.

### Pull workflow

1. Sign in to Azure:
   - `az login`
2. Log in to the ACR:
   - `az acr login --name searchacr`
3. Select the `AbzuUTL` subscription if prompted.
4. Pull the shared image:
   - `docker pull searchacr.azurecr.io/fss-data-vnext-e2e:latest`
5. Retag it to the local image name expected by AppHost:
   - `docker tag searchacr.azurecr.io/fss-data-vnext-e2e:latest fss-data-vnext-e2e:latest`
6. Optionally remove the fully-qualified tag after retagging.

The key rule is that the local image name must match the AppHost convention:

- `fss-data-<environment>`

So if `environment` is `vnext-e2e`, the loader expects:

- `fss-data-vnext-e2e`

## Recommended local workflow

### First-time or refresh workflow

1. Pull the shared data image from ACR, or build one yourself.
2. Set `runmode` to `import`.
3. Start `AppHost`.
4. In the Aspire dashboard, explicitly start `FileShareLoader`.
5. Wait for the import to complete.
6. Stop the import-mode run.
7. Set `runmode` to `services`.
8. Start `AppHost` again.
9. Open `FileShareEmulator`, `RulesWorkbench`, and the Aspire dashboard.

For rule authoring and rule diagnostics after startup, use [Tools: `RulesWorkbench`](Tools-RulesWorkbench).

### Why import and services are separate

The separation keeps the expensive seeding workflow distinct from the normal dev loop:

- `import` prepares local SQL/blob state from the data image
- `services` runs the actual stack against that prepared state

## What import mode actually does

In `AppHost` import mode:

1. A named Docker volume is created/mounted for emulator data.
2. A data seeder container copies `/data` from the image into that volume if it is empty.
3. `FileShareImageLoader` reads `/data/<environment>.bacpac` and imports the metadata database.
4. `FileShareImageLoader` migrates the local metadata schema as needed.
5. `FileShareImageLoader` imports blob content into the blob container named after the `environment` value.

## Running the stack in services mode

With `runmode=services`:

1. Start `AppHost`.
2. Open the Aspire dashboard.
3. Confirm that `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, storage, SQL, and Elasticsearch are healthy.
4. Use `FileShareEmulator` to inspect statistics and queue batches for ingestion.
5. Watch Aspire metrics and logs while indexing occurs.

## Configuration behavior in local Aspire

### Repository rules

In local run mode, `AppHost` loads the repository `rules/` directory into the configuration emulator with the prefix `rules`.

That means the local workflow is:

- edit rule JSON under `rules/file-share/...`
- run the services stack
- consume those rules through the configuration emulator and runtime rule services

### External services

`configuration/external-services.json` maps local `FileShare` traffic back to `FileShareEmulator` when using the local environment profile.

### Ingestion mode

`IngestionServiceHost` reads the environment variable `ingestionmode` and converts it into an `IngestionModeOptions` singleton.

- `Strict` preserves fail-fast ZIP behavior
- `BestEffort` allows missing ZIPs to be skipped when the failure is specifically treated as "not found"

## Useful post-start checks

- `FileShareEmulator` home page shows metadata statistics.
- `FileShareEmulator` indexing page can submit batches, clear queues, and delete indexes.
- Aspire metrics show the custom ingestion meter described in `docs/metrics.md`.
- dead-letter blobs appear under the configured dead-letter container/prefix.

## Common pitfalls

### The loader cannot find the image

Check that the local Docker image name exactly matches `fss-data-<environment>`.

### You changed `environment`

Keep these in sync:

- AppHost `Parameters:environment`
- the local Docker image tag
- the blob container name created by the loader
- the `.bacpac` filename inside the image

### You skipped import mode

If SQL/blob storage has not been seeded, the emulator may start but have no meaningful data.

### You expected import mode to run automatically

`FileShareLoader` is configured with explicit start. Start it from the Aspire dashboard.

## Related pages

- [Tools: `FileShareImageLoader` and `FileShareEmulator`](Tools-FileShareImageLoader-and-FileShareEmulator)
- [Tools (advanced): `FileShareImageBuilder`](Tools-Advanced-FileShareImageBuilder)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Ingestion pipeline](Ingestion-Pipeline)
