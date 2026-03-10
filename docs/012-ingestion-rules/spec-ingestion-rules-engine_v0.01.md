# Specification: Ingestion Rules Enrichment Engine

Version: v0.01  
Status: Draft  
Work Package: `docs/012-ingestion-rules/`

Note: This specification is not considered complete until the test suite described in §9 is implemented and passing (a complete and comprehensive set of automated tests is part of the deliverable).

## 1. Summary
Build a small, rules-based enrichment engine that updates a `CanonicalDocument` using values found in an `IngestionRequest`.

- Rules are stored in `ingestion-rules.json` and loaded when the ingestion service starts.
- The rules engine implementation is provider-agnostic and belongs in the shared ingestion infrastructure (not in any specific provider project).
- Rules in `ingestion-rules.json` are scoped by provider name; each provider's rules live under a property whose name matches `IIngestionDataProviderFactory.Name` (e.g., `"file-share"`).
- The engine is invoked with a provider name, an `IngestionRequest`, and a `CanonicalDocument`, and mutates the document in-place using existing mutation APIs (e.g., `AddKeyword`, `SetSearchText`, `AddFacetValue`, etc.).
- The engine evaluates rules against the active ingestion payload: `IngestionRequest.AddItem` or `IngestionRequest.UpdateItem` (exactly one is non-null).

## 2. Goals
- Provide a JSON rules DSL that is:
  - Easy to author and review.
  - Safe (bounded execution, no arbitrary code).
  - Validated at startup (fail fast on invalid rules).
- Support predicate evaluation against `IngestionRequest` data, including:
  - File metadata (e.g., MIME type).
  - Ingestion properties (name/type/value).
- Support actions that can update any mutable part of `CanonicalDocument`.
- Ensure all enriched values respect existing canonical normalization behaviour (lowercasing, trimming, dedupe where applicable).

## 3. Non-goals
- A general-purpose scripting engine.
- External data lookups (HTTP, databases) during rule evaluation.
- Cross-request state (each evaluation is independent).
- Editing rules at runtime (initially startup-load only).

## 4. Background / evidence
- `CanonicalDocument` exposes mutation APIs for:
  - `Keywords` via `AddKeyword` / `AddKeywords` / `SetKeywordsFromTokens`.
  - `SearchText` via `SetSearchText`.
  - `Content` via `SetContent`.
  - `Facets` via `AddFacetValue` / `AddFacetValues`.
  - `DocumentType` via a public setter.
- `IngestionRequest` contains exactly one of: `AddItem`, `UpdateItem`, `DeleteItem`, `UpdateAcl`.
- This rules engine applies only to `AddItem` and `UpdateItem` requests.

## 5. Requirements

### 5.1 Rule engine API
- The engine MUST expose an API that accepts:
  - `string providerName` (must match `IIngestionDataProviderFactory.Name`)
  - `IngestionRequest request`
  - `CanonicalDocument document`
  - and mutates `document` based on the rules.
- The engine MUST evaluate only the rules scoped to the specified `providerName`.
- The engine MUST operate on the active payload (`request.AddItem` or `request.UpdateItem`).
  - If both are null, the engine MUST perform no mutations and SHOULD report a structured error (exact behaviour TBD).

Decision (captured from Q&A):
- Rules do not have per-request-type scoping in v0.01; they apply to whichever active payload is present (`AddItem` or `UpdateItem`).

### 5.2 Rule loading
- The ingestion service host MUST load a rules file named `ingestion-rules.json` at startup.
  - Initial location (in-repo): `src/Hosts/IngestionServiceHost/ingestion-rules.json`.
- Rules MUST be validated on load.
  - Invalid JSON or schema violations SHOULD fail startup (exact policy TBD).

Decision (captured from Q&A):
- v0.01 is **fail-fast**: invalid rules cause service startup to fail (no partial-load behaviour).

Decision (captured from Q&A):
- The rules file is **required** in v0.01.
  - If the file is missing OR contains no rules for any provider (i.e., `rules` has no provider keys, or all provider rule arrays are empty), startup MUST fail.

### 5.3 Observability
Decision (captured from Q&A):
- v0.01 MUST support per-request logging at `Debug` (or equivalent) that includes:
  - the active `providerName`
  - the matched rule `id` values (in application order, within that provider)
  - a summary of actions applied (keywords/searchText/content/facets/documentType)
- Startup SHOULD log the number of rules loaded per provider and their `id` values (exact log level TBD).

### 5.4 JSON DSL (v0.01)
This section captures the v0.01 DSL shape and rules.

#### 5.4.1 Top-level
- The rules file MUST be a JSON object with:
  - `schemaVersion` (string)
  - `rules` (object) whose properties are provider names and values are arrays of rule objects
- The engine MUST fail startup if `schemaVersion` is missing or unsupported.

Decision (captured from Q&A):
- v0.01 uses `schemaVersion` value `"1.0"`.

- Each rule object MUST contain:
  - `id` (string)
  - a predicate block (`if` or `match`)
  - a structured action block `then`

Decision (captured from Q&A):
- Each rule MUST have an `id` (string).
- Each rule MAY have:
  - `description` (string)
  - `enabled` (bool; default true)

Decision (captured from Q&A):
- v0.01 uses a structured action shape under `then` (not flat `addKeywords`/`addFacets` fields).

Decision (captured from Q&A):
- When multiple rules match (for the active provider), the engine applies **all matching rules**, in **file order**.

Decision:
- Rule order and application is per-provider (i.e., within the provider's `rules["<provider>"]` array).

Draft example (informative only):
- Rule 1: if any file has `mimeType == "app/s63"` then add keywords and search text.
- Rule 2: if property `abcdef == "a value"` then add keywords.
- Rule 3: if property `abcdef` exists then add facet `facet 1` with value equal to that property value.

#### 5.4.2 Predicate block
Open design note: `when` currently implies existence but not comparison. The DSL likely needs explicit operators.

Draft proposal:
- Use `if` (or `match`) instead of `when`.
- Predicate supports `all` (AND) / `any` (OR) composition.
- Leaf conditions identify a value via a path and apply an operator.

Decision (captured from Q&A):
- Support BOTH:
  - A shorthand form for simple AND-only conditions.
  - An explicit boolean form (`all`/`any`/`not`) for more complex rules.

Decision (captured from Q&A):
- Shorthand AND-only form uses `path -> scalar` mappings where the scalar implies `eq`.
  - Example: `"if": { "properties[\"abcdef\"]": "a value" }` is equivalent to `eq`.

Decision (captured from Q&A):
- The explicit boolean predicate form uses leaf conditions shaped as:
  - `{ "path": "...", "eq": "..." }`
  - `{ "path": "...", "contains": "..." }`
  - `{ "path": "...", "exists": true }`
  - etc.
  (i.e., operator-as-property, not a generic `op`/`value` pair)

Decision (captured from Q&A):
- In the explicit boolean form, each boolean node MUST contain exactly one of:
  - `all` (array of child predicates/conditions)
  - `any` (array of child predicates/conditions)
  - `not` (a single child predicate/condition)
  (nest nodes to express combinations).

Decision (captured from Q&A):
- `not` accepts a single child predicate/condition only (no array form).

Decision (captured from Q&A):
- `all` and `any` arrays MUST be non-empty. Empty arrays are invalid and startup MUST fail.

Decision (captured from Q&A):
- v0.01 MUST support `not` in the explicit boolean predicate form.

Decision (captured from Q&A):
- v0.01 supports both `if` and `match` as aliases for the predicate field.
- Documentation examples SHOULD use `if`.

Example (illustrative):
```json
{
  "if": {
    "any": [
      { "path": "files[*].mimeType", "eq": "app/s63" }
    ]
  },
  "then": {
    "keywords": { "add": ["exchange-set"] },
    "searchText": { "add": ["exchange set", "exchangeset"] }
  }
}
```

#### 5.4.3 Path / selectors
- The DSL MUST support referencing values from the active ingestion payload.
- Draft path rules:
  - `files[*].mimeType` resolves to the set of MIME type values across all files.
  - `properties.<name>` resolves to the value of the named ingestion property (case-insensitive name match) for simple identifier-like names.
  - `properties["<name>"]` resolves to the value of the named ingestion property (case-insensitive name match) for any property name.

Decision (captured from Q&A):
- v0.01 allows paths to reference **any field** on the active request payload (not just `files` and `properties`).
  - This implies the path language must be well-defined and validation must be able to detect invalid *path syntax* at startup.

Decision (captured from Q&A):
- A path MAY fail to resolve at evaluation time for a given request payload (missing optional fields, missing properties, etc.).
  - This MUST NOT be treated as an error.
  - Missing paths cause predicates to evaluate as non-matching and variable resolutions to be treated as null/empty (and therefore skipped in outputs).

Decision (captured from Q&A):
- Path segment matching is **case-insensitive** (e.g., `files[*].mimeType` matches `Files[*].MimeType`).

Decision (captured from Q&A):
- Collection access MUST be explicit. Authors MUST use `[*]` (or another explicit selector) when referencing a collection.
  - Example: `files[*].mimeType` is valid; `files.mimeType` is invalid.

Decision (captured from Q&A):
- v0.01 does NOT support numeric indexing into arrays (e.g., `files[0]`).
  - Only wildcard selection (`[*]`) is supported.

Decision (captured from Q&A):
- v0.01 does NOT support selector/filter syntax for arrays (e.g., `files[name=\"...\"]`).
  - To test for a value within a collection, use a wildcard path and an operator with default ANY-match semantics.
    - Example: `{ "path": "files[*].name", "eq": "some-file.ext" }`.

Decision (captured from Q&A):
- The DSL MUST support bracket notation for property names that are not safe in dot-notation (e.g., `properties["abc-def"]`, `properties["a.b"]`).

Open design notes:
- How to reference a specific file vs any file.
- How to handle multiple files and multiple properties.
- How to treat non-string property values (numbers, dates, booleans).

Decision (captured from Q&A):
- Where a path resolves to multiple values (e.g., `files[*].mimeType`), comparison operators such as `eq` default to **ANY-match** semantics (true if any resolved value matches).

Decision (captured from Q&A):
- v0.01 does NOT include explicit ALL-match operators (e.g., `allEq`).

Decision (captured from Q&A):
- `properties.<name>` resolves to a string value for comparisons and substitutions:
  - Predicate operators compare using the property value coerced to string (type-aware comparisons are out of scope for v0.01).
  - `$val` for a matched `properties.<name>` condition is the property value coerced to string.

#### 5.4.4 Operators
- The DSL SHOULD support at minimum:
  - `exists`
  - `eq` (string equality)

Decision (captured from Q&A):
- v0.01 MUST also support common string operators:
  - `contains`
  - `startsWith`
  - `endsWith`
  - `in` (membership in a provided string array)
- The DSL SHOULD be designed to allow extension for:
  - `neq`, `in`, `contains`, `startsWith`, `endsWith`
  - numeric comparisons: `gt`, `gte`, `lt`, `lte` (not supported in v0.01 if using string-only property semantics)
  - date comparisons (not supported in v0.01 if using string-only property semantics)
  - `regex` (if permitted)

Decision (captured from Q&A):
- String comparisons in predicates are **case-insensitive** and SHOULD be evaluated using invariant rules.
- Inputs SHOULD be trimmed before comparison.

Decision (captured from Q&A):
- If a `path` resolves to missing/null for a given request:
  - `exists` evaluates to `false`.
  - `eq`/`contains`/`startsWith`/`endsWith`/`in` evaluate to `false`.
  - No error is raised; the condition simply does not match.

### 5.5 Actions (v0.01)
- The DSL MUST be capable of updating any mutable part of `CanonicalDocument`, including:
  - `DocumentType`
  - `Keywords`
  - `SearchText`
  - `Content`
  - `Facets`

Decision (captured from Q&A):
- v0.01 supports **add/append only** actions for enrichment (monotonic mutations):
  - `then.keywords.add`
  - `then.searchText.add`
  - `then.content.add`
  - `then.facets.add`
  - plus `then.documentType.set` (scalar assignment)
- v0.01 does NOT include `remove`, `clear`, or `replace` operations.

Decision (captured from Q&A):
- `then.searchText.add` and `then.content.add` MUST deduplicate phrases per field (case-insensitive after normalization), so repeated phrases are appended only once.

Draft action blocks (illustrative):
- `then.keywords.add: string[]`
- `then.searchText.add: string[]` (treated as discrete phrases to append)
- `then.content.add: string[]`
- `then.facets.add: [{ "name": "facet 1", "value": "$val" }]`
- `then.facets.add: [{ "name": "facet 1", "values": ["a", "b"] }]`
- `then.documentType.set: string`

Decision (captured from Q&A):
- `then.facets.add` supports both:
  - single value entries (`value`), and
  - multi-value entries (`values` array)
  (exact mutual-exclusion/merge semantics TBD; see §11).

Decision (captured from Q&A):
- A facet entry MUST NOT specify both `value` and `values`.
- If both are present, the ruleset is invalid and startup MUST fail.

Decision (captured from Q&A):
- `then.documentType.set` supports templating/variables in the same way as other action strings.

Decision (captured from Q&A):
- `then.documentType.set` MUST resolve to exactly one value.
  - If templating/variables would yield multiple values, the ruleset is invalid and startup MUST fail.

### 5.6 Variables / value binding (v0.01)
- The DSL SHOULD support injecting values from the matched request into action values.

Draft proposal:
- Provide a small variable vocabulary:
  - `$val` = the matched leaf condition value (for single-value conditions)
  - `$path:<path>` = resolve a specific path value

Decision (captured from Q&A):
- `$val` is used only to supply action values into `CanonicalDocument` when the value is not provided as a literal in the rule (i.e., via variable substitution/templating).

Decision (captured from Q&A):
- v0.01 supports **string templating** in action values (e.g., `"facet-$val"`).

Decision (captured from Q&A):
- Literal `$` is not supported in templates in v0.01 (no escape mechanism). Any `$` sequence is treated as a variable reference.

Decision (captured from Q&A):
- When a predicate match is driven by a multi-value path, `$val` represents **all matched values** (a list).
- Action application MUST define how lists are expanded into:
  - `keywords.add`
  - `searchText.add`
  - `content.add`
  - `facets.add.value`

Decision (captured from Q&A):
- If a variable resolves to multiple values:
  - For multi-valued targets (`then.keywords.add`, `then.searchText.add`, `then.content.add`, and `then.facets.add`), the engine MUST apply each value (subject to existing normalization/dedupe rules).
  - For scalar targets (`then.documentType.set`), the ruleset is invalid and startup MUST fail (see §5.5).

Decision (captured from Q&A):
- When a list-valued variable (e.g., `$val`) is used inside a templated string, the result is **expanded**: one output string per list element.

Decision (captured from Q&A):
- `$path:<path>` follows the same multi-value semantics as `$val`:
  - if the referenced path resolves to multiple values, it is treated as a list and expanded using the same rules.

Decision (captured from Q&A):
- If a variable resolves to null/empty at evaluation time, the engine MUST skip the produced value (i.e., do not add an empty keyword/facet/searchText/content entry).

Decision (captured from Q&A):
- Unknown variables are treated as missing at evaluation time (equivalent to null/empty) and therefore cause the produced value to be skipped.

### 5.7 Final v0.01 DSL summary (normative)

#### 5.7.1 Ruleset
```json
{
  "schemaVersion": "1.0",
  "rules": {
    "file-share": [ /* Rule[] */ ]
  }
}
```

#### 5.7.2 Rule
```json
{
  "id": "string",
  "description": "string (optional)",
  "enabled": true,
  "if": { /* Predicate */ },
  "then": { /* Actions */ }
}
```

Notes:
- `enabled` defaults to `true` when omitted.
- `match` MAY be used as an alias for `if`.

#### 5.7.3 Predicate
Predicate is one of:

1) Shorthand AND-only:
```json
{ "<path>": "<string>" }
```
All entries are ANDed and imply `eq`.

2) Explicit boolean:
```json
{ "all": [ /* Predicate|Leaf[] */ ] }
{ "any": [ /* Predicate|Leaf[] */ ] }
{ "not": { /* Predicate|Leaf */ } }
```

3) Leaf condition:
```json
{ "path": "<path>", "eq": "<string>" }
{ "path": "<path>", "exists": true }
{ "path": "<path>", "contains": "<string>" }
{ "path": "<path>", "startsWith": "<string>" }
{ "path": "<path>", "endsWith": "<string>" }
{ "path": "<path>", "in": ["a", "b"] }
```

#### 5.7.4 Actions (`then`)
```json
{
  "keywords": { "add": ["..."] },
  "searchText": { "add": ["..."] },
  "content": { "add": ["..."] },
  "facets": {
    "add": [
      { "name": "facet name", "value": "..." },
      { "name": "facet name", "values": ["...", "..."] }
    ]
  },
  "documentType": { "set": "..." }
}
```

### 5.8 End-to-end example (implements the 3 provided sample rules)

```json
{
  "schemaVersion": "1.0",
  "rules": {
    "file-share": [
      {
        "id": "mime-app-s63",
        "description": "When any file is app/s63, enrich as exchange set",
        "enabled": true,
        "if": {
          "files[*].mimeType": "app/s63"
        },
        "then": {
          "keywords": { "add": ["exchange-set"] },
          "searchText": { "add": ["exchange set", "exchangeset"] }
        }
      },
      {
        "id": "prop-abcdef-keywords",
        "description": "When properties.abcdef equals 'a value', add key1/key2",
        "enabled": true,
        "if": {
          "properties[\"abcdef\"]": "a value"
        },
        "then": {
          "keywords": { "add": ["key1", "key2"] }
        }
      },
      {
        "id": "prop-abcdef-facet",
        "description": "When properties.abcdef exists, add facet 1 with that value",
        "enabled": true,
        "if": {
          "all": [
            { "path": "properties[\"abcdef\"]", "exists": true }
          ]
        },
        "then": {
          "facets": {
            "add": [
              { "name": "facet 1", "value": "$path:properties[\"abcdef\"]" }
            ]
          }
        }
      }
    ]
  }
}
```

## 6. Validation rules
- Rule evaluation MUST be deterministic and bounded.
- Actions that add tokens MUST ignore null/empty/whitespace values.
- Any added/derived values MUST be normalized according to existing `CanonicalDocument` logic (lowercase invariant, trimming).

## 7. Compatibility and versioning
- The rules file is a contract and MUST be versioned.

Decision (captured from Q&A):
- The DSL uses a required top-level `schemaVersion` field; v0.01 uses `"1.0"`.

## 8. Acceptance criteria
- A rules file can express the three example rules described in the work package.
- Given an `IngestionRequest` + `CanonicalDocument`, the engine updates the document as specified by matching rules.
- Rules are loaded and validated at ingestion service startup.
- A complete and comprehensive automated test suite exists for this engine (see §9) and is passing.

## 9. Testing strategy
- This spec is not complete until a complete and comprehensive automated test suite exists for the engine and is passing.

- Unit tests for:
  - Rule parsing/validation.
  - Predicate evaluation for files/properties.
  - Action application to `CanonicalDocument`.
  - Variable binding (e.g., `$val`).
  - Provider scoping (only rules under the active `providerName` are evaluated/applied).
  - Fail-fast validation (invalid JSON, unsupported `schemaVersion`, invalid predicate shapes, invalid path syntax, invalid facet entries, invalid scalar/list expansion for `then.documentType.set`).

## 10. Implementation notes (non-normative)
- Prefer `System.Text.Json` for parsing.
- Consider compiling rules into an internal immutable form for fast evaluation.
- Consider an enricher implementation (`IIngestionEnricher`) to integrate into the existing ingestion enrichment pipeline.
- The rules engine code should live in shared ingestion infrastructure (e.g., `UKHO.Search.Infrastructure.Ingestion`) and be referenced by the ingestion host; provider projects should only contribute provider-specific data providers and pipeline nodes.

## 11. Future considerations (out of scope for v0.01)
Decision (captured from Q&A):
- The following capabilities are explicitly out of scope for v0.01 and MAY be considered in future versions:
  - typed comparisons for ingestion properties (numeric/date)
  - non-fail-fast loading modes (warn-and-skip)
