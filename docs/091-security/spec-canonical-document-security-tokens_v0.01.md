# Work Package: `091-security` — Restore Canonical `SecurityTokens`

**Target output path:** `docs/091-security/spec-canonical-document-security-tokens_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package restores the mandatory canonical `SecurityTokens` field that previously existed on `CanonicalDocument` and ensures the value is preserved from ingestion request through canonical enrichment and Elasticsearch indexing.

The intent is to re-establish `SecurityTokens` as a first-class part of the canonical search contract rather than leaving security classification only on the source request model.

### 1.2 Scope

This specification currently covers:

- restoring `CanonicalDocument.SecurityTokens` as `SortedSet<string>`
- adding canonical mutator methods for single and multi-value token input
- lower-case normalization, trimming, deterministic ordering, and de-duplication for canonical security tokens
- populating canonical security tokens when the minimal document is created from `IndexRequest`
- projecting canonical security tokens into the Elasticsearch-facing document
- mapping `securityTokens` as Elasticsearch `keyword`
- treating a canonical document with no retained security tokens as an ingestion failure in the same class of mandatory-document failure as missing `Title`
- updating relevant tests
- updating relevant `./wiki` pages

This work package restores request-sourced security tokens as part of the canonical model and allows enrichers to add further tokens later where required. It does not currently propose a new rule-authored security-token mutation model.

This work package does not expand implementation scope to query-side consumers or filters; query-side compatibility may be reviewed separately if needed.

### 1.3 Stakeholders

- ingestion developers
- infrastructure/indexing developers
- query/security developers
- maintainers of repository wiki/documentation
- test maintainers

### 1.4 Definitions

- `IndexRequest.SecurityTokens`: the inbound request security token array supplied by the caller.
- `CanonicalDocument.SecurityTokens`: the provider-independent canonical representation of retained security tokens.
- `CanonicalIndexDocument`: the Elasticsearch-facing projection of the canonical document.
- traceability copy: the original request payload preserved in `CanonicalDocument.Source`, including original `SecurityTokens` casing.
- retained security token: a non-null, non-empty, trimmed token value accepted into the canonical set after normalization.

## 2. System context

### 2.1 Current state

`IndexRequest` already requires a non-empty `SecurityTokens` array at request-validation time.

`CanonicalDocument` currently does not expose a corresponding `SecurityTokens` field, so canonical and indexed document shapes have drifted away from the inbound ingestion contract.

The minimal canonical creation flow currently preserves `Id`, `Provider`, `Source`, and `Timestamp`, while later enrichment stages add discovery fields such as `Title`, `Keywords`, `SearchText`, and taxonomy fields.

The Elasticsearch canonical mapping currently includes `provider`, `title`, `keywords`, taxonomy fields, `searchText`, `content`, and `geoPolygons`, but not `securityTokens`.

### 2.2 Proposed state

The system will be updated so that:

- `CanonicalDocument` contains `SecurityTokens` as `SortedSet<string>`.
- `CanonicalDocument` provides `AddSecurityToken(string)` and `AddSecurityToken(IEnumerable<string>)` methods aligned with the existing mutator pattern.
- canonical security tokens are trimmed, lower-cased, de-duplicated, and deterministically ordered.
- `CanonicalDocument.CreateMinimal(...)` copies `IndexRequest.SecurityTokens` into the canonical field when the document is first created.
- `CanonicalDocument.Source` preserves the original request payload for traceability, including the original `SecurityTokens` casing supplied by the caller.
- request-sourced and enricher-added security tokens are merged into one canonical set using the same normalization and de-duplication rules.
- repository wiki updates describe `SecurityTokens` as part of the canonical shape, but emphasize that it is a mandatory exact-match security/filter field rather than a user-facing display or full-text discovery field.
- code and index representations should use the same field/property casing convention already established by the surrounding canonical and index document model.
- `CanonicalIndexDocument` projects canonical security tokens to `securityTokens`.
- the Elasticsearch canonical mapping stores `securityTokens` as `keyword`.
- ingestion processing fails when the canonical document reaches validation/index eligibility with no retained security tokens, in the same mandatory-field sense as missing title.
- tests and `./wiki` are updated to describe and verify the restored canonical field.

### 2.3 Assumptions

- security tokens are intended for exact-match security/filtering behavior, not full-text analysis.
- lower-case normalization is required for consistent exact matching and to align with repository indexing guidance for normalized exact-match fields.
- canonical security tokens should be populated from the request at minimal-document creation rather than waiting for later enrichment.
- the defensive traceability copy held in `CanonicalDocument.Source` should preserve the original caller-supplied token casing rather than being normalized in place.
- enrichers may add further security tokens later, but all additions must go through the same canonical normalization, de-duplication, and ordering rules.
- request-sourced and enricher-added values belong to one unified canonical `SecurityTokens` set rather than separate collections.
- rule processing is not expected to be the source of security-token values for this work package unless clarified later.

### 2.4 Constraints

- `CanonicalDocument.SecurityTokens` must be mandatory at the effective ingestion-processing level.
- canonical token storage must follow the existing sorted-set pattern used for normalized exact-match string collections.
- the Elasticsearch field type must be `keyword`.
- relevant repository documentation under `./wiki` must be updated.
- relevant existing tests must be amended and new tests added where coverage is currently missing.

## 3. Component / service design (high level)

### 3.1 Components

1. `CanonicalDocument`
   - restore `SecurityTokens` as a canonical field
   - add canonical mutator methods
   - populate the field during minimal document creation
   - allow later enrichers to add further canonical tokens through the same mutator API into the same unified canonical set

2. Ingestion dispatch / minimal creation
   - ensure request security tokens are copied from `IndexRequest` into the canonical model as soon as the minimal document is created
   - preserve the original request payload in `Source` for traceability without lowercasing the source token values in place

3. Canonical index projection
   - add `securityTokens` to `CanonicalIndexDocument`
   - ensure projected index values come from canonical lowercased tokens rather than directly from the traceability copy

4. Elasticsearch mapping
   - add `securityTokens` as a `keyword` field in the canonical index definition and any mapping validation logic

5. Validation / failure handling
   - reject canonical documents that retain no security tokens after normalization, using the same canonical validation stage/path and processing-failure/dead-letter handling as missing title

6. Tests and documentation
   - update unit/integration-style tests and `./wiki` pages that describe the canonical model and ingestion walkthrough
   - document `SecurityTokens` in the canonical shape while making its security/filtering role explicit

### 3.2 Data flow

1. an `IndexRequest` arrives with `SecurityTokens`
2. dispatch creates a minimal `CanonicalDocument`
3. minimal creation copies request `SecurityTokens` into `CanonicalDocument.SecurityTokens`
4. the original request payload remains preserved in `Source` for traceability
5. canonical mutation rules normalize the tokens to trimmed lower-case set values
6. later enrichers may add further security tokens and other canonical fields
7. the same canonical validation stage/path that rejects missing `Title` also checks that at least one security token remains
8. the canonical document is projected into `CanonicalIndexDocument`
9. Elasticsearch stores `securityTokens` as a multi-valued `keyword`

### 3.3 Key decisions captured so far

- `SecurityTokens` is a canonical field, not just source metadata.
- the traceability copy in `Source` preserves original request token casing.
- canonical token values are normalized to lower-case.
- canonical token values are set during minimal creation from `IndexRequest.SecurityTokens`.
- enrichers may add further security tokens after minimal creation.
- a document with no retained canonical security tokens is a processing failure and must not be indexed.
- this work package updates code-facing documentation and tests alongside the implementation.

## 4. Functional requirements

### FR-001 Restore canonical `SecurityTokens`

`CanonicalDocument` shall expose a `SecurityTokens` field of type `SortedSet<string>`.

The field shall follow the same canonical serialization pattern already used by the other set-based canonical fields, including the existing `JsonInclude` plus private-set shape where that pattern is used elsewhere in `CanonicalDocument`.

The field shall also use the same comparer and deterministic ordering behavior already used by the other normalized string-set fields on `CanonicalDocument`.

### FR-002 Canonical mutator methods

`CanonicalDocument` shall provide:

- `AddSecurityToken(string)`
- `AddSecurityToken(IEnumerable<string>)`

The mutators shall follow the established canonical add-method pattern for exact-match string collections.

`AddSecurityToken(IEnumerable<string>)` shall ignore a `null` collection input in the same way as the other collection-based canonical add methods.

### FR-003 Canonical token normalization

Security token mutation shall:

- ignore `null`, empty, and whitespace-only values
- trim leading and trailing whitespace
- normalize retained values to lower-case using the same invariant approach as the other normalized canonical string fields
- suppress duplicates after normalization
- retain deterministic ordering through the sorted set

Inputs that differ only by surrounding whitespace and/or casing shall collapse to one retained canonical token.

### FR-004 Minimal creation population

When `CanonicalDocument` is created from an `IndexRequest`, the request `SecurityTokens` values shall be copied into the canonical `SecurityTokens` field during minimal creation.

`CreateMinimal(...)` shall populate canonical security tokens through the canonical add-method path so the normal normalization, de-duplication, comparer, and ordering rules are applied consistently.

### FR-004b Traceability preservation

The defensive request copy held in `CanonicalDocument.Source` shall preserve the original `IndexRequest.SecurityTokens` values and casing for traceability.

Normalization to lower-case shall apply to canonical and indexed security-token fields, not by mutating the traceability copy in place.

### FR-004a Enricher extensibility

Enrichers may add further security tokens after minimal creation.

Any enricher-added values shall use the canonical mutator methods and therefore follow the same lower-case normalization, trimming, de-duplication, and deterministic ordering rules as request-sourced values.

### FR-004c Unified canonical token set

Request-sourced and enricher-added security tokens shall be merged into one `CanonicalDocument.SecurityTokens` set.

The canonical model shall not maintain separate retained collections for request-originated versus enricher-originated security tokens.

### FR-005 Elasticsearch projection

`CanonicalIndexDocument` shall include `securityTokens` sourced from `CanonicalDocument.SecurityTokens`.

### FR-006 Elasticsearch mapping

The canonical Elasticsearch index mapping shall define `securityTokens` as `keyword`.

The implementation shall follow the existing field naming and casing convention already used by the surrounding canonical/index model rather than introducing a one-off alternative casing style.

### FR-007 Mandatory retained tokens

A canonical document shall be considered ingestion-invalid if `CanonicalDocument.SecurityTokens` contains no retained strings at the point where canonical mandatory-field validation is enforced for indexing.

This rule shall be enforced in the same canonical validation stage/path that already rejects documents with no retained `Title`.

Documents that fail this mandatory security-token validation shall follow the same existing processing-failure/dead-letter path as documents that fail the mandatory `Title` rule.

This requirement is additive and shall not remove or weaken the existing `IndexRequest.SecurityTokens` request-validation rules.

### FR-008 Tests

Relevant existing tests shall be updated and new tests shall be added to verify:

- canonical token normalization and de-duplication
- `CreateMinimal(...)` copies `IndexRequest.SecurityTokens` into `CanonicalDocument.SecurityTokens` immediately before any enricher runs
- minimal creation copying from `IndexRequest.SecurityTokens`
- canonical JSON round-trip behavior preserves `SecurityTokens` correctly across serialization/deserialization
- index projection of `securityTokens`
- Elasticsearch mapping for `securityTokens`
- Elasticsearch mapping validation tests are updated so the restored `SecurityTokens` mapping is part of the minimum required regression coverage
- failure behavior when no retained security tokens exist
- failure behavior where request input is initially non-empty but canonical normalization discards all values, such as whitespace-only token inputs through applicable constructors or deserialization paths

All affected tests shall be updated to reflect the restored `SecurityTokens` feature as part of the current canonical shape.

Because the canonical model is still in development, the test estate does not need to preserve assertions for the older shape that omitted `SecurityTokens`.

This requirement applies to all affected existing tests with no exceptions.

All existing test classes and cases that serialize, project, validate, compare, or otherwise assert the canonical document shape shall be brought to the new shape where this feature is relevant.

This explicitly includes existing test classes and cases covering canonical serialization, canonical JSON round-trip behavior, index projection, Elasticsearch mapping, mapping validation, ingestion/canonical validation, and any documentation-related coverage that asserts or snapshots the canonical field shape.

This also includes test fixtures, test builders, sample canonical documents, and test example payloads where canonical field lists or example document shapes are constructed.

### FR-009 Wiki updates

Relevant repository wiki pages shall be updated so the canonical model and ingestion flow document `SecurityTokens` as a restored mandatory canonical field.

Wiki guidance shall list `SecurityTokens` in the canonical shape and explain that it is primarily an exact-match security/filter field, not a user-facing display or analyzed search field.

Existing wiki examples, field lists, and canonical-shape descriptions that currently omit `SecurityTokens` shall be updated where they would otherwise become incomplete or misleading.

At minimum, this shall include updates to:

- `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
- `wiki/Ingestion-Walkthrough.md`

## 5. Non-functional requirements

### NFR-001 Deterministic behavior

Security token handling shall remain deterministic across runs for the same inputs, including retained value ordering.

### NFR-002 Provider independence

The canonical representation of security tokens shall remain provider-independent even though values originate from the ingestion request.

### NFR-003 Documentation quality

Wiki and specification updates shall describe internal implementation expectations at the same developer-detail standard used for public-facing components.

## 6. Affected areas

- `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
- `src/UKHO.Search.Ingestion/Requests/IndexRequest.cs`
- minimal canonical creation and validation paths in ingestion pipeline code
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDocument.cs`
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`
- mapping validation logic/tests in infrastructure ingestion test coverage
- relevant wiki pages, expected at minimum to include `wiki/CanonicalDocument-and-Discovery-Taxonomy.md` and `wiki/Ingestion-Walkthrough.md`

## 7. Testing strategy

The implementation should be verified with focused tests covering:

- canonical single-value and multi-value add methods
- lowercase normalization, trimming, and de-duplication behavior
- copying request tokens into the minimal canonical document
- a focused regression test proving `CreateMinimal(...)` populates canonical security tokens before any enricher contributes additional values
- canonical JSON round-trip coverage proving the `SortedSet<string>`-backed `SecurityTokens` field serializes and deserializes with the expected retained values and ordering
- index projection serialization shape for `securityTokens`
- index mapping validation for `securityTokens`
- mandatory-field failure behavior when canonical tokens are absent after normalization
- an explicit regression case where incoming request token data is non-empty before canonical normalization but results in zero retained canonical tokens after normalization
- updated wiki references where repository documentation tests or checks exist

## 8. Clarification log

### CL-001 Ownership of canonical `SecurityTokens` after minimal creation

Confirmed: `SecurityTokens` is initially copied from `IndexRequest.SecurityTokens` during minimal creation, and enrichers may add further canonical tokens later if needed.

Rules are not currently in scope as token authors for this work package.

### CL-002 Source casing versus canonical casing

Confirmed: original `IndexRequest.SecurityTokens` casing is preserved in the traceability copy stored in `CanonicalDocument.Source`.

`CanonicalDocument.SecurityTokens` and the indexed `securityTokens` field are lowercased for normalization and filtering behaviour.

### CL-003 Validation stage for missing canonical security tokens

Confirmed: the missing-security-token failure is enforced in the same canonical validation stage/path that already rejects documents with no retained `Title`.

`CreateMinimal(...)` should populate canonical tokens, but the mandatory-field failure rule is owned by the later canonical validation path.

### CL-004 Unified handling of request and enricher tokens

Confirmed: request-sourced and enricher-added tokens are merged into one unified `CanonicalDocument.SecurityTokens` set.

De-duplication occurs after canonical lowercase normalization, regardless of where each token originated.

### CL-005 Whitespace and casing collapse

Confirmed: values such as `" Admin "`, `"admin"`, and `"ADMIN"` collapse to one retained canonical token: `admin`.

Whitespace-only values are discarded.

### CL-006 Wiki positioning of `SecurityTokens`

Confirmed: `SecurityTokens` should be listed as part of the canonical shape in repository wiki updates.

The documentation should explicitly frame it as a mandatory exact-match security/filter field rather than a user-facing display or full-text discovery field.

### CL-007 Focused `CreateMinimal(...)` regression coverage

Confirmed: the spec explicitly requires a focused regression test proving `CreateMinimal(...)` copies `IndexRequest.SecurityTokens` into `CanonicalDocument.SecurityTokens` immediately, before any enricher runs.

### CL-008 Non-empty request input that normalizes to zero canonical tokens

Confirmed: the spec explicitly requires regression coverage for cases where request token input is initially non-empty but canonical normalization discards all values, such as whitespace-only token inputs through applicable constructors or deserialization paths.

### CL-009 Canonical JSON round-trip coverage

Confirmed: the spec explicitly requires JSON round-trip coverage for `CanonicalDocument.SecurityTokens` so the `SortedSet<string>` field survives serialization and deserialization correctly.

### CL-010 Field naming and casing convention

Confirmed: the field is named `SecurityTokens` and should use whatever casing convention is already in place for the surrounding fields.

The spec should align with existing repository conventions rather than introducing a new special-case casing rule for this field.

### CL-011 Query-side scope

Confirmed: scope remains strictly on ingestion, canonical/index mapping, wiki, and tests.

Query-side models or filters are not included as required implementation scope for this work package.

### CL-012 Existing `IndexRequest` validation remains in place

Confirmed: existing `IndexRequest.SecurityTokens` validation remains unchanged.

Canonical validation adds a second mandatory-field safeguard later in the ingestion flow rather than replacing request validation.

### CL-013 Concrete wiki/list updates

Confirmed: the spec explicitly requires updating existing wiki field lists, examples, and canonical-shape descriptions wherever omission of `SecurityTokens` would leave the documentation incomplete.

### CL-014 Minimum wiki pages to update

Confirmed: the minimum required wiki pages are:

- `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
- `wiki/Ingestion-Walkthrough.md`

### CL-015 Elasticsearch mapping validation coverage

Confirmed: updating the Elasticsearch mapping validation tests is a mandatory part of the minimum test changes for this work package.

### CL-016 Update all affected tests for the new canonical shape

Confirmed: all affected tests must be updated for this mandatory feature.

The spec should assume the old `CanonicalDocument` shape is no longer the expected target for tests, because this area is still in development.

### CL-017 No exceptions for affected test updates

Confirmed: all affected tests are mandatory to bring to the new shape, with no exceptions.

The spec should explicitly require updating every affected existing test that depends on the canonical shape rather than allowing partial or selective coverage updates.

### CL-018 Mandatory affected test categories

Confirmed: the spec should explicitly call out the affected test categories that must be updated.

This includes serialization, canonical JSON round-trip, projection, Elasticsearch mapping, mapping validation, ingestion/canonical validation, and documentation-related coverage where those tests assert the canonical shape.

### CL-019 Canonical field serialization pattern

Confirmed: `CanonicalDocument.SecurityTokens` should use the same `JsonInclude` and private-set serialization pattern already used by the other set-based canonical fields in `CanonicalDocument`.

### CL-020 Lowercase normalization approach

Confirmed: lowercase normalization for `SecurityTokens` should use the same invariant approach already used by the other normalized canonical string fields.

### CL-021 Set comparer and ordering behavior

Confirmed: `CanonicalDocument.SecurityTokens` should use the same comparer and deterministic ordering pattern already used by the other normalized string-set fields on `CanonicalDocument`.

### CL-022 Null collection handling for bulk add

Confirmed: `AddSecurityToken(IEnumerable<string>)` should ignore a `null` collection input in the same way as the other collection-based canonical add methods.

### CL-023 `CreateMinimal(...)` population path

Confirmed: `CreateMinimal(...)` should populate `SecurityTokens` through the canonical add-method path rather than by directly assigning the set.

This ensures the normal canonical normalization and ordering rules are applied in one place.

### CL-024 Failure/dead-letter path

Confirmed: documents that fail mandatory `SecurityTokens` validation should follow the same existing processing-failure/dead-letter path as documents with no retained `Title`.

### CL-025 Test fixtures, builders, and examples

Confirmed: `SecurityTokens` must be added not only to assertions, but also to affected test fixtures, test builders, sample canonical documents, and test example payloads where the canonical shape is constructed or documented.

### CL-026 Clarification complete

Confirmed: the specification is now sufficiently clarified to proceed without further routine clarification questions.

Any remaining low-risk implementation details should follow the existing repository conventions and the defaults already established in this specification.
