# 006 Ingestion Service — Local Runbook

This runbook describes how to run the ingestion pipeline locally (queue → dispatch → microbatch → Elasticsearch bulk index), and how to inspect dead-letter output.

## Prerequisites

- .NET SDK (per repo)
- Local dependencies via the repo’s default stack (typically the Aspire `AppHost`)
  - Elasticsearch
  - Azurite (Queues + Blobs)

## Configuration

Dead-letter settings (see `configuration/configuration.json` → `ingestion`):

- `ingestion:deadletterContainer` (default: `ingestion-deadletter`)
- `ingestion:deadletterBlobPrefix` (default: `deadletter`)
- Optional: `ingestion:deadletterFatalIfCannotPersist` (default: `true`)

## Run locally

1. Start the local stack (Aspire):

   - `dotnet run --project src/Hosts/AppHost`

2. Start the ingestion host:

   - `dotnet run --project src/Hosts/IngestionServiceHost`

3. Enqueue an ingestion message onto the provider queue (e.g., `ingestion:filesharequeuename`).

## Inspect indexing

- Verify documents in Elasticsearch index `ingestion:indexname`.

## Inspect dead-letter

Dead-letter items are persisted to Blob Storage as JSON:

- Container: `ingestion:deadletterContainer`
- Blob path pattern: `<deadletterBlobPrefix>/yyyy/MM/dd/<Key>/<MessageId>.json`

To locate a specific item:

- Find the date folder for the run
- Navigate to the document key folder
- Open the JSON blob and inspect `Envelope.Status` / `Envelope.Error`

## Diagnostics

The pipeline emits structured diagnostics logs after dispatch and after successful bulk indexing.

- Filter logs by node name prefix `ingestion-` and look for `ingestion-diagnostics` entries.
