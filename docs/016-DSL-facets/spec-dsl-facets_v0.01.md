# Specification: Facets via Ingestion Rules DSL (Disable Automatic Facet Population)

Version: v0.01  
Status: Draft  
Work Package: `docs/016-DSL-facets/`

## 1. Purpose
Ensure that `CanonicalDocument.Facets` are populated only via the ingestion rules DSL (business rules), rather than automatically deriving facets from every ingestion request property.

This avoids unintentionally creating facet fields for all inbound properties and makes facet behaviour explicit and auditable in `ingestion-rules.json`.

## 2. Scope
### In scope
- Update file-share provider enrichment so it no longer automatically adds facet values for every `IngestionProperty`.
- Update unit tests for the modified enrichment behaviour.
- Add sample `facets.add` actions to each existing file-share business unit rule in `src/Hosts/IngestionServiceHost/ingestion-rules.json`.

### Out of scope
- Final decision on the long-term allowed list of facet keys.
- Query-side facet UI behaviour and aggregation definitions.
- Any change to Elasticsearch mappings (this work assumes `facets.*` is mapped as `keyword`).

## 3. High-level design
### 3.1 Facet population source of truth
- The ingestion rules DSL (`ingestion-rules.json`) is the source of truth for which facet keys are written and which values are attached.

### 3.2 Provider behaviour
- The file-share provider `BasicEnricher` continues to contribute to `CanonicalDocument.Keywords`.
- `BasicEnricher` must not add values to `CanonicalDocument.Facets`.

### 3.3 DSL facet values from request properties
Facet values in rules should be derived from ingestion request properties using `$path:` templates.

For this work package, add sample facet actions using:
- `$path:properties["businessUnitName"]`
- `$path:properties["agency"]`
- `$path:properties["productCode"]`
- `$path:properties["source"]`

Notes:
- If a property does not exist at runtime, `$path:` resolves to an empty set and no facet value is added.
- Facet names and values are normalized by `CanonicalDocument` to lowercase.

## 4. Functional requirements
### FR-1: Disable automatic facet creation
- `BasicEnricher` MUST NOT call `CanonicalDocument.AddFacetValue`.
- Facets MUST be empty after `BasicEnricher` runs unless populated by another enricher (e.g., rules engine).

### FR-2: Sample facet actions present in business unit rules
- Each file-share business unit rule MUST include a `then.facets.add` section that attempts to add the four sample facets listed in §3.3.

## 5. Non-functional requirements
- Changes must preserve existing ingestion pipeline behaviour except for facet population.
- Unit tests must be updated to reflect new enrichment behaviour.

## 6. Acceptance criteria
- `BasicEnricher` no longer adds any facet values.
- `test/UKHO.Search.Ingestion.Tests/Enrichment/BasicEnricherTests.cs` passes with updated assertions.
- `src/Hosts/IngestionServiceHost/ingestion-rules.json` contains `facets.add` blocks in every file-share business unit rule.

## 7. Testing strategy
- Unit tests:
  - Update `BasicEnricherTests` to assert `CanonicalDocument.Facets` is empty after enrichment.
- Manual (optional):
  - Run ingestion end-to-end and confirm facet values appear only when rules are configured to add them.

## 8. Implementation notes
Target files:
- `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BasicEnricher.cs`
- `test/UKHO.Search.Ingestion.Tests/Enrichment/BasicEnricherTests.cs`
- `src/Hosts/IngestionServiceHost/ingestion-rules.json`
