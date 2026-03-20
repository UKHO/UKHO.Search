# Work Package: `054-rule-title` — Canonical `Title` Field and Mandatory Rule Title Mapping

**Target output path:** `docs/054-rule-title/spec-domain-canonical-document-title_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package introduces a new canonical discovery field, `Title`, to `CanonicalDocument` so indexed documents can carry a human-meaningful search-result title.

The field is intended to provide a good summary for query results while remaining compatible with the existing canonical-document mutation pattern and rules-driven enrichment model.

This work also makes rule-authored title mapping mandatory by extending the rule contract so every rule definition contains a `rule.title` mapping template. When a rule matches, that template contributes one or more values to `CanonicalDocument.Title`.

### 1.2 Scope

This specification covers:

- the `CanonicalDocument.Title` field
- canonical/index projection and Elasticsearch mapping updates
- rule schema and runtime updates for mandatory `rule.title`
- rule catalog loading validation
- updates to rule assets under `./rules`
- updates to existing tests and addition of new tests
- updates to relevant documentation under `./wiki`

This specification does not require a separate migration path for legacy rule payloads. All in-repository rules are updated in place and remain on `schemaVersion` `1.0`.

### 1.3 Stakeholders

- ingestion developers
- query developers
- rule authors
- RulesWorkbench users
- maintainers of repository wiki/documentation

### 1.4 Definitions

- `CanonicalDocument`: provider-independent search document produced during ingestion.
- `CanonicalIndexDocument`: Elasticsearch-facing projection of the canonical document.
- `Title`: a multi-valued canonical field containing display-quality title text for search results.
- `rule.title`: a mandatory rule-level title template that is evaluated when a rule matches and written into `CanonicalDocument.Title`.
- title mapping: the template string authored in `rule.title`, including literal text and supported runtime substitutions such as `$path:` and `$val`.

## 2. System context

### 2.1 Current state

The current canonical discovery model includes fields such as `Keywords`, `SearchText`, `Content`, taxonomy fields, and `GeoPolygons`, but it does not contain a dedicated title field.

The rules engine currently enriches `CanonicalDocument` through `then` actions such as `keywords.add`, `searchText.add`, `content.add`, and taxonomy field mutations. Rule files under `./rules` do not currently require a title mapping.

The wiki documentation currently describes the canonical document and rule model without a dedicated title surface.

### 2.2 Proposed state

The system will be updated so that:

- `CanonicalDocument` contains a multi-valued `Title` field with matching mutator methods such as `AddTitle(...)`.
- the canonical-to-index projection includes `Title`.
- the Elasticsearch mapping includes `title` as a `keyword` field that supports multiple values.
- every rule definition contains a mandatory `rule.title` mapping in the position shown below:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "bu-adds-s57-5-dvd-info-support",
    "context": "adds-s57",
    "title": "DVD Info support $path:properties[\"content\"]",
    "description": "ADDS-S57 DVD info support item.",
    "enabled": true
  }
}
```

- when a rule matches, `rule.title` is evaluated using the existing templating semantics and its resolved values are added to `CanonicalDocument.Title`.
- rule loading fails if any rule omits `rule.title` or provides an invalid/blank title template.
- all existing rule files and affected tests/wiki pages are updated to reflect the mandatory title contract.

### 2.3 Assumptions

- `Title` is intended primarily for display, not for full-text analysis.
- multiple title values are required to align with the existing canonical mutation pattern.
- all indexed documents pass through the ingestion rules path.
- all rule files under `./rules` that are treated as repository-owned rule definitions must be brought into compliance.
- no compatibility layer is required for pre-title rules at runtime.

### 2.4 Constraints

- `schemaVersion` remains `1.0`.
- rule loading remains fail-fast for invalid rule definitions.
- title mapping must fit into the current rule file structure without introducing a second rule schema version.
- the work must update repository documentation in `./wiki` where the canonical document, rule contract, or RulesWorkbench behavior is described.

## 3. Component / service design (high level)

### 3.1 Components

The work affects the following areas:

1. `CanonicalDocument`
   - add `Title` as a multi-valued canonical field
   - add matching mutation helpers such as `AddTitle(...)`

2. Canonical index projection
   - project `CanonicalDocument.Title` into `CanonicalIndexDocument`
   - add/update Elasticsearch mapping for `title`

3. Rules engine and rule model
   - recognize mandatory `rule.title`
   - evaluate `rule.title` when a rule matches
   - add resolved values to `CanonicalDocument.Title`
   - fail rule loading when `rule.title` is absent or invalid

4. Rule assets under `./rules`
   - update all existing rule files to include a meaningful `title`

5. Tests
   - update existing tests that assert canonical shape, rule parsing, rule application, mapping, or repository rule validity
   - add focused tests for title behavior

6. Wiki and authoring guidance
   - update the relevant pages in `./wiki`

### 3.2 Data flows

Updated enrichment flow:

1. provider/request data is transformed into a minimal `CanonicalDocument`
2. rules are loaded and validated
3. a matching rule evaluates `rule.title`
4. resolved title value(s) are added to `CanonicalDocument.Title`
5. other `then` actions continue to mutate the canonical document as today
6. the fully enriched canonical document is validated to ensure at least one non-empty title value exists
7. documents with no retained title values are rejected and routed through the existing failure/dead-letter path rather than indexed
8. valid canonical documents are projected into `CanonicalIndexDocument`
9. Elasticsearch stores `title` as a multi-valued `keyword`
10. query-side consumers can retrieve the indexed title values for display

### 3.3 Key decisions

- `Title` is a canonical field, not just an index-only projection field.
- `Title` is multi-valued to match the general canonical-field pattern.
- `Title` is authored from `rule.title`, not from a new `then.title.add` action.
- `rule.title` is mandatory for every rule and missing values are a startup/configuration error.
- because every indexed document flows through the rules path, at least one non-empty retained title value is mandatory before indexing; a document with no title is treated as a processing failure and must be dead-lettered rather than indexed.
- because `Title` is intended for display, title values must preserve authored casing and should not be lower-cased as part of canonical mutation.
- title values must still be trimmed, null/empty values skipped, and duplicates suppressed.
- title ordering must be deterministic; the first retained title value is the preferred display title when a downstream consumer requires a single title.

## 4. Functional requirements

### FR-001 Canonical `Title` field

`CanonicalDocument` shall expose a new field named `Title`.

The field shall:

- support zero or more values
- be part of the canonical discovery surface
- be available to downstream indexing and query consumers

### FR-002 Title mutator API

`CanonicalDocument` shall provide mutator methods consistent with the existing field pattern, including `AddTitle(...)`.

The implementation shall support:

- adding a single title value
- adding multiple title values where the current canonical API pattern supports it
- de-duplication of equivalent values
- deterministic ordering of retained values

### FR-003 Title value normalization

Title mutation shall:

- trim leading/trailing whitespace
- ignore `null`, empty, or whitespace-only inputs
- preserve original casing for display purposes
- avoid adding duplicate retained values

Title mutation shall not lower-case values by default.

### FR-004 Canonical index projection

The canonical document projection shall include `Title` so that indexed documents contain the same title values produced during enrichment.

### FR-005 Elasticsearch mapping

The canonical index mapping shall include a `title` field.

The `title` field shall:

- be mapped as `keyword`
- support multiple values
- preserve the display values provided by the canonical projection

### FR-006 Rule contract: mandatory `rule.title`

Every rule definition shall contain a `rule.title` property.

The property shall:

- live alongside `id`, `context`, and `description` as shown in the required JSON shape
- be required for all rules under `./rules`
- be treated as part of rule validity

### FR-007 Rule title templating semantics

`rule.title` shall support the same runtime template concepts already used by the rules engine for string outputs, including literal text and supported substitutions such as `$path:` and `$val` where applicable.

When a rule matches:

- the engine shall evaluate `rule.title`
- any resolved non-empty value(s) shall be added to `CanonicalDocument.Title`
- duplicate/empty results shall be skipped using canonical title mutation rules

### FR-008 Rule loading validation

Every rule-authored document intended for indexing shall finish enrichment with at least one retained non-empty title value.

If a fully enriched `CanonicalDocument` contains no retained title values:

- the document shall be rejected before indexing
- the document shall be treated as a processing failure
- the document shall be routed to the existing dead-letter/failure handling path with a clear validation reason

### FR-009 Rule loading validation

Rule loading shall fail if any rule:

- omits `rule.title`
- provides `rule.title` as `null`
- provides `rule.title` as an empty or whitespace-only string
- provides a title template that is syntactically invalid according to the existing template/parser rules

This is a configuration/startup error and shall prevent the ruleset from being accepted.

### FR-010 Existing rules update

All existing repository rule definitions under `./rules` shall be updated to include a reasonable display-oriented `rule.title`.

The authored title should use best judgement for the rule intent and produce a useful human-readable summary when applied.

### FR-011 Existing test updates

Existing tests shall be updated where necessary to account for:

- the new `CanonicalDocument.Title` field
- the updated canonical/index mapping
- the mandatory `rule.title` contract
- rejection/dead-letter behavior when a fully enriched canonical document has no retained title
- updated repository rule files

### FR-012 New tests

New tests shall be added to cover:

- `CanonicalDocument.Title` mutation behavior
- title de-duplication and empty-value handling
- case preservation for titles
- canonical-to-index mapping of `Title`
- rule parsing/validation success when `rule.title` is present
- rule parsing/validation failure when `rule.title` is missing or blank
- rule application adding title values to `CanonicalDocument`
- rejection of a fully enriched canonical document when no non-empty title value is retained
- routing of missing-title documents to the failure/dead-letter path rather than indexing
- repository ruleset validity after all rule files are updated

### FR-013 Wiki updates

The following wiki pages shall be reviewed and updated where appropriate:

- `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
- `wiki/Ingestion-Rules.md`
- `wiki/Tools-RulesWorkbench.md`
- `wiki/Ingestion-Pipeline.md`
- `wiki/Home.md`
- `wiki/Documentation-Source-Map.md`

The updates shall describe the new `Title` field, the mandatory `rule.title` contract, and any RulesWorkbench behavior or authoring guidance that changes because of this work.

## 5. Non-functional requirements

### NFR-001 Determinism

Title mutation and rule evaluation outcomes shall be deterministic for the same input payload and ruleset.

### NFR-002 Backward compatibility stance

This work is intentionally a breaking ruleset-contract change at repository level. No runtime migration or fallback support is required for rules lacking `title`.

### NFR-003 Maintainability

The implementation shall follow the existing canonical-field and rule-validation patterns where practical, keeping the new title behavior obvious to future rule authors and maintainers.

### NFR-004 Authoring usability

Rule authors shall be able to understand and supply `rule.title` using the same mental model as other rule-authored string outputs.

### NFR-005 Documentation quality

Repository wiki guidance shall be sufficient for a developer to discover:

- what `Title` is for
- how `rule.title` works
- why `rule.title` is mandatory
- where to update or inspect title behavior in local workflows

## 6. Data model

### 6.1 `CanonicalDocument`

Add a new canonical field:

| Field | Type | Cardinality | Notes |
|---|---|---:|---|
| `Title` | string collection | 0..n | display-oriented title values; preserved casing; deterministic ordering; de-duplicated |

### 6.2 `CanonicalIndexDocument`

Add/update the projected field:

| Field | Type | Cardinality | Notes |
|---|---|---:|---|
| `title` | `keyword` | 0..n | exact stored values for display retrieval |

### 6.3 Rule JSON contract

Updated rule shape:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "bu-example-1",
    "context": "example-context",
    "title": "Example display title $path:properties[\"content\"]",
    "description": "Example rule description.",
    "enabled": true,
    "if": {
      "properties[\"product\"]": "AVCS"
    },
    "then": {
      "keywords": {
        "add": [
          "exchange-set"
        ]
      }
    }
  }
}
```

### 6.4 Runtime title behavior

If multiple rules match, each matching rule may contribute one or more title values.

The canonical document shall retain all accepted distinct title values. Downstream consumers that require a single title should treat the first retained value as the preferred display title.

If no title values are retained after enrichment, the document is invalid for indexing and shall be rejected via the failure/dead-letter path.

## 7. Interfaces & integration

### 7.1 Rule loading interface

The rule loader/validator integration shall treat `rule.title` as a required field in the same validation pass that currently enforces schema validity and other mandatory rule metadata.

### 7.2 Rules engine integration

The rules engine shall integrate title application into the existing enrichment pipeline without introducing a separate migration mode or alternate schema version.

Because all indexed documents pass through the rules path, missing title after enrichment is treated as a document-processing failure rather than an optional enrichment outcome.

### 7.3 Query/index integration

The indexing contract shall make `title` available in the canonical Elasticsearch document so query-side components can display a meaningful result title.

This work does not require a separate migration path for already-indexed documents as part of the specification scope.

## 8. Observability (logging/metrics/tracing)

- rule-loading failures caused by missing/invalid `rule.title` shall remain visible through the existing configuration/startup failure surfaces
- document-processing failures caused by missing retained title values shall be visible through the existing validation/dead-letter diagnostics
- any diagnostic output or validation messaging that currently describes rule metadata should include `title` where helpful
- RulesWorkbench and related validation experiences should surface title-related validation errors clearly

## 9. Security & compliance

No new security boundary is introduced by this change.

Existing rule validation and repository-controlled rule authoring remain the primary controls for preventing malformed title mappings from entering the runtime ruleset.

## 10. Testing strategy

### 10.1 Unit tests

Add or update unit tests for:

- `CanonicalDocument` title mutators
- normalization behavior for title values
- title duplicate suppression
- title case preservation
- validation failure when a fully enriched canonical document has no retained title values
- index projection/mapping population
- rule JSON validation for mandatory `title`
- rule runtime application of `title`

### 10.2 Integration / ruleset tests

Add or update tests that:

- load representative rule files containing `title`
- assert that a missing `title` prevents ruleset loading
- assert that matching rules contribute title values to the canonical document
- assert that documents with no retained title are rejected before indexing
- assert that missing-title documents are routed to dead-letter/failure handling rather than indexed
- validate the in-repository rules under `./rules` after all files are updated

### 10.3 Documentation / tooling verification

Verify that relevant wiki examples and RulesWorkbench-facing guidance align with the final rule JSON contract and title behavior.

## 11. Rollout / migration

- no schema-version increment is required; rules remain at `schemaVersion` `1.0`
- no runtime migration path is required for old rule shapes
- all repository rule files are updated as part of the same change set
- environments consuming the updated rules must load only the updated ruleset; any missing-title rule is treated as invalid and prevents load

## 12. Open questions

None.
