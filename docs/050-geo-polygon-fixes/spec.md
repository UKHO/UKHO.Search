# Work Package 050 — Geo Polygon Elasticsearch Serialization Fixes

**Target output path:** `docs/050-geo-polygon-fixes/spec.md`

**Version:** `v0.01`

## 1. Overview

### 1.1 Purpose
Fix Elasticsearch indexing failures for `geoPolygons` by ensuring the domain `GeoPolygon` model is serialized into a valid GeoJSON shape compatible with Elasticsearch `geo_shape` fields.

The immediate driver is the observed dead-lettering of `UpsertOperation` documents with errors such as `failed to parse field [geoPolygons] of type [geo_shape]`. Current dead-letter diagnostics show that `GeoPolygon` is being serialized as an internal CLR/domain object graph (`rings`, `longitude`, `latitude`) rather than as GeoJSON.

### 1.2 Scope
This work defines the required serialization and mapping behavior for `GeoPolygon` values when documents are sent to Elasticsearch.

In scope:
- map domain `GeoPolygon` values to valid GeoJSON for Elasticsearch `geo_shape`
- support documents containing zero, one, or many polygons
- define how multiple polygons are represented in the indexed payload
- keep the existing domain model (`GeoPolygon`, `GeoCoordinate`, `CanonicalDocument`) unless a minimal supporting DTO/serializer layer is needed
- use a `System.Text.Json`-compatible approach only
- avoid dependencies that require `Newtonsoft.Json`
- ensure indexed payloads are structurally valid for Elasticsearch

Out of scope:
- changing the domain meaning of `GeoPolygon`
- redesigning ingestion enrichment logic that extracts polygon data
- changing dead-letter schema further beyond what is already implemented
- adding third-party GeoJSON libraries that depend on `Newtonsoft.Json`
- advanced geometry correction such as self-intersection repair unless needed to satisfy Elasticsearch acceptance

### 1.3 Stakeholders
- developers working on ingestion and Elasticsearch indexing
- developers diagnosing geo-shape dead-letters
- testers validating file-share geo ingestion scenarios

### 1.4 Definitions
- **GeoJSON**: the standard JSON format for geographic data structures such as `Polygon` and `MultiPolygon`
- **geo_shape**: the Elasticsearch field type used to index spatial shapes
- **Domain polygon**: the in-memory `GeoPolygon` representation used within the application domain
- **Index payload polygon**: the GeoJSON representation sent to Elasticsearch during indexing
- **MultiPolygon**: a GeoJSON shape type used to represent multiple polygons in one field value

## 2. System context

### 2.1 Current state
The Elasticsearch index mapping defines `geoPolygons` as `geo_shape`.

The domain document currently stores geo data as `List<GeoPolygon>`, where each `GeoPolygon` contains `Rings`, and each ring contains `GeoCoordinate` values with `Longitude` and `Latitude` properties.

The dead-letter evidence shows that this domain object graph is currently being serialized directly into JSON when indexing, producing output such as:
- `geoPolygons`
- `rings`
- `longitude`
- `latitude`

This is not valid GeoJSON and is therefore rejected by Elasticsearch for a `geo_shape` field.

### 2.2 Proposed state
Before a document is sent to Elasticsearch, the geo polygon data shall be transformed from the domain model into valid GeoJSON.

The indexed representation shall be:
- omitted when no polygons are present
- a GeoJSON `Polygon` when exactly one polygon is present
- a GeoJSON `MultiPolygon` when multiple polygons are present

This gives Elasticsearch a single valid `geo_shape` value for the `geoPolygons` field while preserving support for multiple polygons at the domain level.

### 2.3 Assumptions
- Elasticsearch `geo_shape` support for GeoJSON is the target interoperability path
- the existing domain model remains the authoritative in-memory model
- multiple polygons should remain supported at the domain level
- `System.Text.Json` is the required JSON stack for this repository
- no `Newtonsoft.Json` dependency should be introduced for this work

### 2.4 Constraints
- the solution must not use `GeoJSON.Net` because it relies on `Newtonsoft.Json`
- the implementation must remain compatible with the repository's current serialization conventions
- the mapping must be explicit and testable rather than relying on accidental serializer behavior
- the solution should fit the current Onion Architecture boundaries

## 3. Component / service design (high level)

### 3.1 Components affected
Expected components affected:
- Elasticsearch bulk indexing payload generation
- `CanonicalDocument` serialization or index payload mapping path
- geo domain types and/or a dedicated GeoJSON projection layer
- tests covering indexing serialization

Likely code areas:
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/*`
- `src/UKHO.Search.Ingestion/Pipeline/Documents/*`
- `src/UKHO.Search/Geo/*`
- `test/UKHO.Search.Ingestion.Tests/Elastic/*`
- `test/UKHO.Search.Ingestion.Tests/Documents/*`

### 3.2 Data flows
1. Enrichment builds one or more domain `GeoPolygon` instances and adds them to `CanonicalDocument.GeoPolygons`.
2. The indexing path prepares a document payload for Elasticsearch.
3. The geo polygon mapping layer converts domain polygons into GeoJSON.
4. The resulting GeoJSON is serialized with `System.Text.Json`.
5. Elasticsearch receives a valid `geo_shape` payload for `geoPolygons`.

### 3.3 Key decisions
- The domain model shall remain separate from the Elasticsearch wire format.
- GeoJSON mapping shall be explicit rather than relying on direct serialization of `GeoPolygon`.
- A single polygon shall map to GeoJSON `Polygon`.
- Multiple polygons shall map to GeoJSON `MultiPolygon`.
- Zero polygons shall result in no geo-shape payload being emitted for the field.
- The implementation shall use `System.Text.Json`-compatible types or custom serialization.
- No `Newtonsoft.Json`-based GeoJSON library shall be introduced.

## 4. Functional requirements

### 4.1 GeoJSON serialization contract
- The indexing layer shall serialize geo polygon data for Elasticsearch as valid GeoJSON.
- The serialized JSON for a single polygon shall contain:
  - `type: "Polygon"`
  - `coordinates`
- The serialized JSON for multiple polygons shall contain:
  - `type: "MultiPolygon"`
  - `coordinates`
- Coordinate arrays shall be serialized in GeoJSON order as `[longitude, latitude]`.
- Ring nesting shall follow GeoJSON rules:
  - `Polygon.coordinates` = array of rings
  - `MultiPolygon.coordinates` = array of polygons, each polygon containing an array of rings

### 4.2 Single polygon behavior
- When `CanonicalDocument.GeoPolygons` contains exactly one polygon, the indexed `geoPolygons` field shall be emitted as a GeoJSON `Polygon`.
- The polygon's exterior ring shall be mapped to the first ring in the GeoJSON `coordinates` array.
- Any additional rings in the domain polygon shall be mapped as additional rings in the same GeoJSON polygon.

### 4.3 Multiple polygon behavior
- When `CanonicalDocument.GeoPolygons` contains more than one polygon, the indexed `geoPolygons` field shall be emitted as a GeoJSON `MultiPolygon`.
- Each domain `GeoPolygon` shall become one polygon entry in the GeoJSON `MultiPolygon.coordinates` array.
- Each polygon within the multipolygon shall preserve its ring structure.

### 4.4 Empty polygon behavior
- When `CanonicalDocument.GeoPolygons` is empty, the indexed payload shall not emit an invalid empty geo-shape object.
- The absence of polygons shall not cause the document to fail indexing.

### 4.5 Validation and geometry handling
- The existing domain validation for ring closure and minimum point count shall continue to apply.
- The mapping layer shall not change coordinate order semantically; it shall preserve `longitude` then `latitude`.
- The mapping layer should preserve all rings and coordinates as provided by the validated domain objects.
- If additional geometry validation is required specifically to satisfy Elasticsearch, it shall be added in the mapping/indexing path rather than by weakening the domain model.
- Consecutive duplicate coordinates should be considered for normalization only if Elasticsearch requires it; this work does not mandate geometry simplification unless required by observed failures beyond the current GeoJSON-shape mismatch.

### 4.6 Library and implementation constraints
- The solution shall not use `GeoJSON.Net`.
- The solution shall not introduce `Newtonsoft.Json` as a dependency for this work.
- The solution shall use either:
  1. dedicated internal GeoJSON DTOs serialized with `System.Text.Json`, or
  2. a custom `System.Text.Json` converter / serializer strategy,
  provided the result is valid GeoJSON for Elasticsearch.

## 5. Non-functional requirements
- The mapping should be deterministic and easy to inspect in tests and dead-letters.
- The implementation should minimize coupling between domain geometry types and Elasticsearch wire-format concerns.
- The solution should be maintainable and extensible for future geo-shape query requirements.
- The solution should avoid unnecessary new package dependencies.

## 6. Data model
The domain model remains:
- `CanonicalDocument.GeoPolygons : List<GeoPolygon>`
- `GeoPolygon.Rings : IReadOnlyList<IReadOnlyList<GeoCoordinate>>`
- `GeoCoordinate(Longitude, Latitude)`

The Elasticsearch wire format shall be modeled separately as GeoJSON-compatible output.

Recommended wire-format shape:
- for one polygon:
  - `type = "Polygon"`
  - `coordinates = number[][][]`
- for multiple polygons:
  - `type = "MultiPolygon"`
  - `coordinates = number[][][][]`

The `geoPolygons` field in the indexed document shall therefore hold one GeoJSON shape object, not the raw domain model.

## 7. Interfaces & integration
- The Elasticsearch bulk index client or an adjacent mapping layer shall be responsible for converting the domain geometry model into GeoJSON.
- The index mapping for `geoPolygons` shall remain `geo_shape`.
- The mapping layer shall integrate with the existing bulk indexing flow without requiring ingestion enrichers to know about GeoJSON.

## 8. Observability (logging/metrics/tracing)
- Existing dead-letter diagnostics shall continue to capture the indexed document payload shape.
- If geo-shape serialization fails before submission, logging should identify that the failure occurred during GeoJSON mapping or serialization.
- If Elasticsearch still rejects a geo-shape after this fix, dead-letter diagnostics should make the final GeoJSON payload inspectable.

## 9. Security & compliance
- No new external trust boundaries are introduced.
- No new JSON stack based on `Newtonsoft.Json` shall be introduced.
- The implementation should continue to treat geometry extracted from input files as untrusted data and rely on validation before indexing.

## 10. Testing strategy
- Add tests proving a single `GeoPolygon` serializes to a valid GeoJSON `Polygon` structure.
- Add tests proving multiple `GeoPolygon` values serialize to a valid GeoJSON `MultiPolygon` structure.
- Add tests proving coordinate order is `[longitude, latitude]`.
- Add tests proving empty `GeoPolygons` does not emit an invalid empty geo-shape payload.
- Add tests proving the Elasticsearch bulk payload contains the expected GeoJSON shape for `geoPolygons`.
- Add or update integration-style tests proving documents with geo polygons are accepted by the indexing path without falling back to the old CLR-object shape.

## 11. Rollout / migration
- Backward compatibility for previously indexed dev data is not required for this work item.
- A fresh index may be created for validation and development.
- No ingestion message schema changes are required.

## 12. Open questions
1. Resolved: do not use `GeoJSON.Net` because it depends on `Newtonsoft.Json`.
2. Resolved: use a `System.Text.Json`-compatible mapping approach.
3. Resolved: support multiple domain polygons by indexing them as a single GeoJSON `MultiPolygon`.
4. Open: if Elasticsearch still rejects some mapped shapes after GeoJSON conversion, should the next phase normalize consecutive duplicate points or other geometry quirks before indexing?
