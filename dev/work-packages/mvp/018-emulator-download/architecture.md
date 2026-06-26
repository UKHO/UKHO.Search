# Architecture

**Target output path:** `dev/work-packages/mvp/018-emulator-download/architecture.md`

## Overall Technical Approach

- Add a new Blazor page hosted within the existing `FileShareEmulator` project.
- Implement the core blob-to-disk download logic in a dedicated emulator service to keep UI code thin.
- Reuse existing blob container selection and blob naming conventions already present in the emulator’s minimal API (`BatchFilesApi`).
- Use configuration (`IConfiguration`) for the local download destination (`DownloadPath`).
- Use dependency injection for `BlobServiceClient` and service wiring.

### Data flow

```mermaid
flowchart LR
  UI[Blazor Downloads page] -->|BatchId| Svc[BatchDownloadService]
  Svc -->|container + blob name| Blob[Azure Blob Storage]
  Svc -->|write stream| Disk[(Local filesystem: DownloadPath)]
  Svc -->|result (success/error)| UI
```

## Frontend

- **Project**: `tools/FileShareEmulator`
- **UI Framework**: Blazor (server interactive components) with Radzen components.

### Pages / components

- `tools/FileShareEmulator/Components/Pages/Downloads.razor`
  - Route: `/downloads`
  - Contains `BatchId` textbox, `Download` button, and a status/error area.

### Navigation

- `tools/FileShareEmulator/Components/Layout/NavMenu.razor`
  - Add a `Downloads` menu item pointing to `/downloads`.

## Backend

- **Project**: `tools/FileShareEmulator`

### Services

- `tools/FileShareEmulator/Services/BatchDownloadService.cs`
  - Responsibilities:
    - Resolve container name (from configuration)
    - Resolve blob name based on batch id using existing conventions
    - Download blob contents and write to `{DownloadPath}/{BatchId}.zip`
    - Map errors into user-friendly messages
    - Log failures via `ILogger`

### Existing integrations reused

- `BlobServiceClient` is already configured/registered in `tools/FileShareEmulator/Program.cs`.
- Blob naming/container selection logic should mirror `tools/FileShareEmulator/Api/BatchFilesApi.cs` to avoid divergence.

