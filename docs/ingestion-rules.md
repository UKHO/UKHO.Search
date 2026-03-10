# Ingestion rules guide (`ingestion-rules.json`)

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

## 2. Where the rules file lives

The host looks for a file named `ingestion-rules.json` in the host content root.

In this repository the committed rules file is at:

- `src/Hosts/IngestionServiceHost/ingestion-rules.json`

The host project copies it to output and the ingestion service validates it during startup.

---

## 3. Top-level file schema

The rules file must be valid JSON and must have this shape:

```json
{
  "schemaVersion": "1.0",
  "rules": {
    "<providerName>": [ /* Rule[] */ ]
  }
}
```

### 3.1 `schemaVersion`

- Required.
- Must equal exactly `"1.0"`.

### 3.2 `rules`

- Required.
- Must be an object where:
  - each property name is a provider name (for example `"file-share"`)
  - each value is a **non-empty array** of rules for that provider

Fail-fast behavior:

- Missing `ingestion-rules.json` fails startup.
- A file with **no providers** or **only empty provider arrays** fails startup.

---

## 4. Provider scoping

Rules are scoped by provider name.

- When the engine is invoked with `providerName = "file-share"`, only `rules["file-share"]` is evaluated/applied.
- Provider key matching is case-insensitive.

---

## 5. Rule schema

A rule is a JSON object with these fields:

```json
{
  "id": "string",
  "description": "string (optional)",
  "enabled": true,
  "if": { /* predicate */ },
  "then": { /* actions */ }
}
```

### 5.1 `id`

- Required.
- Must be non-empty.
- Must be unique **within a provider’s rule array**.

### 5.2 `description` (optional)

Free-text description for maintainability.

### 5.3 `enabled` (optional)

- Optional boolean.
- Defaults to `true`.
- If `false`, the rule is skipped.

### 5.4 `if` vs `match`

Rules must contain **exactly one** predicate block, either:

- `if` (preferred), or
- `match` (alias)

You must not specify both.

---

## 6. Predicates (matching conditions)

A predicate is evaluated against the **active payload**:

- `request.AddItem` if present
- otherwise `request.UpdateItem` if present

If neither is present (for example `DeleteItem` / `UpdateAcl`), the rules engine performs no mutations.

### 6.1 Predicate forms

There are two supported predicate forms:

1. **Shorthand AND-only form** (simple equality)
2. **Explicit boolean form** (`all` / `any` / `not`), including leaf conditions

---

## 7. Shorthand AND-only predicate form

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

## 8. Explicit boolean predicate form

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

## 9. Paths

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

Meaning:

- Matches if any resolved value is non-empty.

`$val` binding:

- Includes all resolved non-empty values.

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
  "documentType": { "set": "..." }
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

### 11.5 `facets.add`

Adds facets to `CanonicalDocument.Facets`.

Each entry must have:

- `name`: string or template
- either:
  - `value`: string/template, or
  - `values`: array of string/templates

Rules:

- A facet entry must not specify both `value` and `values`.
- Facet names and values are normalized and deduplicated.

### 11.6 `documentType.set`

Sets `CanonicalDocument.DocumentType`.

- The produced result must be **exactly one value**.
  - If it produces **zero values**, document type is not set.
  - If it could produce **multiple values**, the ruleset is rejected at startup.

Startup scalar-safety rules:

- `$path:` in `documentType.set` must not reference a wildcard path containing `[*]`.
- `$val` in `documentType.set` is only allowed when the predicate is a **single leaf** and the leaf path does not contain `[*]`.

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

## 13. Complete example (spec §5.8)

This example shows three rules for provider `file-share`:

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

---

## 14. Troubleshooting and common validation errors

### 14.1 Startup fails: missing rules file

- Ensure `ingestion-rules.json` is present at the host content root.

### 14.2 Startup fails: empty rules

- Ensure at least one provider has at least one rule.

### 14.3 Path validation errors

Common causes:

- Missing wildcard when traversing a collection:
  - invalid: `files.mimeType`
  - valid: `files[*].mimeType`
- Numeric index:
  - invalid: `files[0].mimeType`
- Selector/filter syntax:
  - invalid: `files[name=\"x\"].mimeType`

### 14.4 Predicate shape errors

- `all` / `any` arrays must be non-empty.
- `not` must be an object, not an array.
- Leaf must specify exactly one operator.

### 14.5 Facet entry errors

- A facet entry must not contain both `value` and `values`.

### 14.6 DocumentType scalar-safety errors

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
