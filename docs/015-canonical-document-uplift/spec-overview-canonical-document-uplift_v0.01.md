# Work Package: 015-canonical-document-uplift — Overview Specification

**Target output path:** `docs/015-canonical-document-uplift/spec-overview-canonical-document-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## 0. Related documents

- Canonical document model + index mapping (historical): `docs/011-canonical-document/`
- Ingestion rules guide: `docs/ingestion-rules.md`

Component specs in this work package:

- Canonical document uplift: `docs/015-canonical-document-uplift/spec-domain-canonical-document-uplift_v0.01.md`
- FileShare `BasicEnricher`: `docs/015-canonical-document-uplift/spec-domain-basic-enricher_v0.01.md`
- Ingestion rules DSL impact check: `docs/015-canonical-document-uplift/spec-domain-ingestion-rules-dsl-impact_v0.01.md`

---

## 1. Overview

This work package uplifts the ingestion canonical document model and the FileShare provider enrichment baseline so that:

- The canonical document retains a **provider-agnostic** record of the *active ingestion properties* (AddItem/UpdateItem) for traceability.
- A new low-ordinal enricher (`BasicEnricher`) performs a deterministic “baseline” enrichment by copying ingestion properties into:
  - `CanonicalDocument.Keywords` (values only)
  - `CanonicalDocument.Facets` (name/value)
- The canonical document includes a first-class `Timestamp` for downstream consumers.

This is intended to provide consistent data availability for indexing, filtering/aggregation, and rule-based enrichment.

---

## 2. High-level components

1. **Canonical Document Model (`CanonicalDocument`)**
   - Adds a first-class `Timestamp`.
   - Changes `Source` from an `IngestionRequest` snapshot to a minimal list of `IngestionProperty` representing the active Add/Update payload.

2. **FileShare Provider Baseline Enricher (`BasicEnricher`)**
   - Runs early in enrichment ordering (ordinal 10).
   - Maps ingestion properties into canonical keywords and facets.

3. **Ingestion Rules DSL Compatibility & Documentation**
   - Validate and (if required) update ingestion rules path parsing tests and documentation to ensure this uplift does not introduce ambiguity.

4. **Tests**
   - Update existing `CanonicalDocument` tests for the new `Source`/`Timestamp` shape.
   - Add a complete set of tests for `BasicEnricher` behavior and edge cases.

---

## 3. High-level flow (ingestion → canonical → enrichment)

At a high level:

1. Ingestion receives an `IngestionRequest` with exactly one of Add/Update/Delete/UpdateAcl.
2. For Add/Update, the provider’s canonical document builder creates a `CanonicalDocument` and stamps it with:
   - `Source` = active payload’s properties
   - `Timestamp` = active payload’s timestamp
3. Enrichment is applied in ordinal order:
   - `BasicEnricher` executes early to populate baseline keywords/facets.
   - Provider-specific enrichers execute later.
   - Rule-based enricher executes according to its configured ordinal.

(Implementation-specific requirements are defined in the component specs referenced above.)
