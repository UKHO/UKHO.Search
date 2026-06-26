# Work Package 026 - S-57 Parser (Overview)

> Target: `dev/work-packages/mvp/026-s57-parser/overview.md`

## 1. Overview

### 1.1 Purpose

Introduce basic support for parsing S-57 ENC datasets (ISO/IEC 8211, typically `*.000`, `*.001`, …) within the file-share ingestion pipeline.

The initial goal is to:

- Detect S-57 datasets in incoming batches.
- Extract a dataset coverage/boundary polygon.
- Extract human-readable textual metadata.
- Map extracted values into `CanonicalDocument` so they become searchable and can be used for geo queries.

### 1.2 Scope

In scope:

- File discovery changes in `S57BatchContentHandler` to treat an S-57 dataset as a collection of files sharing the same base name (e.g., `SAMPLE.000`, `SAMPLE.001`, …)
- A new `IS57Parser` abstraction and a basic GDAL-backed implementation (`BasicS57Parser`) in the FileShare provider.
- A new package dependency on `MaxRev.Gdal.Universal` to enable reading S-57 via GDAL/OGR.
- Extraction of:
  - coverage polygon (WKT) based on dataset extent / envelope
  - textual fields such as `DSID.DSID_COMT` / `DSID.DSPM_COMT` (deduplicated)

Out of scope (initial increment):

- Full S-57 object model mapping (OBJL codes, feature catalog enrichment)
- Converting all S-57 geometries into application geometry primitives
- Advanced polygon generation beyond envelope/extent (e.g., union of areas)

### 1.3 Stakeholders

- Search ingestion maintainers
- FileShare provider maintainers
- Consumers of geo search / bounding queries

## 2. System context

### 2.1 Current state

- The FileShare ingestion provider handles certain S-100 formats (e.g., S-101) and enriches `CanonicalDocument` with extracted searchable text and geo information.
- `S57BatchContentHandler` exists but currently does not perform discovery or extraction.

### 2.2 Proposed state

- Extend `S57BatchContentHandler` to detect S-57 datasets by locating `*.000` files and any related `*.00n`/`*.nnn` siblings that belong to the same dataset base name.
- Add an S-57 parsing path similar in intent to existing S-101 extraction, but scoped to envelope-based coverage and selected metadata fields.

### 2.3 Assumptions

- GDAL S-57 driver is available in the `MaxRev.Gdal.Universal` package.
- The primary dataset file extension is `.000`, with optional additional files `.001` … `.nnn`.
- The spatial reference will typically be WGS84 (`EPSG:4326`) for extracted extents.

### 2.4 Constraints

- Must not introduce code changes as part of this work package authoring step (spec-only).
- When implemented, changes must follow existing Onion architecture and repository coding standards.

## 3. Component / service design (high level)

### 3.1 Components

- `S57BatchContentHandler` (existing): extend detection to include S-57 dataset file groups and drive extraction into `CanonicalDocument`.
- `IS57Parser` (new): S-57 parsing abstraction.
- `BasicS57Parser` (new): GDAL/OGR-based implementation.

### 3.2 Data flows

1. Batch ingestion enumerates files.
2. S-57 datasets are identified by `baseName + .000` plus any sibling `baseName + .nnn`.
3. Parser opens the dataset using GDAL/OGR (S-57 driver).
4. Parser extracts:
   - coverage envelope min/max lon/lat
   - selected textual metadata strings
5. Enrichment writes:
   - search text into `CanonicalDocument`
   - coverage polygon into `CanonicalDocument` geo polygon field

### 3.3 Key decisions

- Coverage polygon generation approach (envelope polygon vs union/hull).
- Whether parser takes filename of `.000` only, or full set of dataset files.

## Related component specifications

- `dev/work-packages/mvp/026-s57-parser/s57-parser.md`
- `dev/work-packages/mvp/026-s57-parser/s57-dataset-detection.md`
