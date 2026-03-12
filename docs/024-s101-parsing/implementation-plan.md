# Implementation Plan

- **Target output path**: `docs/024-s101-parsing/implementation-plan.md`
- **Related spec**: `docs/024-s101-parsing/s101-parsing-spec.md`

## Project / folder structure
- `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/`
  - `S100BatchContentHandler.cs`: add S-101 catalogue parsing and enrichment.
- `test/UKHO.Search.Ingestion.Tests/`
  - `TestData/s101-CATALOG.XML`: verbatim copy of `test/sample-data/s101-CATALOG.XML`.
  - `Enrichment/`: add tests validating enrichment behaviour.

Naming conventions
- Test class name: `S100BatchContentHandlerS101ParsingTests`.
- Test data file name: `s101-CATALOG.XML` (match casing and content exactly per spec).

## S-101 Catalogue Enrichment (FileShare ingestion)

- [x] Work Item 1: Add S-101 gating + keyword/search-text enrichment from `catalog.xml` - Completed
  - **Purpose**: Provide the first minimal but end-to-end runnable behaviour: when a FileShare batch includes a `catalog.xml` for S-101, the ingestion enrichment pipeline adds S-101 keywords and human-readable text into `CanonicalDocument`.
  - **Acceptance Criteria**:
    - Given a batch containing a `catalog.xml` matching the sample S-101 exchange catalogue, `CanonicalDocument.Keywords` includes `s-101` and `s101`.
    - `CanonicalDocument.SearchText` contains the producing organisation name and the exchange catalogue comment text.
    - If `XC:productSpecification/XC:name` is not `S-101` or `S101` (case-insensitive), no S-101 enrichment is applied (no keywords/search text added by this handler).
  - **Definition of Done**:
    - S-101 gating implemented in `S100BatchContentHandler`.
    - Org/comment parsing implemented using namespace-aware LINQ-to-XML.
    - Unit tests passing.
    - Warning-level logging added for malformed/unexpected XML (without failing the pipeline).
    - Can execute end-to-end via: `dotnet test .\test\UKHO.Search.Ingestion.Tests\UKHO.Search.Ingestion.Tests.csproj -c Release`
  - [x] Task 1: Add S-101 gating logic - Completed
    - [x] Step 1: Load `catalog.xml` via `XDocument.LoadAsync`. (Implemented in `S100BatchContentHandler`.)
    - [x] Step 2: Read `XC:productSpecification/XC:name` (root-level) with `XNamespace` set to `http://www.iho.int/s100/xc/5.2`.
    - [x] Step 3: Treat the catalogue as S-101 only if name equals `S-101` or `S101` (case-insensitive).
    - [x] Step 4: If not S-101, return early.
  - [x] Task 2: Parse and enrich organisation + comment + keywords - Completed
    - [x] Step 1: Extract organisation string from `XC:contact/XC:organization/gco:CharacterString`.
    - [x] Step 2: Extract comment from `XC:exchangeCatalogueComment/gco:CharacterString`.
    - [x] Step 3: Add `S-101` and `S101` tokens as keywords.
    - [x] Step 4: Add organisation + comment as search text.
  - [x] Task 3: Implement tests for Work Item 1 - Completed
    - [x] Step 1: Copied `test/sample-data/s101-CATALOG.XML` into `test/UKHO.Search.Ingestion.Tests/TestData/s101-CATALOG.XML` as a verbatim copy.
    - [x] Step 2: Added test covering S-101 enrichment in `S100BatchContentHandlerS101ParsingTests`.
    - [x] Step 3: Added test variant where the product name is changed to non S-101 and asserted no enrichment.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S100BatchContentHandler.cs`: add S-101 gating + keyword/search-text parsing.
    - `test/UKHO.Search.Ingestion.Tests/TestData/s101-CATALOG.XML`: add verbatim-copy file.
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/S100BatchContentHandlerS101ParsingTests.cs`: add unit tests.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet test .\test\UKHO.Search.Ingestion.Tests\UKHO.Search.Ingestion.Tests.csproj -c Release`
  - **User Instructions**:
    - Ensure the test data file is copied without any automatic formatting/normalization.

  - **Completed summary**:
    - Added S-101 gating and search enrichment to `S100BatchContentHandler` (keywords + organisation/comment search text), with warning logs for missing expected XML nodes.
    - Added test data as a content file copied to the test output directory.
    - Added unit tests validating positive and negative (non S-101) cases.

- [x] Work Item 2: Add geo polygon enrichment from `gml:posList` (EPSG:4326, lat-lon) - Completed
  - **Purpose**: Extend the end-to-end runnable capability: S-101 batches now contribute dataset coverage geometry to `CanonicalDocument.GeoPolygons` for spatial search.
  - **Acceptance Criteria**:
    - For the sample XML, at least one geo polygon is added to `CanonicalDocument.GeoPolygons`.
    - Coordinate pairs are treated as **latitude, longitude** and converted to `GeoCoordinate(longitude, latitude)`.
    - The handler tolerates malformed `posList` strings by skipping only the bad polygon(s) and still enriching other fields.
  - **Definition of Done**:
    - Geo polygon parsing implemented and covered by tests.
    - Existing tests from Work Item 1 still pass.
    - Can execute end-to-end via: `dotnet test .\test\UKHO.Search.Ingestion.Tests\UKHO.Search.Ingestion.Tests.csproj -c Release`
  - [x] Task 1: Parse `gml:posList` into `GeoPolygon` - Completed
    - [x] Step 1: Discover `gml:posList` nodes under `XC:dataCoverage/.../gml:posList`.
    - [x] Step 2: Split `posList` on whitespace; validate even token count and minimum of 4 coordinate pairs (ring length).
    - [x] Step 3: Parse each pair as `lat lon` (InvariantCulture) and convert to `GeoCoordinate.Create(lon, lat)`.
    - [x] Step 4: Ensure ring closure (first == last). Auto-close by appending the first coordinate if needed.
    - [x] Step 5: Construct `GeoPolygon` and add to `CanonicalDocument`.
  - [x] Task 2: Tests for geo polygon parsing - Completed
    - [x] Step 1: Added assertion `GeoPolygons.Count > 0` for the verbatim sample XML.
    - [x] Step 2: Added invalid `posList` variant test; asserts no throw, polygon skipped, keywords/search text still applied.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S100BatchContentHandler.cs`: implement geo parsing from `gml:posList`.
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/S100BatchContentHandlerS101ParsingTests.cs`: extend tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test .\test\UKHO.Search.Ingestion.Tests\UKHO.Search.Ingestion.Tests.csproj -c Release`

  - **Completed summary**:
    - Implemented geo polygon parsing from `gml:posList` (EPSG:4326, lat-lon) into `CanonicalDocument.GeoPolygons`.
    - Extended `S100BatchContentHandlerS101ParsingTests` to validate geo polygon creation and tolerance of invalid `posList`.

## Summary / key considerations
- Implement in two vertical slices so ingestion remains runnable and verifiable after each increment.
- Use namespace-aware parsing in all cases (XC/gco/gml/gex).
- Strictly gate parsing to S-101 only.
- Ensure geo parsing treats coordinates as **lat-lon** and CRS as **EPSG:4326 (WGS-84)** (sample uses `urn:ogc:def:crs:EPSG::4326`).
- Use a verbatim copy of `test/sample-data/s101-CATALOG.XML` for tests to avoid divergence from real sample data.
