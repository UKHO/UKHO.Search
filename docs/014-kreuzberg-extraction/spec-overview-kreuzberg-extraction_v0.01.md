# Specification: Kreuzberg Extraction (Overview)

Version: v0.01  
Status: Draft  
Work Package: `docs/014-kreuzberg-extraction/`

## 1. Purpose
Introduce a file-content extraction enrichment step for FileShare ingestion, enabling downstream search indexing to include extracted textual content from files supplied in FileShare batch ZIP payloads.

## 2. Scope
This work package covers:
- Adding/finishing a FileShare ingestion enricher that downloads a batch ZIP for an ingestion request and extracts text from contained files.
- Integrating the Kreuzberg library for text extraction.
- Ensuring temporary on-disk artifacts created during extraction are cleaned up reliably.

Out of scope (initially):
- Search ranking/highlighting behaviour.
- Any UI changes.
- Changes to FileShareEmulator payload formats.

## 3. High-level design
### Components
- `UKHO.Search.Ingestion.Providers.FileShare`
  - Implements provider-specific enrichment steps for FileShare ingestion.
  - Adds a file-content extraction step backed by Kreuzberg.

- FileShareEmulator (tooling)
  - Provides batch ZIP download endpoint used by the enricher.

### Component specifications
- `docs/014-kreuzberg-extraction/spec-fileshare-filecontentenricher_v0.01.md`
