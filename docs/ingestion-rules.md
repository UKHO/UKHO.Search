# Ingestion rules guide (per-rule JSON files)

This document explains how to write and maintain ingestion enrichment rules for the **UKHO.Search Ingestion Rules Engine**.

It is aimed at developers, testers, and professional services users who need to create new ingestion rules or update existing ones.

> Rules are loaded and **validated at service startup**. Invalid rules fail startup (fail-fast).

---

## 1. What ingestion rules do

Ingestion rules enrich a `CanonicalDocument` using values found in an `IngestionRequest`.

At runtime the ingestion pipeline:

1. Determines a `providerName` (for example `file-share`).
2. Loads the rules scoped to that provider.
3. Selects the active payload from the request:
   - `request.AddItem` (Add)
   - `request.UpdateItem` (Update)
4. Evaluates rule predicates against that active payload.
5. Applies **all matching rules**, **in the order they appear in the JSON file** (file order).

Rules are **provider-scoped**. Rules for other providers are ignored.

---

## 2. Where the rules live

Rules are loaded from a directory named `Rules` in the host content root.

Rules are stored as **one JSON file per rule**.

In this repository, the committed rules directory for the ingestion host is at:

- `src/Hosts/IngestionServiceHost/Rules/`

The host project copies the `Rules/` directory to output and the ingestion service validates rules during startup.

---

## 3. Directory layout

The rules root directory must have this structure:

```json
Rules/
  <providerName>/
    <any-subdirectories-allowed>/
      <rule-file>.json
```

Notes:

- `<providerName>` is the provider scope (for example `file-share`).
- Any subdirectory structure is allowed below the provider directory.
- When loading rules for a provider, the engine scans **all** `.json` files under `Rules/<providerName>` recursively.

## 4. Per-rule file schema

Each rule file must be valid JSON and must have this shape:

```json
{
  "schemaVersion": "1.0",
  "rule": { /* Rule */ }
}
```

### 4.1 `schemaVersion`

- Required.
- Must equal exactly `"1.0"`.

### 4.2 `rule`

- Required.
- Must be a JSON object conforming to the rule schema described in §5.

Fail-fast behavior:

- Missing required `Rules/` directory fails startup.
- Invalid JSON or schema errors in any rule file fail startup.
- Duplicate rule ids within the same provider scope fail startup.

---

## 5. Provider scoping

Rules are scoped by provider name.

- When the engine is invoked with `providerName = "file-share"`, only rules loaded from `Rules/file-share/**.json` are evaluated/applied.
- Provider key matching is case-insensitive.

### 5.1 Rule ordering

Rules are applied in a deterministic order.

When loading rules from per-rule files under a provider directory, ordering is:

1. File path (ordinal, case-insensitive)
2. Rule id (ordinal, case-insensitive) as a tie-break

---

## 6. Rule schema

A rule is a JSON object with these fields:

```json
{
  "id": "string",
  "context": "string (optional metadata; see §6.2)",
  "description": "string (optional)",
  "enabled": true,
  "if": { /* predicate */ },
  "then": { /* actions */ }
}
```

### 6.1 `id`

- Required.
- Must be non-empty.
- Must be unique **within a provider scope** (across all files under `Rules/<providerName>`).

Rule file name:

- The file name does **not** need to match the rule id.

### 6.2 `context` (optional metadata, conditionally required for `file-share`)

- `context` is optional metadata on the rule definition.
- When supplied, it is normalized to trimmed lowercase.
- `context` is not itself a predicate and does not change predicate evaluation semantics.
- It is intended for rule classification / tooling scenarios alongside the rule definition.

Current `file-share` validation behavior:

- For providers other than `file-share`, `context` is optional.
- For `file-share`, a transitional validation rule applies:
  - if **no** `file-share` rules declare `context`, startup validation still allows the legacy all-missing state
  - once **any** `file-share` rule declares `context`, **all** `file-share` rules must declare `context`
- Missing required `context` in a partially or fully uplifted `file-share` ruleset fails startup validation.

### 6.3 `description` (optional)

Free-text description for maintainability.

### 6.4 `enabled` (optional)

- Optional boolean.
- Defaults to `true`.
- If `false`, the rule is skipped.

### 6.5 `if` vs `match`

Rules must contain **exactly one** predicate block, either:

- `if` (preferred), or
- `match` (alias)

You must not specify both.

---

## 7. Predicates (matching conditions)

A predicate is evaluated against the **active payload**:

- `request.AddItem` if present
- otherwise `request.UpdateItem` if present

If neither is present (for example `DeleteItem` / `UpdateAcl`), the rules engine performs no mutations.

### 7.1 Predicate forms

There are two supported predicate forms:

1. **Shorthand AND-only form** (simple equality)
2. **Explicit boolean form** (`all` / `any` / `not`), including leaf conditions

---

## 8. Shorthand AND-only predicate form

Shorthand form is an object mapping **paths** to **string values**.

Each entry is treated as an `eq` comparison, and **all entries must match** (logical AND).

Example:

```json
"if": {
  "properties[\"product\"]": "AVCS",
  "id": "my-document-id"
}
```

Semantics:

- Each value must be a JSON string.
- The rule matches only if every path resolves and equals the provided string.

`$val` binding for shorthand:

- `$val` is the concatenation of the **matched values** from each shorthand entry, in JSON property order.

---

## 9. Explicit boolean predicate form

Boolean nodes allow nested logic:

- `all`: AND across children
- `any`: OR across children
- `not`: negation of a single child

Rules:

- A boolean node must contain **exactly one** of `all`, `any`, or `not`.
- `all` and `any` must be non-empty arrays.
- `not` must be a single predicate object (not an array).

Example:

```json
"if": {
  "all": [
    { "path": "properties[\"abcdef\"]", "exists": true },
    { "not": { "path": "id", "eq": "blocked" } }
  ]
}
```

### 8.1 Leaf predicates

A leaf condition has the shape:

```json
{ "path": "<path>", "<operator>": <value> }
```

Rules:

- `path` is required and must be a string.
- Exactly one operator must be specified.

---

## 10. Paths

Paths identify values on the active payload (AddItem/UpdateItem).

### 9.1 Key rules

- **Case-insensitive** segment matching.
- Collection traversal must be explicit using `[*]`.
- Numeric indexes (like `[0]`) are not allowed.
- Selector/filter syntax (like `[name=...]`) is not allowed.

If a path fails to resolve at runtime (missing optional data), it is **not an error**:

- operators evaluate as non-match
- variable resolution produces no values (and the engine skips outputs)

### 9.2 Common path examples

#### 9.2.1 Files

- `files[*].mimeType`
- `files[*].filename`

`[*]` means “all elements”. Operators use **ANY-match semantics** by default (see operators section).

#### 9.2.2 Ingestion properties

Two equivalent forms are supported:

- Dot form (identifier-like names): `properties.abcdef`
- Bracket form (any name): `properties["abcdef"]`

Property name matching is case-insensitive.

### 9.3 Path validation (startup)

Paths are validated at startup for:

- syntax correctness
- allowed selector usage (only `[*]`)
- traversing collections requires `[*]`
- segments resolve via reflection on known request types

---

## 10. Operators

Operators are evaluated against the resolved values from a path.

### 10.1 Normalization

All string comparisons:

- trim whitespace
- compare using lowercased invariant form

### 10.2 ANY-match semantics

If a path resolves to multiple values (for example `files[*].mimeType`), the comparison succeeds if **any** resolved value matches.

### 10.3 Supported operators

#### 10.3.1 `exists`

Leaf shape:

```json
{ "path": "properties[\"abcdef\"]", "exists": true }
```

```json
{ "path": "properties[\"abcdef\"]", "exists": false }
```

Meaning:

- `exists: true` matches if any resolved value is non-empty.
- `exists: false` matches if no resolved value is non-empty.

For `exists`, a retained value means a resolved value that is not null, empty, or whitespace-only.

That means `exists: false` matches when:

- the path is missing at runtime
- the path resolves only to null values
- the path resolves only to empty strings
- the path resolves only to whitespace-only strings

`$val` binding:

- For `exists: true`, includes all resolved non-empty values.
- For `exists: false`, includes no values.

`exists: false` is semantically equivalent in match outcome to:

```json
{
  "not": {
    "path": "properties[\"abcdef\"]",
    "exists": true
  }
}
```

#### 10.3.2 `eq`

```json
{ "path": "files[*].mimeType", "eq": "app/s63" }
```

Matches if any resolved value equals the comparator after normalization.

`$val` binding:

- Includes only the resolved values that actually matched.

#### 10.3.3 `contains`

```json
{ "path": "files[*].mimeType", "contains": "s63" }
```

#### 10.3.4 `startsWith`

```json
{ "path": "id", "startsWith": "doc-" }
```

#### 10.3.5 `endsWith`

```json
{ "path": "id", "endsWith": "-final" }
```

#### 10.3.6 `in`

```json
{ "path": "files[*].mimeType", "in": ["app/s63", "text/plain"] }
```

- Value must be a non-empty array of strings.

### 10.4 Missing/unresolvable paths

If a path resolves to no values at runtime:

- `exists` evaluates to `false`
- all other operators evaluate to `false`

No exception is thrown.

---

## 11. Actions (`then`)

The `then` block defines monotonic enrichments:

- `keywords.add`
- `searchText.add`
- `content.add`
- `facets.add`
- `documentType.set`
- additional top-level fields (see §11.7)

Example structure:

```json
"then": {
  "keywords": { "add": ["..."] },
  "searchText": { "add": ["..."] },
  "content": { "add": ["..."] },
  "facets": {
    "add": [
      { "name": "facet name", "value": "..." },
      { "name": "facet name", "values": ["...", "..."] }
    ]
  },
  "documentType": { "set": "..." },
  "authority": { "add": ["..."] },
  "region": { "add": ["..."] },
  "format": { "add": ["..."] },
  "category": { "add": ["..."] },
  "series": { "add": ["..."] },
  "instance": { "add": ["..."] },
  "majorVersion": { "add": [1] },
  "minorVersion": { "add": [0] }
}
```

### 11.1 Normalization and skipping empty values

All action-produced strings are:

- trimmed
- lowercased invariant

Null/empty/whitespace outputs are skipped.

### 11.2 `keywords.add`

Adds keywords to `CanonicalDocument.Keywords`.

- Keyword set is naturally deduplicated.
- Values can be literal strings or templates.

### 11.3 `searchText.add`

Appends phrases to `CanonicalDocument.SearchText`.

- Values are treated as **phrases** (strings may include spaces).
- The engine deduplicates phrases per field using a boundary-aware match.
  - A phrase is considered present only if it matches as a whole phrase separated by spaces.

### 11.4 `content.add`

Same behavior as `searchText.add`, but targets `CanonicalDocument.Content`.

### 11.5 Additional top-level fields (`*.add`)

The rules engine can also add values to additional top-level set-based fields on `CanonicalDocument`.

These are **set-based**, **additive** enrichments (non-destructive):

- Adding the same value multiple times does not create duplicates.
- String outputs are normalized like other actions (trim + lowercase invariant).
- Empty/null/whitespace outputs are skipped.
- Numeric outputs are produced by parsing operators (see §13). If parsing fails, no output is produced.

Notes:

- String fields support templates/variables (see §12).
- Numeric fields (`majorVersion`, `minorVersion`) support templates/variables only when used via parsing operators (see §13).

Supported actions:

- `authority.add` (string[])
- `region.add` (string[])
- `format.add` (string[])
- `category.add` (string[])
- `series.add` (string[])
- `instance.add` (string[])
- `majorVersion.add` (number[])
- `minorVersion.add` (number[])

Example (string fields):

```json
{
  "id": "region-and-authority",
  "if": {
    "all": [
      { "path": "properties[\"region\"]", "exists": true },
      { "path": "properties[\"authority\"]", "exists": true }
    ]
  },
  "then": {
    "region": { "add": ["$path:properties[\"region\"]"] },
    "authority": { "add": ["$path:properties[\"authority\"]"] }
  }
}
```

Example (numeric fields):

```json
{
  "id": "versions",
  "if": {
    "all": [
      { "path": "properties[\"majorVersion\"]", "exists": true },
      { "path": "properties[\"minorVersion\"]", "exists": true }
    ]
  },
  "then": {
    "majorVersion": { "add": ["toInt($path:properties[\"majorVersion\"])"] },
    "minorVersion": { "add": ["toInt($path:properties[\"minorVersion\"])"] }
  }
}
```

---

## 13. Parsing operators (typed outputs)

Parsing operators convert string values (including variables like `$val`) into typed outputs required by some actions.

### 13.1 `toInt(value)`

`toInt(value)` converts a value to a base-10 integer.

Common usage is to parse `$val` into numeric fields like `majorVersion.add` and `minorVersion.add`.

#### 13.1.1 How it works (exact semantics)

When evaluating `toInt(value)`:

1. The engine resolves variables first (e.g. `$val`, `$path:...`).
2. The resolved value is treated as a string.
3. The input string is trimmed.
4. Parsing uses invariant culture.
5. Accepted format is a base-10 integer, optionally with leading `+` or `-`.

If parsing fails for any reason (null/empty/whitespace, non-numeric characters, overflow/out of range), **no value is produced**.

#### 13.1.2 Failure behavior

If `toInt(...)` fails:

- The engine **does not add anything** to the target numeric field.
- The engine continues evaluating other values/actions/rules.
- Parsing failures MUST NOT fail ingestion.

#### 13.1.3 Examples

**Example A: parse `$val` into `majorVersion`**

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "parse-major-from-val",
    "if": { "files[*].filename": "ENC-2" },
    "then": {
      "majorVersion": {
        "add": ["toInt($val)"]
      }
    }
  }
}
```

**Example B: mixed values (only valid ints are added)**

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "mixed-parse",
    "if": { "id": "doc-1" },
    "then": {
      "minorVersion": {
        "add": ["toInt(10)", "toInt( 02 )", "toInt(not-a-number)"]
      }
    }
  }
}
```

Expected:

- `10` and `2` are added.
- `not-a-number` is ignored.

**Example C: failure is non-fatal and rules still apply**

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "parse-and-continue",
    "if": { "id": "doc-1" },
    "then": {
      "majorVersion": { "add": ["toInt($path:properties[\"major\"])" ] },
      "keywords": { "add": ["version-parsed-or-not"] }
    }
  }
}
```

If `properties["major"]` is missing or not numeric, the keyword is still added while `majorVersion` remains unchanged.

---

## 12. Variables and templating

Action strings support variable substitution.

### 12.1 Variable vocabulary

- `$val`
  - Values bound from the predicate match.
- `$path:<path>`
  - Resolves values from the active payload using a path.

### 12.2 Template expansion rules

- There is no escaping for `$`.
  - Any `$...` sequence is treated as a variable.
- Unknown variables are treated as missing.
  - The produced value is skipped.
- If a variable resolves to multiple values:
  - multi-valued actions apply one output per value (`keywords`, `searchText`, `content`, `facets`)
  - scalar action (`documentType.set`) must not be able to produce multiple values

Examples:

- Whole-string `$val`:
  - if `$val` is `["a", "b"]` then the expansion is `["a", "b"]`
- Embedded `$val`:
  - `"facet-$val"` expands to `["facet-a", "facet-b"]`

### 12.3 `$val` binding details

`$val` is derived from the predicate evaluation:

- For `exists`, `$val` includes resolved non-empty values.
- For `eq`/`contains`/`startsWith`/`endsWith`/`in`, `$val` includes only values that matched.
- For `all`, `$val` concatenates matched values from each child in order.
- For shorthand predicates, `$val` concatenates matched values from each entry in JSON property order.
- For `any`, `$val` is taken from the **first matching branch** in evaluation order.

---

## 13. Complete example (per-rule file)

This example shows one complete per-rule JSON file for provider `file-share`.

Path example:

- `Rules/file-share/catalogue/mime-app-s63.json`

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "mime-app-s63",
    "context": "adds-s57",
    "description": "When any file is app/s63, enrich as exchange set",
    "enabled": true,
    "if": {
      "files[*].mimeType": "app/s63"
    },
    "then": {
      "keywords": { "add": ["exchange-set"] },
      "searchText": { "add": ["exchange set", "exchangeset"] }
    }
  }
}
```

Notes:

- Each file contains exactly one `rule` object.
- `context` is shown here because `file-share` rulesets that have started using `context` must provide it consistently across all `file-share` rules.

---

## 14. Troubleshooting and common validation errors

### 14.1 Startup fails: missing rules directory

- Ensure the `Rules/` directory is present at the host content root.
- Ensure the provider directory exists for the provider you are using (for example `Rules/file-share/`).

### 14.2 Startup fails: empty rules

- Ensure at least one provider directory contains at least one valid rule file.

### 14.3 Startup fails: missing required `context`

For provider `file-share`, once any rule file includes `context`, all `file-share` rule files must include it.

Typical remediation:

- add `context` to the missing `file-share` rule files, or
- temporarily revert the whole provider back to the legacy all-missing state if that is the intended transitional position.

### 14.4 Path validation errors

Common causes:

- Missing wildcard when traversing a collection:
  - invalid: `files.mimeType`
  - valid: `files[*].mimeType`
- Numeric index:
  - invalid: `files[0].mimeType`
- Selector/filter syntax:
  - invalid: `files[name=\"x\"].mimeType`

### 14.5 Predicate shape errors

- `all` / `any` arrays must be non-empty.
- `not` must be an object, not an array.
- Leaf must specify exactly one operator.

### 14.6 Facet entry errors

- A facet entry must not contain both `value` and `values`.

### 14.7 DocumentType scalar-safety errors

- `documentType.set` must not be able to expand to multiple values.
- `$path:` in `documentType.set` must not reference wildcard paths.
- `$val` in `documentType.set` is only allowed for a single, non-wildcard leaf predicate.

---

## 15. Authoring best practices

- Keep rules small and focused.
- Prefer shorthand predicates for simple equality checks.
- Use explicit boolean predicates when you need:
  - OR conditions (`any`)
  - NOT (`not`)
  - structured leaf operators
- Be careful when using wildcard paths:
  - they can produce multiple values
  - `$val` may become list-valued
- Use `$path:` when you want to inject a value that isn’t the matched comparison value.
- Avoid relying on `$val` from `any` when multiple branches might match; it binds from the first matching branch.

---

## 16. How to test rules changes

Recommended local checks:

- Run unit tests:
  - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
- Run the ingestion host locally:
  - `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`

When startup validation fails, review the exception message and error list; rules are validated fail-fast.
