# Specification: `FileShareEmulator.Common` – Canonical ingestion payload + strict SecurityTokens policy

> Work package: `docs/035-fsemualator-common/`

## 1. Purpose

When testing in `RulesWorkbench` on the `/evaluate` screen, the displayed payload currently contains `batchcreate` and `batchcreate_{businessUnitName}`, **but also shows additional tokens/keywords** that must not be present.

This specification defines a single canonical implementation for creating the ingestion message used by both:

- `tools/FileShareEmulator` (when sending to the ingestion queue)
- `tools/RulesWorkbench` (when building the payload used for evaluation)

It also defines a strict SecurityTokens policy so the two tools are guaranteed to have *absolute parity*.

## 2. Scope

### In scope

1. Enforce a strict SecurityTokens policy for evaluation/ingestion payloads.
2. Create a shared project `tools/FileShareEmulator.Common`.
3. Move ingestion message creation logic (payload + tokens) into the shared project.
4. Reference the shared project from both `tools/FileShareEmulator` and `tools/RulesWorkbench`.
5. Remove or refactor old code so only the shared implementation is used.
6. Add a comprehensive test suite in `test/FileShareEmulator.Common.Tests`.

### Out of scope

- Any change to the ingestion service/rules engine beyond consuming the payload.
- Changes to UI styling/layout in RulesWorkbench.
- End-to-end tests of the queue transport itself (unit tests for payload construction are sufficient for this scope).

## 3. Definitions

- **Business Unit Name**: the name of the business unit associated with the batch. It is only considered valid when it is **active** (as indicated by `BusinessUnit.IsActive = 1`).
- **Ingestion message / payload**: the serialized JSON message sent to the ingestion queue, containing an `IngestionRequest` with an `IndexRequest` payload (properties, security tokens, timestamp, files, etc.).
- **SecurityTokens**: the token list embedded in the `IndexRequest` (and displayed/used by RulesWorkbench).

## 4. Functional requirements

### FR-1: Required SecurityTokens only

The SecurityTokens list MUST contain **exactly** the following tokens:

1. `batchcreate` (always)
2. `batchcreate_{businessUnitName}` (only when there is an active business unit name)
3. `public` (always)

No other tokens are permitted (e.g., group identifiers, user identifiers, raw business unit name without prefix, or anything else).

### FR-2: Normalization of `{businessUnitName}`

When generating `batchcreate_{businessUnitName}`:

- Trim whitespace from the business unit name.
- Convert to lowercase using invariant culture.
- If the normalized result is empty, the token MUST NOT be added.

### FR-3: Active business unit only (decision)

The `batchcreate_{businessUnitName}` token must only be generated when the business unit is **active**.

Implementation detail:

- Retrieve `BusinessUnitName` via a query equivalent to:

  - join `Batch` -> `BusinessUnit` by `BusinessUnitId`
  - `BusinessUnit.IsActive = 1`

If no active business unit exists, then:

- the `BusinessUnitName` property in payload properties should be set to `""` (empty string)
- `batchcreate_{businessUnitName}` MUST NOT be present

### FR-4: Canonical ingestion message creation

The shared library must be the single source of truth for constructing the ingestion message.

The canonical ingestion message must:

- Include a `BusinessUnitName` property (string) in the `IndexRequest.Properties` list.
- Include `SecurityTokens` exactly per FR-1.
- Carry through batch attributes and files consistently between both tools.

## 5. Technical requirements

### TR-1: New shared project

Create project:

- `tools/FileShareEmulator.Common`

Responsibilities:

- Provide a small API to build:
  - the canonical SecurityTokens list
  - the canonical ingestion request/index request payload

Constraints:

- The shared project must be referenceable from both:
  - `tools/FileShareEmulator`
  - `tools/RulesWorkbench`

### TR-2: Remove duplicated implementations

- `tools/FileShareEmulator` must not build SecurityTokens independently.
- `tools/RulesWorkbench` must not build SecurityTokens independently.

Any prior token/payload construction logic must be deleted or refactored so it delegates to `FileShareEmulator.Common`.

### TR-3: Compatibility / serialization

The payload produced by `FileShareEmulator.Common` must remain compatible with the existing ingestion JSON contract (System.Text.Json).

## 6. Testing requirements

Create project:

- `test/FileShareEmulator.Common.Tests`

Test coverage must include:

1. Always contains `batchcreate`.
2. Always contains `public`.
3. Adds `batchcreate_{businessUnitName}` when active BU exists.
4. Normalizes BU name (trim + invariant lowercase).
5. Omits BU token when BU is null/empty/whitespace.
6. **No other tokens**: assert set equality with exactly the allowed tokens.
7. Regression tests ensuring both tools would serialize the same payload for the same inputs (unit-level, against shared API).

## 7. Acceptance criteria

1. In `RulesWorkbench` `/evaluate`, for a batch with an active business unit `X`, SecurityTokens are exactly:

   - `batchcreate`
   - `batchcreate_x`
   - `public`

2. In `RulesWorkbench` `/evaluate`, for a batch without an active business unit, SecurityTokens are exactly:

   - `batchcreate`
   - `public`

3. `FileShareEmulator` sends queue messages whose JSON payload (including SecurityTokens) matches what RulesWorkbench uses for evaluation.

4. All tests in `test/FileShareEmulator.Common.Tests` pass.

## 8. Implementation notes (non-normative)

- Prefer a dedicated type for token generation (e.g., `SecurityTokenPolicy`), with deterministic order to simplify snapshot tests. Ordering requirement is not functional, but deterministic output helps debugging.
- Token comparisons should be case-insensitive when validating, but output tokens must be normalized to lower-case.
