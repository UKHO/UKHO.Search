# Specification: Geo ingestion support in `CanonicalDocument`

**Work package:** `dev/work-packages/mvp/023-geo-ingestion/`

## 1. Overview

### 1.1 Purpose
This specification defines the changes required to add *optional* geographic coverage data to the ingestion model by extending `CanonicalDocument` to store one or more geo polygons suitable for indexing into Elasticsearch using the `geo_shape` field type.

This work package covers the data model, serialization/indexing considerations, and the automated test coverage needed to ensure the new capability is robust.

### 1.2 Goals
- Add the capability to store **zero or more** geographic polygons against a `CanonicalDocument`.
- Ensure the data can be mapped to Elasticsearch as a `geo_shape` polygon.
- Ensure the capability is fully tested (unit tests and any relevant integration tests).

### 1.3 Non-goals / Out of scope
- **Extracting geo data from a batch ZIP** (or from any batch artifact) is out of scope. A future work package/spec will define how ingestion providers parse and populate the geo polygons.
- Defining geo search/query features in the API/UI is out of scope.
- Guaranteeing polygon validity beyond minimal structural validation (e.g., ring closure) is out of scope unless already required by existing ingestion/indexing constraints.

### 1.4 Background / Context
Some ingested batch types contain one or more geographic polygons describing the spatial coverage of their contents. This geometry must be persisted in the `CanonicalDocument` so it can later be indexed and queried spatially.

Elasticsearch field type reference: `geo_shape`\
https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/geo-shape

### 1.5 Stakeholders
- Search/Ingestion engineering team
- Search platform consumers (downstream services that rely on indexed documents)

## 2. Functional requirements

### 2.1 Store geo polygons on `CanonicalDocument`
- The system shall allow `CanonicalDocument` to optionally contain **one or more** geo polygons.
- The system shall allow `CanonicalDocument` to contain **zero** polygons (default/typical case for batches without geo coverage).
- The system shall support **multiple polygons** per document.

### 2.2 Polygon representation
- The stored polygon representation shall be compatible with Elasticsearch `geo_shape` indexing.
- The representation shall support polygons with:
  - an exterior ring, and
  - optional interior rings (holes).

### 2.3 Backwards compatibility
- Existing ingestion and indexing flows that do not provide geo polygons shall continue to work without change.
- Existing serialized documents (if persisted) shall remain readable.

## 3. Technical requirements

### 3.1 Data contract / Domain model
#### 3.1.1 New domain type(s)
Introduce one public domain type to represent geo polygons in a way that is:
- easy to validate,
- easy to serialize, and
- independent of third-party geo libraries.

All new geo domain types introduced by this work (e.g., `GeoPolygon`, `GeoCoordinate`) shall be created in the `UKHO.Search` project (Domain layer) under a suitable geo-related namespace (e.g., `UKHO.Search.Domain.Geo` or equivalent consistent with existing namespace conventions in the repository).

Recommended approach:
- Add a new domain model type such as `GeoPolygon` (or similarly named) containing:
  - `IReadOnlyList<IReadOnlyList<GeoCoordinate>> Rings` (first ring = exterior; subsequent rings = holes)
- Add a supporting value type such as `GeoCoordinate` containing:
  - `double Longitude`
  - `double Latitude`

If the codebase already contains geo primitives, reuse them. Do not introduce a new geo library unless required.

#### 3.1.2 `CanonicalDocument` changes
- Add a property to `CanonicalDocument` to store geo polygons, e.g.:
  - `IReadOnlyCollection<GeoPolygon> GeoPolygons { get; }` or nullable equivalent.
- Ensure the property is optional:
  - default state is empty (preferred) or `null`.

#### 3.1.3 Validation rules (minimum)
At minimum, validation should ensure:
- Latitude is within `[-90, 90]`
- Longitude is within `[-180, 180]`
- Each ring contains at least 4 coordinates (polygon ring must be closed, and needs at least 3 distinct points)
- Each ring is closed (first coordinate equals last coordinate)

Notes:
- More advanced geometry validation (self-intersections, winding order) is not required unless Elasticsearch rejects it during indexing or existing repository conventions require it.

### 3.2 Elasticsearch index mapping
- Extend the relevant index mapping to include a new field for geo polygons.
- Field type shall be `geo_shape`.

Proposed mapping snippet (illustrative):

```json
{
  "properties": {
    "geoPolygons": {
      "type": "geo_shape"
    }
  }
}
```

Notes:
- Elasticsearch `geo_shape` can accept GeoJSON-like structures. Ensure the serialization strategy aligns with the Elasticsearch client usage in this repository.
- Confirm whether the field should be:
  - a single `geo_shape` containing multiple polygons (e.g., `MultiPolygon`), or
  - an array of `geo_shape` polygons.

This decision should be based on:
- how documents are currently serialized to Elasticsearch
- how queries are expected to be authored downstream

Recommendation for this work package:
- Store **multiple polygons** at the domain level.
- Serialize as either `MultiPolygon` or an array depending on the existing indexing model (to be determined during implementation).

### 3.3 Serialization / indexing
- Ensure geo polygons are included in the payload sent to Elasticsearch when a `CanonicalDocument` is indexed.
- Ensure (de)serialization works with existing JSON settings (e.g., `System.Text.Json` converters).

### 3.4 Testing
The geo ingestion capability must be fully tested.

#### 3.4.1 Unit tests
Add unit tests to cover:
- `CanonicalDocument` default state contains no geo polygons.
- Adding a single polygon results in it being present and retrievable.
- Adding multiple polygons is supported.
- Validation:
  - rejects invalid latitude/longitude
  - rejects rings that are not closed
  - rejects rings with fewer than 4 points

#### 3.4.2 Serialization/indexing tests
Add tests to confirm:
- The geo polygons serialize into the expected JSON shape for Elasticsearch `geo_shape`.
- Documents without geo polygons do not emit an invalid geo field (e.g., avoid emitting malformed empty objects).

Depending on repository patterns, these may be:
- unit tests around a mapper/serializer, or
- integration tests around the indexing component.

#### 3.4.3 Regression coverage
- Ensure existing tests continue to pass.
- Add tests to cover backward-compat behavior (e.g., reading older documents without the geo field).

## 4. Implementation notes (guidance)

### 4.1 Where to implement
Given the repository’s Onion Architecture:
- The `CanonicalDocument` type is expected to be part of the Domain layer.
- Elasticsearch mapping/indexing is expected to be in Infrastructure.

### 4.2 Versioning and migrations
If an index template or mapping is versioned:
- increment the template/mapping version
- ensure deployment/rollout steps are compatible with existing indices (reindexing may be required depending on strategy)

### 4.3 Security and data governance considerations
- Geo polygons may reveal sensitive or restricted coverage areas depending on the dataset.
- Ensure no additional PII is introduced by this change.

## 5. Acceptance criteria
- `CanonicalDocument` can store zero, one, or many geo polygons.
- Geo polygons validate basic coordinate bounds and ring closure.
- Index mapping includes a `geo_shape` field.
- Geo polygons are serialized/indexed correctly.
- Automated tests exist and pass, covering:
  - model behavior
  - validation
  - serialization/indexing

