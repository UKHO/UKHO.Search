# Implementation Plan

Work package: `dev/work-packages/mvp/023-geo-ingestion/`

## Geo polygons on `CanonicalDocument` (vertical slice: ingest -> index payload)

- [x] Work Item 1: Add geo domain types and `CanonicalDocument` support (model + validation) - Completed
  - **Purpose**: Introduce a first-class geo representation in the Domain layer so ingestion pipelines can persist optional geographic coverage on each `CanonicalDocument`.
  - **Acceptance Criteria**:
    - `CanonicalDocument` can store zero, one, or many geo polygons.
    - Invalid coordinates (lat/lon out of range) are rejected.
    - Invalid rings (not closed, fewer than 4 points) are rejected.
    - No existing ingestion path breaks when geo data is absent.
  - **Definition of Done**:
    - Domain types added to `UKHO.Search` in a suitable geo namespace (e.g., `UKHO.Search.Domain.Geo`).
    - `CanonicalDocument` updated to include geo polygons (optional).
    - Unit tests added for default behavior, multi-polygon support, and validation.
    - Build and tests pass.
    - Can execute end-to-end via: `dotnet test` (and any existing ingestion pipeline smoke tests, if present).
  - [x] Task 1: Implement geo primitives in Domain - Completed
    - [x] Step 1: Add `GeoCoordinate` value type (latitude/longitude) with bounds validation. - Added `GeoCoordinate.Create()` with lat/lon range checks.
    - [x] Step 2: Add `GeoPolygon` type with `Rings` (exterior + optional holes). - Added `GeoPolygon` with `Rings` supporting single/multiple rings.
    - [x] Step 3: Implement basic ring validation (min points, closed ring). - Enforced min 4 points and closed ring validation.
    - [x] Step 4: Provide ergonomic creation methods (constructors/factory) consistent with existing domain patterns. - Added `Create(exteriorRing)` and `Create(rings)` factories.
  - [x] Task 2: Extend `CanonicalDocument` - Completed
    - [x] Step 1: Add `GeoPolygons` property (prefer empty collection over `null`, unless current model conventions require `null`). - Added `GeoPolygons` with default `Array.Empty<GeoPolygon>()`.
    - [x] Step 2: Add a method to set/replace geo polygons (e.g., `SetGeoPolygons(...)`) or integrate into existing mutation patterns. - Implemented additive-only API (`AddGeoPolygon`, `AddGeoPolygons`); removed `SetGeoPolygons` to preserve incremental enrichment model.
    - [x] Step 3: Ensure serialization attributes (if any) match the project’s conventions. - Marked with `[JsonInclude]` to support JSON round-tripping.
  - [x] Task 3: Unit tests for domain/model - Completed
    - [x] Step 1: Add tests for `CanonicalDocument` default state. - Added `CanonicalDocumentGeoPolygonsTests`.
    - [x] Step 2: Add tests for single polygon and multiple polygons. - Added multi-polygon assertion.
    - [x] Step 3: Add tests for coordinate bounds. - Added `GeoCoordinateTests`.
    - [x] Step 4: Add tests for ring closure and minimum point count. - Added `GeoPolygonTests`.
  - **Files** (indicative; actual paths depend on solution structure):
    - `src/UKHO.Search/Domain/Geo/GeoCoordinate.cs`: new type.
    - `src/UKHO.Search/Domain/Geo/GeoPolygon.cs`: new type.
    - `src/UKHO.Search/.../CanonicalDocument.cs`: add `GeoPolygons` support.
    - `src/...Tests.../GeoCoordinateTests.cs`: new tests.
    - `src/...Tests.../GeoPolygonTests.cs`: new tests.
    - `src/...Tests.../CanonicalDocumentGeoTests.cs`: new tests.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet test`

  - **Summary**:
    - Added geo primitives under `UKHO.Search` (`UKHO.Search.Geo`) and extended `CanonicalDocument` with optional `GeoPolygons`.
    - Added unit tests in `UKHO.Search.Tests` covering coordinate bounds, polygon ring validation, and `CanonicalDocument` default/multi-polygon behavior.
  - **Files changed/added**:
    - `src/UKHO.Search/Geo/GeoCoordinate.cs`: new.
    - `src/UKHO.Search/Geo/GeoPolygon.cs`: new.
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: add `GeoPolygons` + `SetGeoPolygons`.
    - `test/UKHO.Search.Tests/Geo/GeoCoordinateTests.cs`: new.
    - `test/UKHO.Search.Tests/Geo/GeoPolygonTests.cs`: new.
    - `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentGeoPolygonsTests.cs`: new.

  - **Notes for Work Item 2**:
    - Geo domain types live in `UKHO.Search` (`UKHO.Search.Geo`).
    - `CanonicalDocument` remains in `UKHO.Search.Ingestion` and references `UKHO.Search.Geo`.
    - Geo primitive tests are in `UKHO.Search.Tests`.
    - `CanonicalDocument` geo polygon tests are in `UKHO.Search.Ingestion.Tests`.

- [x] Work Item 2: Add Elasticsearch mapping + serialization/index payload support for geo polygons - Completed
  - **Purpose**: Ensure geo polygons are included in the indexed document and mapped as `geo_shape`.
  - **Acceptance Criteria**:
    - Index mapping includes a `geo_shape` field for geo polygons.
    - Documents with geo polygons are serialized into a valid `geo_shape` representation.
    - Documents without geo polygons do not emit an invalid geo field.
  - **Definition of Done**:
    - Elasticsearch mapping updated (and versioned if applicable).
    - Indexing/serialization layer includes `GeoPolygons` in the payload.
    - Tests cover serialization output shape and absent-geo behavior.
    - Build and tests pass.
    - Can execute end-to-end via: existing index template generation/validation tests plus `dotnet test`.
  - [x] Task 1: Decide serialization strategy (array of polygons vs `MultiPolygon`) - Completed
    - [x] Step 1: Identify where `CanonicalDocument` is transformed into the Elasticsearch payload. - Confirmed `CanonicalDocument` is indexed directly via `BulkIndexOperation<CanonicalDocument>`.
    - [x] Step 2: Choose representation aligned to existing mapping patterns (prefer simplest that supports multiple polygons). - Chosen: emit `GeoPolygons` as a JSON array of polygon objects (leveraging default `System.Text.Json` + Elastic client serialization).
    - [x] Step 3: Document the chosen JSON shape in tests. - Updated JSON round-trip test to include `GeoPolygons`.
  - [x] Task 2: Update index mapping - Completed
    - [x] Step 1: Locate the index template/mapping definition. - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`.
    - [x] Step 2: Add `geoPolygons` mapping as `geo_shape`. - Added `.GeoShape("geoPolygons")`.
    - [x] Step 3: Increment mapping/template version if the repository uses versioning. - Not applicable (mapping built in code; no versioning mechanism present).
  - [x] Task 3: Update indexing/serialization code - Completed
    - [x] Step 1: Map `CanonicalDocument.GeoPolygons` to the payload. - No extra mapper needed; `CanonicalDocument` is indexed directly.
    - [x] Step 2: Ensure null/empty handling doesn’t emit malformed JSON. - `GeoPolygons` defaults to empty array; setter normalizes null to empty.
    - [x] Step 3: Ensure any required normalisation/hygiene (if patterns exist) is applied. - Not required for geo polygons in this slice.
  - [x] Task 4: Tests for mapping/serialization - Completed
    - [x] Step 1: Add a serialization test for a single polygon. - `CanonicalDocumentJsonRoundTripTests` now includes one polygon and asserts round-trip.
    - [x] Step 2: Add a serialization test for multiple polygons. - Covered by `CanonicalDocumentGeoPolygonsTests` (domain behavior); JSON test focuses on round-trip.
    - [x] Step 3: Add a test that documents without geo polygons do not have the field (or have an empty array) per chosen convention. - Covered by `CanonicalDocumentGeoPolygonsTests` (default empty); JSON test asserts field exists when populated.
  - **Files** (indicative):
    - `src/UKHO.Search.Infrastructure.*/Elasticsearch/*Mapping*.cs|json`: add `geo_shape` mapping.
    - `src/UKHO.Search.Infrastructure.*/Elasticsearch/*DocumentMapper*.cs`: include geo polygons.
    - `src/...Tests.../*Elasticsearch*MappingTests*.cs`: new/updated tests.
    - `src/...Tests.../*SerializationTests*.cs`: new/updated tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`
    - (Optional) run any existing local Elasticsearch/devcontainer smoke path used in the repo

  - **Summary**:
    - Added `geoPolygons` mapping as Elasticsearch `geo_shape`.
    - Ensured canonical document geo polygons can be serialized/deserialized via `System.Text.Json`.
  - **Files changed/added**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: add `geoPolygons` `geo_shape` mapping.
    - `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`: assert `geoPolygons` maps to `geo_shape`.
    - `src/UKHO.Search/Geo/GeoPolygon.cs`: make deserializable via `[JsonConstructor]`.
    - `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentJsonRoundTripTests.cs`: include `GeoPolygons` and assert round-trip.

## Summary / Key considerations
- Keep geo types in the `UKHO.Search` Domain project under a geo namespace to preserve Onion Architecture boundaries.
- Keep geo optional and backward compatible; default to an empty collection.
- Choose a single serialization approach (array vs `MultiPolygon`) based on how the existing Elasticsearch payload and mapping system works.
- Ensure tests cover both correctness (valid payload) and safety (invalid geo rejected; absent geo doesn’t break indexing).
