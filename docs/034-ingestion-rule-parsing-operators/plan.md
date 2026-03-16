# Implementation Plan

**Target output path:** `docs/034-ingestion-rule-parsing-operators/plan.md`

## Project structure / organization

- Implement parsing operators inside the existing ingestion rules value expansion / templating pipeline.
- Keep the implementation in `UKHO.Search.Infrastructure.Ingestion` (rules engine lives there).
- Ensure both `IngestionServiceHost` and `RulesWorkbench` benefit automatically (shared rules engine).

Recommended code areas (confirm exact locations during implementation):

- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/*`
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/*`
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/*` (only if contract changes are needed)
- Tests:
  - `test/UKHO.Search.Ingestion.Tests/Rules/*`

## Vertical slice 1 — Add `toInt(...)` operator end-to-end for `majorVersion` / `minorVersion`

- [x] Work Item 1: Implement `toInt(...)` operator in rule value expansion and apply to int taxonomy actions - Completed
  - **Purpose**: Allow rule authors to explicitly convert string values (e.g. `$val`) to integer outputs for numeric actions, with non-fatal failure behavior (skip value).
  - **Acceptance Criteria**:
    - `toInt(value)` can be used in `then.majorVersion.add[]` and `then.minorVersion.add[]`.
    - Variables are resolved first (e.g. `$val`, `$path:`), then parsed.
    - Parse failure produces no output and does not fail ingestion.
    - Deterministic behavior (same input => same output).
    - Debug logging emitted on parse failure (rule id, provider, action, original value, reason).
  - **Definition of Done**:
    - Code implemented and wired into rules engine
    - Unit tests pass (operator evaluation + action application)
    - Integration tests pass (end-to-end rules engine)
    - Logging present and non-fatal failure behavior verified
    - Documentation updated (`docs/ingestion-rules.md` already updated in this work package)
    - Can execute end-to-end via: run ingestion rules tests and confirm parsing works
  - [x] Task 1.1: Operator parsing and evaluation - Completed
    - [x] Step 1: Add a small parser/recognizer for `toInt(<expr>)` in the value expansion stage.
    - [x] Step 2: Implement evaluation:
      - Resolve inner expression to string(s)
      - Trim
      - Parse using invariant culture
      - Return zero/one output per input value (skip on failure)
    - [x] Step 3: Ensure failures return “no outputs” rather than errors.
  - [x] Task 1.2: Wire operator into numeric taxonomy actions - Completed
    - [x] Step 1: Identify where `majorVersion` / `minorVersion` action values are read.
    - [x] Step 2: If values are currently typed-only (numbers), extend handling to also accept string templates and run through the expansion pipeline.
    - [x] Step 3: Ensure existing numeric literals still work.
  - [x] Task 1.3: Logging and diagnostics - Completed
    - [x] Step 1: Add structured debug logs on parse failure (ruleId, providerName, actionName, inputValue, errorReason).
    - [x] Step 2: Add a unit test or verify via log-capture test pattern if available (optional per spec).
  - [x] Task 1.4: Tests (comprehensive) - Completed
    - [x] Step 1: Unit tests for `toInt(...)` evaluation:
      - valid strings: `0`, `1`, `-1`, `+2`
      - whitespace trimming
      - null/empty/whitespace
      - non-numeric: `abc`, `1.2`, `1,000`
      - overflow/out of range
      - determinism
      - invariant culture enforcement
    - [x] Step 2: Unit tests for action application:
      - parsed ints are added to `CanonicalDocument.MajorVersion`/`MinorVersion`
      - failures add nothing
      - mixed list yields only valid ints
      - dedupe/sort preserved
    - [x] Step 3: Integration tests:
      - `$val` produced by matches can be parsed
      - parsing failure doesn’t stop other actions/rules
  - **Files** (expected, adjust to repo structure):
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/*`: add `toInt(...)` support
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/IngestionRulesActionApplier.cs`: numeric action value expansion/parsing integration
    - `test/UKHO.Search.Ingestion.Tests/Rules/*`: new/updated tests
  - **Work Item Dependencies**:
    - None (builds on existing rules engine)
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
    - Optionally run `IngestionServiceHost` with a rule using `toInt($val)` and observe behavior.
  - **User Instructions**:
    - No manual action required.

  **Summary (what changed)**
  - Added `toInt(...)` evaluation in `IngestionRulesTemplateExpander` via `ExpandToInt(...)` (resolve variables first, trim, invariant-culture parse, skip failures).
  - Updated integer action DTOs to support mixed numeric literals and string templates, and wired parsing into `IngestionRulesActionApplier` for `majorVersion`/`minorVersion`.
  - Added unit tests in `ToIntOperatorTests` and validated with full `UKHO.Search.Ingestion.Tests` run.

---

## Summary / key considerations

- Keep conversion explicit (`toInt(...)`) to avoid surprising rule authors.
- Treat parse failures as runtime data issues: skip value, continue processing.
- Ensure behavior is deterministic and logged at Debug for troubleshooting.
- Add comprehensive unit and integration tests as per the spec.
