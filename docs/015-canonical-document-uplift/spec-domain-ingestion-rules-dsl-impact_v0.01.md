# Specification: Ingestion Rules DSL Impact Check (Canonical Document Uplift)

**Target output path:** `docs/015-canonical-document-uplift/spec-domain-ingestion-rules-dsl-impact_v0.01.md`

**Version:** v0.01 (Draft)

## 0. Related documents

- Overview: `docs/015-canonical-document-uplift/spec-overview-canonical-document-uplift_v0.01.md`
- Ingestion rules guide: `docs/ingestion-rules.md`

---

## 1. Summary

This work package changes the shape of `CanonicalDocument.Source` and introduces `CanonicalDocument.Timestamp`.

Although the ingestion rules DSL primarily evaluates predicates against the active Add/Update payload (not the canonical document), this uplift requires an explicit compatibility check:

- Ensure rule path parsing/resolution remains correct.
- Ensure documentation and examples do not incorrectly imply rules target `CanonicalDocument.Source`.

---

## 2. Goals

1. Confirm that ingestion rules predicate evaluation remains based on:
   - `request.AddItem` or `request.UpdateItem` payloads.
2. Ensure DSL path parsing and path resolution tests remain valid and cover any edge cases impacted by model changes.
3. Update `docs/ingestion-rules.md` if it contains examples or explanations that become misleading due to the canonical document uplift.

---

## 3. Requirements

### 3.1 DSL runtime behavior

- The rules engine MUST continue to evaluate predicates against the active Add/Update payload object.
- Paths like `properties["name"]` MUST continue to resolve against the active payload’s `Properties` collection.

### 3.2 Documentation

- `docs/ingestion-rules.md` MUST be reviewed for:
  - references to `CanonicalDocument.Source` (if any)
  - references to canonical document indexing of `source`
- If updates are necessary, they MUST:
  - clarify that rules operate on Add/Update payloads
  - avoid introducing specific requirements into overview-level documents

Decision (current):

- Do not add FileShare `BasicEnricher` behavior to `docs/ingestion-rules.md` unless the review identifies existing content that becomes misleading without it.

### 3.3 Tests

- All existing ingestion rules DSL parsing and evaluation tests MUST be re-run and updated if they assume a specific canonical document `Source` shape.
- Add a regression test (if absent) that asserts rule evaluation payload selection:
  - AddItem preferred when present
  - UpdateItem used when AddItem absent

---

## 4. Open questions

1. Do we want to extend the DSL in future to resolve paths against the canonical document (post-enrichment), or keep it strictly request-payload scoped?
