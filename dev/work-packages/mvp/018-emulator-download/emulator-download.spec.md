# Emulator Download (FileShareEmulator) — Specification

**Target output path:** `dev/work-packages/mvp/018-emulator-download/emulator-download.spec.md`

## 1. Overview

This work package adds a new `Downloads` page to the `FileShareEmulator` Blazor application so users can download an ingestion batch ZIP from the emulator’s configured Azure Blob Storage.

The page will allow a user to enter a `BatchId` and click a `Download` button. The emulator will retrieve the corresponding batch ZIP from blob storage (using the existing blob naming rules already used by the emulator) and save it to a local folder configured by `DownloadPath` in `appSettings.json`.

User feedback will be provided when a download fails (e.g., missing batch, access denied, configuration issues).

## 2. Goals / Non-goals

### Goals

- Add a `Downloads` page (`downloads.razor`) to the emulator UI.
- Add navigation entry to access the page.
- Provide a `BatchId` input and a `Download` action.
- Download the batch ZIP from blob storage using the existing batch ZIP storage conventions.
- Save the ZIP to the local filesystem under a configured directory (`DownloadPath`), creating it if required.
- Display an error message in the UI if the batch cannot be downloaded.

### Non-goals

- No changes to how batches are generated/seeded into blob storage.
- No change to authentication/authorization beyond existing emulator defaults.
- No bulk/multi-batch downloads.

## 3. High-level design

### 3.1 UI additions (Blazor)

- Add new page component: `tools/FileShareEmulator/Components/Pages/Downloads.razor`
- Add navigation item in: `tools/FileShareEmulator/Components/Layout/NavMenu.razor`

The page will:

- Render a textbox for `BatchId`.
- Render a `Download` button.
- Render a status/error area.

### 3.2 Download behavior

On click:

1. Validate `BatchId` is not empty.
2. Determine the blob container name from configuration (same mechanism as existing batch-file download endpoint).
3. Determine the blob name/path for the batch ZIP by reusing the emulator’s established conventions.
4. Read `DownloadPath` from configuration.
5. Ensure the local directory exists (create if missing).
6. Download the blob into a local file named `{BatchId}.zip` under `DownloadPath`.
7. Report success (including the full local path) or an error.

### 3.3 Error handling

The UI should show a user-facing error when:

- `BatchId` is missing/whitespace.
- Blob container cannot be determined from configuration.
- Blob does not exist for the requested batch.
- Blob download fails due to permissions or transient storage errors.
- `DownloadPath` is missing/invalid, or the directory cannot be created.
- The local file cannot be written.

## 4. Configuration

### 4.1 New/required setting

- `DownloadPath` (string): local filesystem path where downloaded batch ZIPs will be stored.

### 4.2 Existing settings used

- Blob container naming uses existing configuration used elsewhere in `FileShareEmulator` (e.g., environment-based container).

## 5. Services / components impacted

- `FileShareEmulator` Blazor UI (new page + navigation update)
- Blob download implementation (new service recommended for separation)

## 6. Acceptance criteria

- A new navigation item labeled `Downloads` is visible in the emulator.
- The `Downloads` page has a `BatchId` input and a `Download` button.
- Clicking `Download` downloads the correct batch ZIP from blob storage using the existing blob naming convention.
- The ZIP is saved to `DownloadPath` and the directory is created if it does not exist.
- If the batch cannot be downloaded, the UI shows an error message (not a silent failure).

## 7. Technical notes / implementation considerations

- Prefer reusing the existing blob naming and container selection logic already present in the emulator (avoid divergence).
- Use `BlobServiceClient` via DI (already configured in `Program.cs`).
- Use async I/O for blob and filesystem operations.
- Avoid buffering the entire ZIP in memory when writing to disk.
- Ensure input normalization aligns with existing blob lookup behavior (blob names are case-sensitive).
