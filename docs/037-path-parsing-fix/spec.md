# Work Package 037 – `$path:` argument parsing fix (spaces in `properties["..."]`)

**Target path:** `docs/037-path-parsing-fix/spec.md`

## 1. Overview

### 1.1 Purpose
The ingestion rules DSL supports templates such as `toInt($path:properties["week number"])` to extract values from the ingestion payload and project them onto canonical document fields.

A defect exists in the current `$path:` variable parsing implementation: it terminates the `$path:` argument at the first whitespace character. This prevents rules from referencing property keys that include spaces (e.g., `"week number"`) even though the underlying path resolver supports `properties["..."]` lookups with spaces.

This work package specifies a change to the rules templating layer to correctly parse `$path:` arguments containing spaces inside bracketed segments (and, more generally, to parse the argument to the end-of-path for the surrounding expression), thereby restoring expected rule behavior while preserving the existing DSL semantics.

### 1.2 Problem statement
Given a rule fragment:

- `"minorVersion": { "add": [ "toInt($path:properties[\"week number\"])" ] }`

…and an input payload where the `properties` collection includes a property named `"week number"` with value `"10"`, the canonical document should include:

- `"MinorVersion": [10]`

However, due to `$path:` argument tokenization stopping at whitespace, the argument is truncated (e.g., `properties["week`) and path resolution returns no values. As a result, `MinorVersion` remains empty.

### 1.3 Scope
In scope:
- Update the ingestion rules template expansion/parsing logic so `$path:` arguments are not prematurely terminated by whitespace when the whitespace occurs inside syntactically valid segments such as `properties["..."]`.
- Ensure `toInt(...)` calls can consume `$path:` expressions with spaces in bracketed lookups.
- Add/adjust tests to cover the regression scenario and validate the intended parsing behavior.

Out of scope:
- Any normalization/rewriting of input property names (e.g., stripping spaces) or changes to ingestion payload shape.
- Changes to the path resolver semantics (`IngestionRulesPathResolver`) beyond what is required to support correct parsing in the templating layer.
- Broader expression language additions beyond what is needed for robust `$path:` argument parsing.

### 1.4 Users and stakeholders
- Search ingestion pipeline owners.
- Rule authors (engineers/ops creating or maintaining rules JSON).
- Downstream consumers relying on populated canonical document facets (`MajorVersion`, `MinorVersion`, etc.).

### 1.5 Assumptions
- Property names in ingestion payloads can legitimately contain spaces and should remain supported.
- The `$path:` template variable is intended to accept full path expressions that are already validated at startup.
- Current behavior of variables other than `$path:` (e.g., `$val`) should remain unchanged.

## 2. Current system summary

### 2.1 Relevant components
- `IngestionRulesTemplateExpander` (templating / variable expansion)
  - Locates template variables such as `$path:<argument>`.
  - For integer actions, supports `toInt(<argumentTemplate>)` and then expands `<argumentTemplate>`.
- `IngestionRulesPathResolver` (path evaluation)
  - Supports `properties["<name>"]` lookups, lower-casing lookup keys.
  - Can resolve property names with spaces if given the full key.

### 2.2 Current behavior
- `$path:` argument parsing is implemented as: “consume characters until whitespace or `$`”.
- As a result, `$path:properties["week number"]` is truncated to `$path:properties["week`.

## 3. Proposed change (Option 1)

### 3.1 High-level solution
Change `$path:` argument parsing so that it can include whitespace characters *when those whitespace characters occur within a bracketed lookup expression* (e.g., inside `properties["..."]`).

Additionally, when `$path:` is used inside a function-style template (currently `toInt(...)`), parse the argument up to the expected delimiter (e.g., the closing `)` of `toInt(...)`) rather than stopping at whitespace.

This keeps the DSL expressive, matches existing path resolver capabilities, and avoids introducing breaking data normalization changes.

### 3.2 Parsing rule requirements
The `$path:` argument parser must:

1. Accept paths containing bracket segments (`[...]`) and quoted strings within brackets that may include spaces.
2. Not treat whitespace as a terminator while inside:
   - a bracket segment (`[...]`), and/or
   - a quoted string within a bracket segment (e.g., `"week number"`).
3. Continue to treat whitespace as a terminator *only* when not inside any bracket/quoted context.
4. Terminate on `$` as before (to allow more than one variable in a template string).
5. Be resilient to malformed templates (do not throw; treat as unresolved/empty output consistent with existing “skip derived outputs” behavior).

### 3.3 `toInt(...)` interaction
`toInt(<argumentTemplate>)` currently extracts the substring between `toInt(` and `)` and then calls `ExpandToInt` → `Expand` on the inner template.

With the proposed parser changes:
- `toInt($path:properties["week number"])` should expand `$path:...` into the string `"10"` and then parse to integer `10`.

### 3.4 Backward compatibility
- Existing templates that do not contain whitespace in `$path:` arguments continue to work unchanged.
- Existing behavior for `$val` is unchanged.
- Existing behavior for “unknown variables” remains unchanged.
- Existing behavior where missing runtime data yields no outputs remains unchanged.

### 3.5 Security and robustness considerations
- Do not interpret or execute arbitrary code.
- Limit parsing changes to template scanning; do not increase the expressive power beyond correct argument extraction.
- Avoid unbounded loops and ensure linear scans over template strings.

### 3.6 Telemetry / diagnostics (non-functional)
- Existing logging currently summarizes rule application, not parsing failures.
- This change should not introduce noisy logging for missing runtime data.
- If any diagnostics are added in the future, they must be debug-level and rate-limited to avoid log flooding when rules encounter unexpected templates.

## 4. Functional requirements

### FR1 – Expand `$path:` arguments with spaces
The system shall correctly expand templates containing `$path:` lookups where the path includes a bracketed key with spaces, e.g.:
- `$path:properties["week number"]`

### FR2 – Support `toInt($path:...)` for keys with spaces
The system shall correctly evaluate `toInt($path:properties["week number"])` to `[10]` when the resolved value is `"10"`.

### FR3 – Preserve existing `$path:` termination rules
The system shall continue to treat `$` as the start of a new variable and terminate the current `$path:` argument at `$`.

### FR4 – Non-throwing behavior
For malformed templates (e.g., unmatched brackets/quotes), the system shall not throw during rule evaluation and shall behave as “no expansions found” for the affected variable.

## 5. Non-functional requirements

### NFR1 – Performance
Template parsing remains O(n) with respect to template length.

### NFR2 – Test coverage
Add regression tests demonstrating correct behavior for property keys containing spaces. Tests must be thorough (not just the happy path) and cover:

- `$path:` with `properties["..."]` containing spaces.
- `toInt(...)` wrapping such `$path:` expressions.
- Mixed templates containing multiple variables (e.g., `$path:properties["a b"]-$path:properties["c"]`).
- Whitespace termination outside bracketed lookups (e.g., `$path:properties["a b"] $path:properties["c"]`).
- Suffix/prefix text adjacent to the variable (e.g., `prefix-$path:properties["a b"]-suffix`).
- Malformed/unbalanced bracket/quote inputs to confirm non-throwing behavior and empty-output semantics.

### NFR3 – Minimal change surface
Confine changes to the templating layer (`IngestionRulesTemplateExpander`) unless a small, clearly-related change is required elsewhere.

## 6. Technical design

### 6.1 Proposed implementation approach
Update `IngestionRulesTemplateExpander.TryFindVariable()` parsing of `$path:` arguments.

Current logic:
- Start at `argStart = i + 6`.
- Increment `argEnd` until `char.IsWhiteSpace(c) || c == '$'`.

Proposed logic (conceptual):
- Maintain parser state while scanning characters:
  - `bracketDepth` (increment on `[` decrement on `]`)
  - `inQuotes` (toggle on `"` when inside bracket context; treat escaped quotes if present)
- While scanning:
  - Always break on `$`.
  - Break on whitespace only when `bracketDepth == 0` and `inQuotes == false`.
- Extract `argument = text.Substring(argStart, argEnd - argStart)`.

Notes:
- The current DSL uses paths like `properties["key"]`; implementing just bracket-depth handling (without full quote parsing) may be sufficient if whitespace only ever appears inside the quoted key. However, quote-tracking is recommended for correctness and future-proofing.

### 6.2 Edge cases
- `$path:properties["a b"]suffix` (no whitespace outside brackets): should consume the path; the suffix remains part of the template output replacement.
- `$path:properties["a b"] $path:properties["c"]` (space between variables): first `$path:` should terminate at whitespace outside brackets, then parsing proceeds to next `$`.
- `$path:properties["unterminated]` or `$path:properties["x]`:
  - Must not throw.
  - Either consume until whitespace/`$` or end-of-string; resolution likely yields empty.

### 6.3 Files likely impacted
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/IngestionRulesTemplateExpander.cs`
- Tests (expected new/updated):
  - `test/UKHO.Search.Ingestion.Tests/Rules/...` (or nearest existing test suite for templating)

### 6.4 Acceptance tests (examples)
1. Given payload properties include `("week number", "10")`, template `toInt($path:properties["week number"])` expands to integer list `[10]`.
2. Given payload properties include `("year", "2026")`, existing template `toInt($path:properties["year"])` continues to expand to `[2026]`.
3. Given template `$path:properties["week number"]` used in a string-add action, the expanded string includes `"10"` (normalized as appropriate by existing token normalization).

## 7. Delivery plan

### 7.1 Steps
1. Add failing regression test(s) for `$path:` with spaces + `toInt(...)`.
2. Implement updated `$path:` argument scanning in `TryFindVariable()`.
3. Run unit tests.
4. Validate behavior using an example rule and payload (as documented in the defect report).

### 7.2 Rollout
- Ship as a backward-compatible change.
- No rule changes required for existing rules; rules that previously failed due to spaces will begin working.

## 8. Risks and mitigations

- **Risk:** Parser becomes too permissive and consumes unintended suffix characters.
  - **Mitigation:** Limit special handling to bracketed/quoted contexts and retain `$` as a hard terminator.

- **Risk:** Different quoting/escaping patterns exist in templates.
  - **Mitigation:** Add tests for representative patterns used in repo rules; keep parsing rules minimal and documented.

## 9. Open questions / decisions

1. Should `$path:` parsing be enhanced only for bracketed/quoted key lookups, or should it also support parentheses-delimited contexts (future functions beyond `toInt`)?
2. Do any existing rules rely on the current (buggy) whitespace-terminating behavior inside bracketed segments? (Unlikely, but worth a quick scan.)

