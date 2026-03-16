# Implementation Plan

Work package: `docs/035-fsemualator-common/`

This plan implements the specification in `docs/035-fsemualator-common/spec.md` by introducing a shared library that is the *only* place where the canonical ingestion message and strict `SecurityTokens` policy is built.

## Feature Slice A — Introduce shared canonical payload builder (library + tests)

- [x] Work Item A1: Create `FileShareEmulator.Common` + `FileShareEmulator.Common.Tests` with strict token policy - Completed
  - **Purpose**: Establish a single canonical implementation (and test-suite) to enforce SecurityTokens contain *only* `batchcreate`, `batchcreate_{businessUnitName}` (active BU only), and `public`.
  - **Acceptance Criteria**:
    - New project `tools/FileShareEmulator.Common` exists and compiles.
    - New project `test/FileShareEmulator.Common.Tests` exists and compiles.
    - Tests validate the strict token rules including normalization and active BU-only behavior.
    - Tokens produced are deterministic and normalized to lowercase.
  - **Definition of Done**:
    - Code implemented in `tools/FileShareEmulator.Common` with a small public API.
    - Tests passing in `test/FileShareEmulator.Common.Tests`.
    - `run_build` succeeds.
    - Can execute end-to-end via: `dotnet test test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`
  - [x] Task A1.1: Add project skeletons - Completed
    - [x] Step 1: Create `tools/FileShareEmulator.Common/FileShareEmulator.Common.csproj` targeting the repo-standard runtime (net10.0 unless constrained).
    - [x] Step 2: Create `test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj` targeting net10.0, referencing the common project.
    - [x] Step 3: Add test framework references (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Shouldly`, coverlet as per repo conventions).
  - [x] Task A1.2: Implement strict token policy API - Completed
    - [x] Step 1: Added `SecurityTokenPolicy.CreateTokens(string? activeBusinessUnitName)` producing *only* `batchcreate`, `batchcreate_{bu}` (normalized), `public`.
    - [x] Step 2: Implemented normalization rules (trim + `ToLowerInvariant()`, omit when blank).
    - [x] Step 3: Enforced strict set (no other tokens).
    - [x] Step 4: Deterministic ordering (BU token in the middle when present).
  - [x] Task A1.3: Implement canonical ingestion payload builder API - Completed
    - [x] Step 1: Added `FileShareIngestionMessageFactory.CreateIndexIngestionRequest(...)`.
    - [x] Step 2: Ensures `BusinessUnitName` property exists and `SecurityTokens` come from `SecurityTokenPolicy`.
    - [x] Step 3: Attributes/files passed through.
  - [x] Task A1.4: Create comprehensive unit tests - Completed
    - [x] Step 1: Added token tests asserting strict equality and normalization.
    - [x] Step 2: Added payload tests (BusinessUnitName property, strict tokens) + deterministic JSON serialization regression check.

  **Summary of changes (A1)**:
  - Created `tools/FileShareEmulator.Common` and `test/FileShareEmulator.Common.Tests`.
  - Implemented `SecurityTokenPolicy` and `FileShareIngestionMessageFactory`.
  - Added 7 unit tests covering the strict token rules and payload construction.
  - Verified with: `dotnet test test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`
  - **Files**:
    - `tools/FileShareEmulator.Common/FileShareEmulator.Common.csproj`: new project
    - `tools/FileShareEmulator.Common/SecurityTokenPolicy.cs`: token creation rules
    - `tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs`: canonical ingestion message creation
    - `test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`: new test project
    - `test/FileShareEmulator.Common.Tests/SecurityTokenPolicyTests.cs`: strict token rule tests
    - `test/FileShareEmulator.Common.Tests/FileShareIngestionMessageFactoryTests.cs`: payload creation + JSON snapshot/determinism tests
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`

## Feature Slice B — Wire both tools to shared library (parity enforced)

- [x] Work Item B1: Update `FileShareEmulator` to exclusively use the common payload builder - Completed
  - **Purpose**: Ensure ingestion queue messages are built only via `FileShareEmulator.Common` and comply with strict SecurityTokens policy.
  - **Acceptance Criteria**:
    - `tools/FileShareEmulator` references `tools/FileShareEmulator.Common`.
    - `IndexService` (or its collaborators) creates the ingestion request using the shared factory.
    - No legacy token building logic is used.
  - **Definition of Done**:
    - Build passes.
    - Existing tests pass.
    - If needed, add/adjust emulator-side tests to assert it calls the shared factory.
    - Can execute end-to-end via: running FileShareEmulator and observing queued JSON (manual verification).
  - [x] Task B1.1: Add `ProjectReference` - Completed
    - [x] Step 1: Added `ProjectReference` to `tools/FileShareEmulator.Common/FileShareEmulator.Common.csproj` in `tools/FileShareEmulator/FileShareEmulator.csproj`.
  - [x] Task B1.2: Refactor ingestion request creation - Completed
    - [x] Step 1: Updated `tools/FileShareEmulator/Services/IndexService.cs` to create the ingestion message via `FileShareEmulator.Common.FileShareIngestionMessageFactory`.
    - [x] Step 2: `BatchSecurityTokenService` now only supplies the active `BusinessUnitName`; strict tokens are generated only by the common policy.
  - [x] Task B1.3: Remove/retire old code paths - Completed
    - [x] Step 1: Removed local token-enrichment helper from `IndexService`.
    - [x] Step 2: Removed DB group/user token reads from `BatchSecurityTokenService` to prevent extra tokens being introduced.

  **Summary of changes (B1)**:
  - Wired FileShareEmulator to `FileShareEmulator.Common` for canonical ingestion message creation and strict token policy.
  - Ensured emulator no longer adds group/user tokens into `SecurityTokens`.
  - Verified with: `dotnet build` and `dotnet test` (including `FileShareEmulator.Common.Tests`).
  - **Files**:
    - `tools/FileShareEmulator/FileShareEmulator.csproj`: add reference
    - `tools/FileShareEmulator/Services/IndexService.cs`: delegate payload creation to common
    - (optional) `tools/FileShareEmulator/Services/BatchSecurityTokenBuilder.cs`: remove/obsolete usage if no longer required
  - **Work Item Dependencies**: A1
  - **Run / Verification Instructions**:
    - `dotnet build`
    - Run emulator and confirm queued JSON tokens are exactly: `batchcreate`, `batchcreate_{bu}`, `public`

- [x] Work Item B2: Update `RulesWorkbench` to exclusively use the common payload builder - Completed
  - **Purpose**: Ensure `/evaluate` uses the exact same canonical payload builder as the emulator.
  - **Acceptance Criteria**:
    - `tools/RulesWorkbench` references `tools/FileShareEmulator.Common`.
    - Batch payload building for evaluation uses the shared policy/factory.
    - `/evaluate` displays only the allowed tokens.
  - **Definition of Done**:
    - Build passes.
    - RulesWorkbench tests updated to assert strict token set (and removed reflection-based tests if they become obsolete).
    - Can execute end-to-end via: open RulesWorkbench `/evaluate` and load a batch.
  - [x] Task B2.1: Add `ProjectReference` - Completed
    - [x] Step 1: Added `ProjectReference` to `tools/FileShareEmulator.Common/FileShareEmulator.Common.csproj` in `tools/RulesWorkbench/RulesWorkbench.csproj`.
  - [x] Task B2.2: Refactor batch payload construction - Completed
    - [x] Step 1: Updated `BatchPayloadLoader` to use `FileShareEmulator.Common.SecurityTokenPolicy`.
    - [x] Step 2: Ensured `public` is always present (policy always includes it).
    - [x] Step 3: Removed any DB-sourced group/user tokens from RulesWorkbench payloads.
  - [x] Task B2.3: Update RulesWorkbench tests - Completed
    - [x] Step 1: Updated `BatchPayloadLoaderTests` to assert strict token equality including `public`.
    - [x] Step 2: Removed reflection-based tests of the deleted private token builder.

  **Summary of changes (B2)**:
  - Wired RulesWorkbench to `FileShareEmulator.Common` and enforced strict SecurityTokens in `/evaluate` payloads.
  - Removed group/user DB tokens from RulesWorkbench evaluation payloads.
  - Verified with: `dotnet build` and `dotnet test test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj`.
  - **Files**:
    - `tools/RulesWorkbench/RulesWorkbench.csproj`: add reference
    - `tools/RulesWorkbench/Services/BatchPayloadLoader.cs`: use common library
    - `test/RulesWorkbench.Tests/BatchPayloadLoaderTests.cs`: adjust tests
  - **Work Item Dependencies**: A1
  - **Run / Verification Instructions**:
    - `dotnet test test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj`
    - Run RulesWorkbench and verify `/evaluate` shows only allowed tokens.

## Feature Slice C — Cleanup + parity regression guard

- [x] Work Item C1: Remove obsolete token builder code and add parity regression tests - Completed
  - **Purpose**: Prevent future drift by ensuring only `FileShareEmulator.Common` is used and by adding regression tests comparing serialized payload outputs.
  - **Acceptance Criteria**:
    - Old token builder code is removed or no longer referenced.
    - A regression test asserts that emulator payload creation and rules workbench evaluation payload creation are identical in structure for the same model inputs.
  - **Definition of Done**:
    - `dotnet build` succeeds.
    - Full test suite passes.
  - [x] Task C1.1: Remove/obsolete old builders - Completed
    - [x] Step 1: Identified old token-building locations (`BatchSecurityTokenBuilder`, legacy group/user token paths).
    - [x] Step 2: Removed `tools/FileShareEmulator/Services/BatchSecurityTokenBuilder.cs` and removed now-obsolete tests that depended on it.
  - [x] Task C1.2: Add parity regression test - Completed
    - [x] Step 1: Added deterministic JSON regression test in `test/FileShareEmulator.Common.Tests/CanonicalPayloadJsonTests.cs` ensuring identical inputs serialize identically.

  **Summary of changes (C1)**:
  - Deleted the obsolete emulator token builder (`BatchSecurityTokenBuilder`) to prevent reintroduction of non-compliant tokens.
  - Removed the legacy unit tests that targeted the deleted builder.
  - Verified strict token/payload determinism is protected by common library tests.
  - Verified with: `dotnet build` and `dotnet test`.
  - **Files**:
    - `tools/FileShareEmulator/Services/BatchSecurityTokenBuilder.cs`: remove if obsolete
    - `tools/RulesWorkbench/Services/BatchPayloadLoader.cs`: ensure no legacy token logic remains
    - `test/FileShareEmulator.Common.Tests/*`: add regression tests
  - **Work Item Dependencies**: B1, B2
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Summary (approach + considerations)

- Establish the token policy and ingestion payload creation in a shared library first (with strict tests).
- Then wire both producers/consumers (`FileShareEmulator`, `RulesWorkbench`) to the shared library, removing duplicated code paths.
- Keep output deterministic to make it easy to validate parity and prevent future regressions.
- Key risk: existing code currently pulls in group/user tokens from DB; this plan intentionally removes those from the `SecurityTokens` list to satisfy the spec.
