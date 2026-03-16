# 034 - Ingestion rule parsing operators (string → int)

**Target output path:** `docs/034-ingestion-rule-parsing-operators/spec.md`

## 1. Introduction

### 1.1 Purpose

This specification introduces **explicit parsing operators** to the ingestion rules DSL to support safe conversion from **string values** (e.g. `$val` produced by wildcard matches) to **typed values**, with an initial focus on converting to integers.

This is required because `CanonicalDocument` maintains numeric taxonomy fields as integer sets:

- `MajorVersion: SortedSet<int>`
- `MinorVersion: SortedSet<int>`

Rules frequently populate these fields from `$val` in file-share rules, but `$val` is always a **string**.

### 1.2 Scope

In scope:

- Define a new **explicit** rule DSL operator for integer parsing.
- Define runtime behavior when parsing succeeds or fails.
- Define error handling and logging behavior.
- Define rule file examples.
- Define a comprehensive test plan.

Out of scope:

- Adding parsing operators for types other than `int` (e.g. decimal, DateTime) in this work item.
- Changing the `CanonicalDocument` data model.
- Migrating existing rules.

### 1.3 Background / current behavior

- Rule inputs like `$val` originate from runtime payload data (e.g. filename-derived tokens, wildcard matches).
- The ingestion rules engine currently applies typed actions to `CanonicalDocument` fields.
- Without parsing support, rules cannot reliably use `$val` to populate integer fields.

## 2. Goals and non-goals

### 2.1 Goals

1. Add an explicit operator in the rules DSL: **`toInt(...)`**.
2. Enable rules to populate integer actions (`majorVersion`, `minorVersion`) from `$val` safely.
3. Default behavior when parsing fails: **do not add anything** (non-fatal).
4. Parsing failures must not cause a rule load failure (schema is valid) and must not fail ingestion.
5. Provide clear, testable semantics for trimming, culture/format, and out-of-range cases.

### 2.2 Non-goals

- No implicit type conversion (no “magic” conversion of string → int).
- No changes to how predicates are evaluated.
- No changes to rule file layout (`Rules/<provider>/**/*.json`).

## 3. Proposed solution overview

### 3.1 Operator definition

Introduce a DSL expression function:

- `toInt(value)`

Where `value` resolves to a string (e.g. `$val`, `$path:...`) at runtime.

### 3.2 Where it is supported

- `toInt(...)` MUST be supported wherever a rule `then` action currently accepts values.
- In this work item, the primary requirement is for:
  - `then.majorVersion.add[]`
  - `then.minorVersion.add[]`

### 3.3 Runtime resolution semantics

At runtime, when expanding `then` action values:

- The engine MUST resolve variables (e.g. `$val`, `$path:...`) first.
- The engine MUST then evaluate `toInt(...)` if the action expects integers.

### 3.4 Parse rules

When evaluating `toInt(value)`:

- Input MUST be treated as a string.
- Leading/trailing whitespace MUST be trimmed.
- Parsing MUST use invariant culture.
- Accepted format MUST be base-10 integer (optionally with leading `+`/`-`).
- If parsing fails for any reason (null/empty/whitespace, non-numeric, overflow), the evaluation MUST yield **no output value**.

### 3.5 Failure behavior

If `toInt(...)` fails for a given candidate value:

- The engine MUST NOT add anything to the target integer field(s).
- The engine MUST continue processing other values/actions/rules.
- The engine SHOULD log at `Debug` (or `Information` if operationally preferred) including:
  - rule id
  - provider
  - action name
  - original value (safe to log)
  - reason (e.g. invalid format / overflow)

Parsing failures MUST NOT:

- Fail rule loading
- Fail ruleset validation
- Fail ingestion requests

## 4. Functional requirements

### 4.1 Rules DSL additions

- Add `toInt()` function available in `then` action value expansion.
- `toInt()` MUST be explicit (there is no implicit conversion of string values).

### 4.2 Determinism

Given the same input payload and rule set, the parsed integer outputs MUST be deterministic.

### 4.3 Examples

#### 4.3.1 Parse `$val` into `majorVersion`

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "parse-major-version",
    "if": { "files[*].path": "*.000" },
    "then": {
      "majorVersion": {
        "add": [ "toInt($val)" ]
      }
    }
  }
}
```

#### 4.3.2 Multiple values → only valid ints are added

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "parse-minor-version",
    "if": { "id": "doc-1" },
    "then": {
      "minorVersion": {
        "add": [ "toInt(10)", "toInt( 02 )", "toInt(not-a-number)" ]
      }
    }
  }
}
```

Expected:

- `10` and `2` are added
- `not-a-number` is ignored

## 5. Technical requirements

### 5.1 Integration points

- Parsing operator evaluation SHOULD be implemented within the existing templating/value-expansion stage used by `then` actions, so behavior is consistent for all providers.

### 5.2 Logging

- Parsing failures SHOULD be logged at `Debug`.
- Logging MUST be structured and include rule id and action name.

### 5.3 Error handling

- Parsing failures are expected at runtime because `$val` can be user/content-driven.
- The engine MUST treat these as non-fatal and skip the invalid value.

## 6. Testing strategy (comprehensive)

This work item MUST include unit and integration tests.

### 6.1 Unit tests (operator evaluation)

Create unit tests for `toInt(...)` operator evaluation:

1. **Valid integer strings**
   - Input: `"0"`, `"1"`, `"-1"`, `"+2"`
   - Expect: outputs ints 0, 1, -1, 2

2. **Whitespace trimming**
   - Input: `"  42  "`
   - Expect: 42

3. **Empty/null/whitespace input**
   - Input: `null`, `""`, `"   "`
   - Expect: no output value

4. **Non-numeric input**
   - Input: `"abc"`, `"1.2"`, `"1,000"` (comma)
   - Expect: no output value

5. **Overflow / out of range**
   - Input: very large string above `int.MaxValue`
   - Expect: no output value

6. **Invariant culture enforcement**
   - Force a culture with different number formats and ensure the same results.

7. **Idempotence/determinism**
   - Same input yields same output each time.

### 6.2 Unit tests (then action application)

Add tests around action application:

1. **MajorVersion add with valid parsed values**
   - Rule adds `toInt($val)` where `$val` resolves to `"2"`
   - Expect: `MajorVersion == {2}`

2. **MinorVersion add when parsing fails**
   - `$val == "abc"`
   - Expect: `MinorVersion` unchanged
   - Ensure no exception is thrown

3. **Mixed add list**
   - Values: `toInt("1")`, `toInt("abc")`, `toInt("2")`
   - Expect: `{1,2}`

4. **Deduping and sorting unaffected**
   - Add `toInt("2")`, `toInt("1")`, `toInt("2")`
   - Expect: `{1,2}`

### 6.3 Integration tests (end-to-end rules engine)

Add rules engine integration tests exercising `$val` from file-share provider contexts:

1. **Wildcard match produces `$val` and is parsed**
   - Construct a payload that triggers wildcard match producing a numeric `$val`
   - Expect integer field populated

2. **Wildcard match produces non-numeric `$val`**
   - Expect integer field not populated, rule engine continues

3. **Multiple rules with mix of success/failure**
   - Ensure failure in one rule/value does not prevent others

### 6.4 Logging tests (optional, but recommended)

If the codebase has test patterns for logging:

- Verify a parse failure emits a structured log with rule id and action

## 7. Acceptance criteria

1. A rule author can write `toInt($val)` inside integer actions (`majorVersion`, `minorVersion`).
2. When parsing succeeds, integer values are added to `CanonicalDocument`.
3. When parsing fails, nothing is added and ingestion continues.
4. Parsing failures do not fail ruleset load or ingestion pipeline.
5. Tests cover success cases, failure cases, trimming, overflow, and mixed lists.

6. `docs/ingestion-rules.md` is updated to:
   - reflect the latest per-rule JSON file format and directory layout (`Rules/<provider>/**/*.json`)
   - remove references to `ingestion-rules.json` as the primary configuration mechanism
   - include a new section documenting parsing operators (starting with `toInt(...)`) with comprehensive examples and exact semantics.

## 8. Open questions

1. Should `toInt(10)` accept numeric literals (non-strings) or only strings? (Spec allows either as long as it resolves to a string representation.)
2. Logging level for parse failures: Debug vs Information.

## 9. Documentation updates required

### 9.1 Update `docs/ingestion-rules.md`

The ingestion rules guide (`docs/ingestion-rules.md`) MUST be updated as part of this work item.

It MUST:

1. **Describe the new storage format**
   - Rules are stored as individual JSON files under `Rules/<provider>/...`.
   - Providers are discovered by directory name under `Rules/`.
   - All subdirectories under a provider are scanned.

2. **Describe the per-rule JSON file schema**
   - Top-level `schemaVersion: "1.0"` is required.
   - Top-level `rule: { ... }` is required and contains the existing rule schema.
   - Clarify that the rule filename does not need to match the rule id.

3. **Document parsing operators**
   - Add a dedicated section covering `toInt(value)`:
     - What inputs are accepted
     - How trimming works
     - Invariant culture behavior
     - Failure mode: parse failure produces no output and is not fatal
     - How it interacts with `$val` / `$path:` expansions

4. **Include comprehensive examples**
   - At minimum:
     - Parsing `$val` into `majorVersion.add`
     - Mixed values where only some parse successfully
     - Demonstrating skip behavior when `$val` is not numeric
     - Demonstrating that rules continue to apply even when parsing fails

