# S-57 Parser (IS57Parser + BasicS57Parser)

> Target: `dev/work-packages/mvp/026-s57-parser/s57-parser.md`

## 1. Overview

### 1.1 Purpose

Specify a minimal S-57 parser capability that can:

- Open S-57 ENC data (ISO/IEC 8211)
- Extract textual dataset metadata suitable for search indexing
- Extract a coverage boundary polygon suitable for geo search/filtering

### 1.2 Scope

In scope:

- New `IS57Parser` abstraction (similar intent to `IS100Parser`) for use by enrichment handlers.
- `BasicS57Parser` implementation backed by GDAL/OGR (via `MaxRev.Gdal.Universal`).
- Extraction of:
  - Coverage boundary polygon as WKT envelope polygon
  - Textual metadata:
    - `DSID.DSID_COMT`
    - `DSID.DSPM_COMT`
    - Deduplicated values (e.g., when identical)

Out of scope:

- Full S-57 semantic interpretation (OBJL decoding, attribute catalogs).
- Geometry extraction for all features.

## 2. System context

### 2.1 Current state

There is a console spike (`TestS57Parser`) demonstrating how to open an S-57 dataset via GDAL/OGR and extract:

- Dataset envelope, producing an envelope polygon:
  `POLYGON((-79.2 33.375, -79.2 33.45, -79.125 33.45, -79.125 33.375, -79.2 33.375))`
- Textual metadata values observed in the sample:
  - `Produced by NOAA` (from both `DSID_COMT` and `DSPM_COMT`)

### 2.2 Proposed state

Introduce an S-57 parser that can be invoked by FileShare enrichment.

## 3. Component / service design (high level)

### 3.1 Package dependency

Add the following package reference to `UKHO.Search.Ingestion.Providers.FileShare`:

- `MaxRev.Gdal.Universal` `3.12.2.472`

Rationale:

- Provides managed bindings and native runtime components for GDAL, including the S-57 OGR driver.

### 3.2 Interfaces

#### 3.2.1 `IS57Parser`

A new interface similar to `IS100Parser`.

Design considerations:

- S-57 parsing likely needs:
  - the path to the `.000` file (primary entry point)
  - optionally, awareness of sibling `.nnn` files (open question: should we pass all files or just `.000` and rely on GDAL to discover siblings?)

Suggested shape (non-code):

- Method takes a file path (string) to the `.000` file.
- Returns a result object containing:
  - `CoveragePolygonWkt` (string)
  - `TextMetadata` (set/list of strings)

#### 3.2.2 Result contract

- Coverage polygon must be emitted in WKT as an envelope polygon.
- Text metadata values must be unique (deduplicated).

### 3.3 `BasicS57Parser` implementation

Implementation outline (non-code):

- Ensure GDAL is configured once per process:
  - `GdalBase.ConfigureAll()`
  - enable exceptions
- Open datasource using OGR:
  - `Ogr.Open(pathTo000, 0)`
- Coverage polygon:
  - compute dataset envelope as union of layer extents
  - create WKT polygon from envelope: `POLYGON((minX minY, minX maxY, maxX maxY, maxX minY, minX minY))`
- Textual metadata:
  - scan the `Meta` layer first (if present)
  - extract `DSID_COMT` and `DSPM_COMT` fields
  - deduplicate values

### 3.4 Mapping into `CanonicalDocument`

Functional intent:

- Text metadata values should be added to `CanonicalDocument` as searchable text.
- Coverage polygon should be added to `CanonicalDocument` as a geo polygon.

Design considerations / open points:

- Identify the target `CanonicalDocument` field(s) for:
  - search text augmentation (append vs replace)
  - geo polygon storage (field name, structure, coordinate reference expectations)

## 4. Functional requirements

- Can open `.000`-based S-57 dataset using GDAL.
- Produces coverage polygon WKT (envelope-based).
- Produces deduplicated text metadata values.
- Handles missing/empty metadata gracefully.

## 5. Non-functional requirements

- Performance: must handle typical ENC sizes without excessive memory usage.
- Resilience: invalid geometries/records should not crash enrichment; log and continue best-effort.
- Determinism: results should be stable.

## 8. Observability (logging/metrics/tracing)

- Log:
  - dataset path opened
  - extracted coverage envelope
  - extracted text metadata keys found/missing
- Avoid logging excessive per-feature details.

## 9. Security & compliance

- The parser must treat input files as untrusted.
- Ensure no path traversal issues when resolving dataset siblings.

## 10. Testing strategy

- Unit tests for:
  - envelope-to-WKT conversion
  - deduplication of extracted text values
- Integration test:
  - run parsing against `sample.000` fixture to assert:
    - polygon equals `POLYGON((-79.2 33.375, -79.2 33.45, -79.125 33.45, -79.125 33.375, -79.2 33.375))`
    - text includes a single `Produced by NOAA`

## 12. Open questions

1. Should `IS57Parser` accept a single `.000` path or a set of dataset member paths?
2. How should we store geo polygons in `CanonicalDocument` (existing geo model reference)?
3. Should we attempt a more accurate boundary than envelope (convex hull / union of areas) in a later increment?
