# Architecture

Version: v0.01  
Status: Draft  
Work Package: `docs/014-kreuzberg-extraction/`

## Overall Technical Approach
- Extend the existing ingestion pipeline for the FileShare provider by implementing `FileContentEnricher`.
- `FileContentEnricher` is responsible for:
  - Downloading the batch ZIP from FileShareEmulator via `IFileShareReadOnlyClient`.
  - Persisting ZIP to a per-invocation temp workspace, extracting safely, then extracting text from allowlisted file types using Kreuzberg.
  - Appending extracted text into `CanonicalDocument.Content` via `CanonicalDocument.SetContent(...)`.
  - Adding document keywords from file names (without extension) via `CanonicalDocument.AddKeyword(...)`.
  - Guaranteeing cleanup of the temp ZIP and extracted contents via `try/finally`.

### Data flow (pipeline-level)
```mermaid
flowchart LR
  A[Queue message: IngestionRequest] --> B[FileShare processing graph]
  B --> C[ApplyEnrichmentNode]
  C --> D[FileContentEnricher]
  D -->|DownloadZipFileAsync(batchId)| E[FileShareEmulator / FileShare ReadOnly API]
  D --> F[Temp workspace: ZIP + extracted files]
  D -->|Extract text| G[Kreuzberg]
  D -->|SetContent + AddKeyword| H[CanonicalDocument]
  H --> I[IndexOperation]
  I --> J[Bulk indexing]

  D -->|throw on download/unzip failure| K[Dead-letter / retry path]
```

## Frontend
- No frontend changes are required for this work package.

## Backend
### Key projects/components
- `src/UKHO.Search.Ingestion.Providers.FileShare`
  - Owns `FileContentEnricher` implementation and any provider-specific extraction adapters.

- `src/UKHO.Search.Infrastructure.Ingestion`
  - Owns ingestion pipeline hosting and wiring, including provider registration (`AddIngestionServices` calls `AddFileShareProvider`).

### Configuration
- The allowlist is configured under the ingestion configuration namespace:
  - Key: `ingestion:fileContentExtractionAllowedExtensions`
  - Value: semicolon-delimited extension list (case-insensitive), e.g. `.pdf;.docx;.txt;.html`

### Error handling policy
- Download/unzip failures are terminal for enrichment: log an error and throw so the ingestion item is retried and/or dead-lettered by the existing pipeline failure handling.
- Per-file Kreuzberg extraction failures are non-terminal: log a warning and continue.

### Temporary storage and cleanup
- A unique temp workspace is created per enrichment invocation under `Path.GetTempPath()`.
- Cleanup of the ZIP file and extracted contents is guaranteed via `try/finally`.
- ZIP extraction includes zip-slip protection by verifying that entry destination paths remain under the extraction root.
