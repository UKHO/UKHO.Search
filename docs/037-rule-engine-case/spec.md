# Specification: Case-insensitive ingestion rule engine (037-rule-engine-case)

Target output path: `docs/037-rule-engine-case/spec.md`

Generated: 2026-03-16

## 1. Overview

### 1.1 Goal
Make **all operations** in the ingestion rule engine **case-insensitive** in a consistent, well-defined way across:

- **Property name look-ups** (e.g. `properties["Week Number"]` vs `properties["week number"]`)
- **Predicate string matching/evaluation** (e.g. `eq`, `contains`, `startsWith`, `endsWith`, `in`)
- **Template variable resolution** (e.g. `$path:properties["Year"]`)

The behavior must be identical in:

- `IngestionServiceHost` (runtime ingestion)
- `RulesWorkbench` (interactive evaluation)

### 1.2 Non-goals
- No changes to the on-disk rule JSON format are required.
- No changes to schema version (`schemaVersion` remains `"1.0"`).
- No change to supported predicate operators beyond case-insensitivity.

Additional constraint (in-scope):
- The ingestion request model must not allow multiple ingestion properties whose names differ only by case (e.g. `"Week Number"` and `"week number"`). This must be enforced via a dedicated collection type (see §3.4).

### 1.3 Background / problem statement
Currently, rules can validate successfully but produce unexpected blank outputs when the incoming payload uses different casing for property names. Example:

- Rule references `$path:properties["week number"]`
- Payload provides `properties["Week Number"]`

This yields missing values at evaluation time.

Additionally, predicate evaluation operators should be robust to casing differences in both payload and rule definitions.

### 1.4 Key design decision
The ingestion rule engine will be made case-insensitive using **ordinal**, culture-invariant behavior:

- **Key comparison**: `StringComparer.OrdinalIgnoreCase`
- **String operations**: `StringComparison.OrdinalIgnoreCase`

This avoids culture-specific casing anomalies.

### 1.5 Collision policy (keys differing only by case)
A payload may contain both `properties["Week Number"]` and `properties["week number"]`. Case-insensitive lookup makes these ambiguous.

Revised constraint:
- With the introduction of `IngestionPropertyList` (§3.4), ingestion property names are constrained to be **unique case-insensitively** at the point of addition.
- Additionally, the ingestion pipeline will constrain/normalize property names to **lowercase**.

Therefore, the rule engine must not attempt multi-case matching for ingestion properties; it should resolve against the canonical lowercase form only (see §3.1).

---

## 2. Scope and affected components

### 2.1 Components requiring changes
- Ingestion rule evaluation (predicate evaluation and template expansion) library under `src/UKHO.Search.Infrastructure.Ingestion/Rules/**`
- Rules loading/validation as needed (only insofar as validator must accept case variants of operator names; operator names are already case-insensitive)
- `RulesWorkbench` evaluation pipeline (must use the same engine code paths as host; if any workbench-specific resolver exists, it must be updated)

### 2.2 Operations that must become case-insensitive

#### 2.2.1 Property name lookups (path resolution)
- Any Json path resolution for objects and `properties[...]` indexing must treat property names case-insensitively per collision policy.
- Applies to:
  - predicates (`if` / `match`) leaf `path`
  - templates (`$path:...`)
  - any helpers used for `ExtractPathVariables` or derived evaluation

#### 2.2.2 Operator evaluation
All string matching operators must be case-insensitive:

- `eq`
- `contains`
- `startsWith`
- `endsWith`
- `in`

Notes:
- `exists` is unaffected by casing of values, but the underlying path lookup must be case-insensitive.

#### 2.2.3 Template evaluation helpers
- `toInt(...)` remains invariant culture for numeric parsing.
- All `$path:` expansions must resolve paths case-insensitively.

---

## 3. Functional requirements

### 3.1 Property lookup
1. When resolving a path segment targeting an object property, the engine must:
   - normalize the requested property key to **lowercase invariant**
   - perform an **exact** lookup using that lowercase key
   - treat as missing if the lowercase key is not present.

2. Path validation syntax remains unchanged.

### 3.2 Predicate evaluation
1. `eq` must compare strings using `OrdinalIgnoreCase`.
2. `contains`, `startsWith`, `endsWith` must use `OrdinalIgnoreCase`.
3. `in` must match payload string against candidate list using `OrdinalIgnoreCase`.
4. Non-string leaf operand types:
   - existing behavior must be retained unless specified elsewhere.

### 3.3 Then/action value resolution
1. Template values using `$path:` must retrieve values case-insensitively.
2. If a `$path:` resolves to missing due to absence or ambiguity, the corresponding output must be skipped.

### 3.4 Ingestion property collection must enforce case-insensitive uniqueness

#### 3.4.1 Rationale
The ingestion rule engine can only be made reliably case-insensitive for property lookups if the ingestion payload model prevents ambiguous keys differing only by case.

The ingestion request model already treats property names as case-insensitive (and rejects duplicates at validation time). This requirement must be **encapsulated** so all property addition routes enforce the same rule.

#### 3.4.2 Requirements
1. Introduce a new type `IngestionPropertyList` in `UKHO.Search.Ingestion.Requests`.
2. `IngestionPropertyList` must be the sole way to add ingestion properties in code (i.e., it owns the mutation/add APIs).
3. `IngestionPropertyList.Add(...)` (or equivalent) must reject inserting a property when another property already exists with the same name under `StringComparer.OrdinalIgnoreCase`.
   - Error message should remain aligned with existing behavior (e.g. “Names are case-insensitive.”).
4. `UKHO.Search.Ingestion.Requests.IndexRequest` must be updated to accept and expose properties via `IngestionPropertyList` rather than a raw `IReadOnlyList<IngestionProperty>`.
   - JSON serialization/deserialization of `IndexRequest.Properties` must remain compatible with the existing wire format (an array of ingestion property objects).
5. All construction paths that currently build `List<IngestionProperty>` (e.g. file-share emulator factories) must be updated to build an `IngestionPropertyList` instead.
6. Any downstream consumers (including `UKHO.Search.Ingestion.Pipeline.Documents.CanonicalDocument`) must be updated accordingly.
7. Tests must be updated/added to ensure:
   - adding `"Year"` then `"year"` throws
   - adding `"Week Number"` then `"week number"` throws
   - adding distinct names differing by more than case succeeds

8. Introducing `IngestionPropertyList` must **not change** the JSON shape of `UKHO.Search.Ingestion.Pipeline.Documents.CanonicalDocument`.
   - `CanonicalDocument.Source.Properties` must continue to serialize as a JSON array of ingestion property objects using the existing contract name `"Properties"`.
   - No wrapper object, discriminator, or additional nesting must be introduced.
   - Add/extend serialization round-trip tests to assert backward compatibility for `CanonicalDocument` JSON.

#### 3.4.3 Out-of-scope behaviors
- The rule engine is not required to implement key-collision handling for ingestion properties because ingestion property names are constrained to be lowercase and unique case-insensitively via `IngestionPropertyList`.

---

## 4. Technical requirements

### 4.1 Shared engine behavior between host and workbench
- `IngestionServiceHost` and `RulesWorkbench` must both use the same ingestion rules evaluation implementation.
- If the workbench has any duplicated path resolution or predicate logic, it must be replaced/refactored to call the shared library.

### 4.2 Diagnostics
- When ambiguous lookup occurs (same key differing only by case), evaluation should:
  - not throw
  - produce no derived output for that resolution
  - issue a diagnostic (e.g. `ILogger` warning in host; UI-visible diagnostic in workbench if supported)

---

## 5. Test strategy and acceptance criteria

### 5.1 Unit test coverage (required)
New tests must be added to thoroughly exercise all combinations of casing for:

- path property names
- predicate operator matching
- `then` `$path:` template resolution

Additionally, tests must validate the ingestion request model enforces case-insensitive uniqueness of ingestion property names (see §3.4).

#### 5.1.1 Property lookup combinator coverage
For each of the following property keys:
- `Year`
- `Week Number`

Tests must cover:
- rule uses `Year`, payload uses `Year`
- rule uses `Year`, payload uses `year`
- rule uses `year`, payload uses `Year`
- rule uses `year`, payload uses `year`
- rule uses mixed case (e.g. `yEaR`), payload uses mixed case (e.g. `YeAr`)

Equivalent matrix for `Week Number`.

#### 5.1.2 Predicate matching combinator coverage
For each operator: `eq`, `contains`, `startsWith`, `endsWith`, `in`

Tests must cover:
- rule value lower, payload value upper
- rule value upper, payload value lower
- mixed case variants

Example set for `eq`:
- rule expects `"avcsdata"`, payload provides `"AVCSDATA"` => match

#### 5.1.3 Collision/ambiguity tests
At minimum:
- payload includes both `properties["Year"]` and `properties["year"]` with different values
- rule references `properties["yEaR"]`

Expected:
- exact-case miss => case-insensitive match is ambiguous => treat as missing => output skipped

### 5.2 RulesWorkbench regression tests
- Extend `RulesWorkbench.Tests` to run evaluation with representative payload fragments.
- Ensure identical results between:
  - workbench execution path
  - engine evaluation API

### 5.3 Acceptance criteria
1. Rules referencing properties with any casing produce the same results as long as the logical key matches.
2. Predicates match regardless of casing of payload string values.
3. RulesWorkbench and IngestionServiceHost produce identical evaluation results.
4. Collision behavior is deterministic per policy.
5. All new unit tests pass and cover the full casing matrix described above.
6. Ingestion property names are case-insensitively unique at the point of addition (via `IngestionPropertyList`), preventing ambiguous duplicates differing only by case.

---

## 6. Implementation notes (non-binding)

Likely implementation approach:
- Introduce a shared helper for object-property lookup on `JsonElement` with:
  - `TryGetPropertyExact`
  - `TryGetPropertyIgnoreCaseSingle`
  - collision detection

- Ensure all operator evaluation uses ordinal ignore-case comparisons.

- Ensure `$path:` resolver uses the same case-insensitive lookup routine.

---

## 7. Changes required in each project

### 7.1 IngestionServiceHost
- Must consume and apply the updated ingestion rules engine behavior.
- Any diagnostics should use `ILogger`.

### 7.2 RulesWorkbench
- Must evaluate rules using the updated engine behavior.
- Any displayed evaluation should reflect the same casing rules.

---

## 8. Risks / trade-offs

- Payloads with keys that differ only by case become ambiguous; collision policy avoids non-determinism but may skip outputs.
- Case-insensitive matching can cause additional rule matches compared to previous behavior; ensure tests reflect intended outcomes.
- Performance overhead of case-insensitive lookup is expected to be low, but collision detection incurs additional scanning.
