# Specification: Ingestion Rules Enrichment (Overview)

Version: v0.01  
Status: Draft  
Work Package: `docs/012-ingestion-rules/`

## 1. Summary
Introduce a small rules-based enrichment capability that can mutate `CanonicalDocument` fields using values available on an incoming `IngestionRequest` (specifically the active payload: `AddItem` or `UpdateItem`).

Rules are defined in a JSON file (`ingestion-rules.json`) and are loaded when the ingestion service starts.

This overview provides only a high-level description; detailed functional and technical requirements are defined in the component specification:
- `spec-ingestion-rules-engine_v0.01.md`

## 2. System context
The ingestion pipeline currently builds a `CanonicalDocument` from an `IngestionRequest` and then runs one or more enrichers.

The rules engine is a provider-agnostic enrichment component that:
- Interprets a JSON ruleset.
- Evaluates rule predicates against the ingestion request payload.
- Applies rule actions to mutate the canonical document (keywords/search text/facets/etc.).

## 3. Components / services
### 3.1 Rules enrichment engine
A reusable component responsible for:
- Loading and validating the rules DSL at process start.
- Evaluating the rules for each ingestion request.
- Applying rule actions to the mutable parts of `CanonicalDocument`.

See: `docs/012-ingestion-rules/spec-ingestion-rules-engine_v0.01.md`
