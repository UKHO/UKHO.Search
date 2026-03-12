# Specification: S-101 Catalogue XML Parsing in `S100BatchContentHandler`

- **Work package**: `024-s101-parsing`
- **Document**: `docs/024-s101-parsing/s101-parsing-spec.md`
- **Target component**: `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S100BatchContentHandler.cs`

## 1. Overview
This work package adds S-101 specific parsing into the existing FileShare enrichment handler `S100BatchContentHandler`.

When a `catalog.xml` file is present in the extracted batch, the handler will load the XML, detect whether it is an S-101 Exchange Catalogue, and if so, enrich the `CanonicalDocument` with:

- Contact/organisation name as **search text**.
- Product specification tokens as **keywords**.
- Exchange catalogue comment as **search text**.
- Dataset coverage polygon(s) from `gml:posList` as **geo polygons**.

The implementation must:
- Use namespace-aware parsing (the sample uses multiple XML namespaces).
- Fail gracefully: parsing errors should not fail the entire enrichment.
- Only apply S-101 parsing when the catalogue’s `XC:productSpecification/XC:name` indicates S-101.

## 2. Scope
### In scope
- Update `S100BatchContentHandler` to parse `catalog.xml` with LINQ-to-XML (`XDocument`, `XNamespace`).
- Add S-101 specific enrichment to `CanonicalDocument`:
  1) `XC:contact/XC:organization/gco:CharacterString` → `CanonicalDocument.AddSearchText(...)`
  2) `XC:productSpecification/XC:name` (when S-101) → `CanonicalDocument.AddKeyword("S-101")` and `CanonicalDocument.AddKeyword("S101")`
  3) `XC:exchangeCatalogueComment/gco:CharacterString` → `CanonicalDocument.AddSearchText(...)`
  4) `XC:datasetDiscoveryMetadata/.../XC:dataCoverage/.../gml:posList` → convert to `GeoPolygon` and add via `CanonicalDocument.AddGeoPolygon(...)`
- Implement tests using the provided sample XML.

### Out of scope
- Supporting other S-1xx product specifications.
- Parsing all catalogue metadata (bounding boxes, producer codes, etc.).
- Index mapping changes.

## 3. High-level design
### 3.1 Catalogue detection / gating
The handler will load the first `catalog.xml` found (current behaviour already selects the first by sorted path).

After loading the `XDocument`, determine whether it is S-101:

- Locate `XC:productSpecification` under the root element.
- Read `XC:name`.
- If `XC:name` is not `S-101` or `S101` (case-insensitive), stop and do not parse further.

### 3.2 Namespace handling
The sample file root is `XC:S100_ExchangeCatalogue` and declares:
- `xmlns:XC="http://www.iho.int/s100/xc/5.2"`
- `xmlns:gco="http://standards.iso.org/iso/19115/-3/gco/1.0"`
- `xmlns:gml="http://www.opengis.net/gml/3.2"`
- `xmlns:gex="http://standards.iso.org/iso/19115/-3/gex/1.0"`

Parsing must use these URIs via `XNamespace` values.

### 3.3 Enrichment mapping
#### Organisation → Search text
From sample:

- Path: `XC:contact/XC:organization/gco:CharacterString`
- Example value: `Australian Hydrographic Office`

Expected enrichment:
- `CanonicalDocument.AddSearchText("Australian Hydrographic Office")`

#### Product spec name → Keywords
From sample:

- Path: `XC:productSpecification/XC:name`
- Example value: `S-101`

Expected enrichment:
- If `XC:name` matches `S-101` or `S101`:
  - Add keywords: `S-101` and `S101`

Note: `CanonicalDocument` normalizes tokens to lowercase; stored values will be `s-101` and `s101`.

#### Exchange catalogue comment → Search text
From sample:

- Path: `XC:exchangeCatalogueComment/gco:CharacterString`

Expected enrichment:
- `CanonicalDocument.AddSearchText(commentText)`

#### `gml:posList` → `GeoPolygon`
From sample (within `XC:datasetDiscoveryMetadata/XC:S100_DatasetDiscoveryMetadata/XC:dataCoverage/.../gml:posList`):

- Path (conceptual): `XC:dataCoverage/XC:boundingPolygon/gex:polygon/gml:Polygon/gml:exterior/gml:LinearRing/gml:posList`

The `gml:posList` text contains pairs of coordinates separated by whitespace.

Implementation approach:
- Split the `posList` string on whitespace.
- Validate that the token count is even.
- Convert token pairs to doubles.
- Coordinates are **definitively** encoded as **latitude, longitude** pairs in the `gml:posList` for this catalogue.
  - Geometry is encoded using **EPSG:4326 (WGS-84)** (as indicated by `gml:Polygon/@srsName` in the sample: `urn:ogc:def:crs:EPSG::4326`).
  - Convert each `lat lon` coordinate pair into `GeoCoordinate.Create(longitude, latitude)`.
- Create a `GeoPolygon` from the exterior ring (`GeoPolygon.Create(...)`).
- Add to `CanonicalDocument`.

Defensive behaviour:
- If parsing fails for a given `posList`, skip that polygon and continue.
- If the ring is not closed, the parser may optionally close it by appending the first coordinate at the end (or treat as invalid and skip). The chosen behaviour must be consistent and covered by tests.

## 4. Testing
### 4.1 Sample XML file
The ingestion tests **must** consume the existing sample XML at:
- `test/sample-data/s101-CATALOG.XML`

The sample file must be copied into the ingestion tests project under:
- `test/UKHO.Search.Ingestion.Tests/TestData/s101-CATALOG.XML`

**Verbatim copy requirement:** the copied test data file must be an **exact byte-for-byte copy** of `test/sample-data/s101-CATALOG.XML` (no truncation, no reformatting/indentation changes, no namespace edits, no element removal, no line-ending normalization changes).

Tests will load the file content and run the handler against an extracted file list containing a `catalog.xml` path.

### 4.2 Unit test cases
1. **Enriches document for S-101 catalogue**
   - Given sample XML
   - Expect:
     - `SearchText` contains `australian hydrographic office`
     - `SearchText` contains the comment text (normalized)
     - `Keywords` contains `s-101` and `s101`
     - `GeoPolygons.Count > 0`

2. **Does not enrich if product spec is not S-101**
   - Given modified catalogue where `XC:productSpecification/XC:name` is not S-101
   - Expect:
     - No S-101 keywords added
     - No catalogue-derived search text added
     - No geo polygons added

3. **Gracefully handles invalid `posList`**
   - Given catalogue with invalid `posList` token count or non-numeric tokens
   - Expect:
     - Handler does not throw
     - Other fields (org/comment/keywords) still enriched

## 5. Dependencies / external references
- LINQ to XML namespace usage patterns (Microsoft Learn):
  - https://learn.microsoft.com/dotnet/standard/linq/create-document-namespaces-csharp

- Sample data:
  - Source of truth: `test/sample-data/s101-CATALOG.XML` (tests must use a verbatim copy)

## 6. Technical decisions / risks
- **Coordinate order / CRS**: `gml:posList` is **lat lon** and geometry is **EPSG:4326 (WGS-84)**; convert to `GeoCoordinate(longitude, latitude)`.
- **Large polygons**: `posList` can be large; parsing should avoid excessive allocations; however, correctness is prioritised for this initial increment.
- **Partial failures**: Individual polygons may fail to parse; handler should continue and keep partial enrichments.
