# Smoke Test Runbook: FileShare Kreuzberg Extraction

Version: v0.01  
Status: Draft  
Work Package: `docs/014-kreuzberg-extraction/`

## Purpose
Provide a repeatable manual procedure to validate the end-to-end behaviour of `FileContentEnricher` against FileShareEmulator content.

This smoke test validates:
- A batch ZIP can be downloaded from FileShareEmulator.
- ZIP entries with allowlisted extensions are processed.
- Extracted text is appended into `CanonicalDocument.Content`.
- File name keywords (without extension) are added.

## Prerequisites
- .NET SDK (repo targets `net10.0`).
- Local Azure Storage emulator (Azurite) available.
- FileShareEmulator running locally.
- Ingestion host running locally.

## Configuration
- Ensure `configuration/configuration.json` includes the allowlist setting under the active environment:
  - `ingestion:fileContentExtractionAllowedExtensions` (JSON key: `fileContentExtractionAllowedExtensions`)
  - Example: `.pdf;.docx;.txt;.html`

## Step 1: Start Azurite
Start Azurite using your standard local setup (Docker, VS, or installed binary).

You need Blob storage available because FileShareEmulator streams ZIP payloads from blob storage.

## Step 2: Start FileShareEmulator
Run:
- `dotnet run --project tools/FileShareEmulator/FileShareEmulator.csproj`

Environment:
- Set environment variable `environment` to your target container name (e.g., `local`).

## Step 3: Seed a batch ZIP into Blob Storage
FileShareEmulator expects the blob name format:
- Container: `<environment>` (value of the `environment` setting)
- Blob: `<batchId>/<batchId>.zip` (case-insensitive lookup supported)

### Recommended seeding approach (manual)
1. Pick a GUID batch id (example): `11111111-1111-1111-1111-111111111111`
2. Create a ZIP containing at least:
   - `a.txt` with some plain text
   - (optional) `b.pdf` or `c.docx` if you want to validate additional Kreuzberg extractors
3. Upload the ZIP to blob storage at:
   - `11111111-1111-1111-1111-111111111111/11111111-1111-1111-1111-111111111111.zip`

You can do this with Azure Storage Explorer or any blob upload tool.

## Step 4: Validate FileShareEmulator can serve the ZIP
Request:
- `GET http://<fileshare-emulator-host>/batch/<batchId>/files`

Expected:
- HTTP 200
- `Content-Type: application/zip` (or equivalent)
- Response body is the uploaded ZIP.

## Step 5: Run the ingestion host and trigger an ingestion request
Run:
- `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`

Trigger an ingestion request for the same `batchId`:
- For FileShare ingestion, the batch id is taken from `IngestionRequest.AddItem.Id` / `UpdateItem.Id`.

(Triggering mechanism depends on your local queue setup and existing ingestion tooling/scripts.)

## Expected results
For the ingested document:
- `CanonicalDocument.Content` contains extracted text (lowercased/normalized by `SetContent`).
- `CanonicalDocument.Keywords` includes file name tokens (without extension), e.g. `a`.

## Troubleshooting
- If no extraction occurs:
  - Confirm `ingestion:fileContentExtractionAllowedExtensions` is configured and non-empty.
  - Confirm the ZIP entry extensions match the allowlist.
- If ingestion fails early:
  - Download/unzip failures are terminal by design (they throw). Check ingestion host logs for the batch id.
- If some files are skipped:
  - Per-file Kreuzberg extraction failures are best-effort and logged as warnings.
