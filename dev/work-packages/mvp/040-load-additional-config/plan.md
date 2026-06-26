# Implementation Plan

**Target output path:** `./dev/work-packages/mvp/040-load-additional-config/plan.md`

## Project structure / touch points

This uplift is expected to touch only the existing configuration-related projects:

- `configuration/UKHO.Aspire.Configuration.Hosting`
  - Extend distributed application wiring to pass two new optional settings to the seeder via environment variables.
- `configuration/UKHO.Aspire.Configuration.Seeder`
  - Add two optional inputs (env + CLI) and, when provided, ingest an additional directory of files into Azure App Configuration.
- Shared/constants (wherever `WellKnownConfigurationName` lives today)
  - Add two new well-known environment variable names.

Naming note: despite the spec examples mentioning “rules”, the uplift must stay **generic** and avoid rule-specific terminology in code (names, logs, options).

## Feature Slice: Generic “additional configuration directory ingestion”

- [x] Work Item 1: Hosting passes optional additional config settings to Seeder (Aspire path) - Completed
  - **Purpose**: Enable local Aspire orchestration to opt-in to additional configuration ingestion without changing existing behaviour.
  - **Acceptance Criteria**:
    - `AddConfigurationEmulator()` accepts `additionalConfigurationPath` and `additionalConfigurationPrefix` parameters, both defaulting to `string.Empty`.
    - Two new `WellKnownConfigurationName` values exist for the additional path and prefix.
    - Seeder container/process receives the new environment variables when `AddConfigurationEmulator()` is called.
    - `AddConfiguration()` remains unchanged.
  - **Definition of Done**:
    - Code compiled.
    - Minimal verification demonstrates env vars are present (e.g., via existing logging/diagnostics mechanism) without impacting existing flow.
    - Documentation updated if there is developer-facing usage guidance.
  - [x] Task 1.1: Locate hosting extension and well-known configuration constants - Completed
    - [x] Step 1: Identify `DistributedApplicationBuilderExtensions.cs` and current `AddConfigurationEmulator()` signature.
    - [x] Step 2: Locate `WellKnownConfigurationName` (or equivalent) and confirm existing naming conventions.
  - [x] Task 1.2: Extend `AddConfigurationEmulator()` to accept and pass new values - Completed
    - [x] Step 1: Add two optional parameters with defaults `string.Empty`.
    - [x] Step 2: Wire them to the seeder via environment variables using the new well-known names.
    - [x] Step 3: Ensure passing empty strings does not change current behaviour.
  - **Files**:
    - `configuration/UKHO.Aspire.Configuration.Hosting/DistributedApplicationBuilderExtensions.cs`: extend `AddConfigurationEmulator()` signature + env var wiring.
    - `configuration/**/WellKnownConfigurationName*.cs`: add two new well-known env var names.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - Build the solution.
    - Start the local Aspire orchestration as per repository instructions and confirm the seeder still runs successfully.
    - (If repo has a way to inspect container env vars) verify the two new env vars are present when non-empty values are supplied.
  - **User Instructions**:
    - Example usage from Aspire host code (where `AddConfigurationEmulator()` is called):
      - Provide a local directory path and a prefix (both optional).

  **Completed summary**:
  - Extended `AddConfigurationEmulator()` to accept optional `additionalConfigurationPath` and `additionalConfigurationPrefix` parameters (default `string.Empty`) and pass them to the seeder as environment variables.
  - Added `WellKnownConfigurationName.AdditionalConfigurationPath` and `WellKnownConfigurationName.AdditionalConfigurationPrefix` constants.
  - Build verified.

- [x] Work Item 2: Seeder supports optional env/CLI inputs and ingests additional configuration directory - Completed
  - **Purpose**: Provide an additive, opt-in mechanism for seeding extra key/values from a directory into Azure App Configuration.
  - **Acceptance Criteria**:
    - Seeder reads additional prefix/path from environment variables (using `WellKnownConfigurationName`).
    - Seeder supports two optional CLI args for the same values when running outside Aspire, following existing argument parsing patterns.
    - If either value is null/empty => additional ingestion is skipped.
    - If both are non-empty => all files under the root path are recursively enumerated and written to App Configuration:
      - Key: `{prefix}:{path0}:...:{pathn}:{filenameWithoutExtension}`
      - Value: file contents as string
    - No existing seeding behaviour changes (baseline keys remain identical).
    - Code remains generic (no “rules” terminology in names/logs).
  - **Definition of Done**:
    - Unit tests for key construction and directory traversal behaviour added and passing.
    - Build succeeds.
    - Standalone run can be exercised with CLI args.
  - [x] Task 2.1: Add new env/CLI options (non-breaking) - Completed
    - [x] Step 1: Identify current Seeder option model / argument parsing entry point.
    - [x] Step 2: Add two optional options for additional path/prefix with defaults aligned to existing patterns (`string.Empty` or `null` as per current code) while treating them as optional.
    - [x] Step 3: Implement precedence consistent with existing patterns (e.g., CLI overriding env or vice versa).
  - [x] Task 2.2: Implement additional directory ingestion - Completed
    - [x] Step 1: Implement a utility method/function to enumerate files recursively and compute relative path segments.
    - [x] Step 2: Compute key per spec using `:` delimiters, removing the file extension.
    - [x] Step 3: Read file content as string and upsert into App Configuration using existing client/writer code path.
    - [x] Step 4: Ensure additional ingestion is performed at an appropriate point that does not affect existing seeding (e.g., after existing sources seed, or before, but with no behavioural change).
    - [x] Step 5: Add logging consistent with existing code style; keep terminology generic (e.g., “additional configuration”).
  - [x] Task 2.3: Add tests - Completed
    - [x] Step 1: Add unit tests for key generation from:
      - root-level file
      - nested directories
      - multiple extensions
    - [x] Step 2: Add unit tests for opt-in logic (both provided vs missing values).
    - [x] Step 3: Add a lightweight integration-style test if the repo has a test harness for App Configuration emulator/client (optional; only if patterns exist).
  - **Files**:
    - `configuration/UKHO.Aspire.Configuration.Seeder/**`: add optional CLI args + env ingestion + directory ingestion.
    - `configuration/UKHO.Aspire.Configuration.Seeder/**Tests/**` (or existing test project): add/update tests.
  - **Work Item Dependencies**: Work Item 1 (for env var naming alignment).
  - **Run / Verification Instructions**:
    - Standalone:
      - Run the seeder executable with the two new CLI args pointing to a local directory and a prefix.
      - Verify keys are created in App Configuration with expected names/values.
    - Aspire:
      - Configure `AddConfigurationEmulator(additionalConfigurationPath: <path>, additionalConfigurationPrefix: <prefix>)` in the Aspire host.
      - Run orchestration and verify extra keys appear.

  **Completed summary**:
  - Added optional "additional configuration" CLI parameters for standalone runs and plumbed corresponding env/config values through Aspire run path.
  - Implemented recursive file ingestion into App Configuration where keys are built as `{prefix}:{relativePathSegments}:{filenameWithoutExtension}` and values are file contents.
  - Added new seeding helpers under `configuration/UKHO.Aspire.Configuration.Seeder/AdditionalConfiguration`.
  - Added new unit test project `test/UKHO.Aspire.Configuration.Seeder.Tests` verifying key building and relative path segmentation.
  - `dotnet test` verified.

- [x] Work Item 3: Developer documentation / discoverability - Completed
  - **Purpose**: Make the feature easy to adopt and hard to misuse.
  - **Acceptance Criteria**:
    - `dev/work-packages/mvp/040-load-additional-config/spec.md` remains accurate.
    - `dev/work-packages/mvp/040-load-additional-config/plan.md` and any relevant repo documentation mentions how to use the two new `AddConfigurationEmulator()` parameters and the CLI args.
    - Clarifies the feature is generic, with “rules” only as an example use.
    - Code-level documentation exists for the new behaviour (public API XML docs / usage notes) and is consistent across `UKHO.Aspire.Configuration.Hosting`, `UKHO.Aspire.Configuration.Seeder`, and `UKHO.Aspire.Configuration`.
    - Code comments are reviewed and updated/added where needed to clarify non-obvious logic introduced by this change, excluding emulator code.
  - **Definition of Done**:
    - Documentation updated.
    - Build/test unchanged.
  - [x] Task 3.1: Update usage notes (docs) - Completed
    - [x] Step 1: Add a short usage snippet describing where to set the two parameters in Aspire wiring.
    - [x] Step 2: Add a short standalone execution snippet describing the new optional CLI args.
  - [x] Task 3.2: Add code-level documentation (non-emulator) - Completed
    - [x] Step 1: Add/adjust XML documentation on the extended `AddConfigurationEmulator()` parameters describing purpose, defaults, and opt-in behaviour (keep terminology generic).
    - [x] Step 2: Add/adjust XML documentation or usage help text around Seeder CLI options/env vars (keep terminology generic).
    - [x] Step 3: If the `UKHO.Aspire.Configuration` project exposes shared configuration types/constants, ensure new constants have naming and doc consistency.
  - [x] Task 3.3: Review/update code comments for new logic (non-emulator) - Completed
    - [x] Step 1: Review the implementation changes in:
      - `UKHO.Aspire.Configuration.Hosting`
      - `UKHO.Aspire.Configuration.Seeder`
      - `UKHO.Aspire.Configuration`
    - [x] Step 2: Add `//` comments on their own line to explain non-obvious logic (e.g., path-to-key mapping, relative path segmentation, opt-in gating), matching repo style.
    - [x] Step 3: Confirm no comment changes are made in emulator project code.
  - **Files**:
    - `dev/work-packages/mvp/040-load-additional-config/spec.md`: minor clarifications if needed.
    - (Optional) existing repo README or configuration docs, if present and appropriate.
  - **Work Item Dependencies**: Work Items 1–2.
  - **Run / Verification Instructions**:
    - N/A (documentation only).

  **Completed summary**:
  - Added usage snippets to `dev/work-packages/mvp/040-load-additional-config/spec.md` for Aspire host wiring and standalone seeder execution.
  - Added XML docs for the extended `AddConfigurationEmulator()` parameters and for new `WellKnownConfigurationName` constants.
  - Added targeted `//` comments for non-obvious additional configuration key/path mapping logic (no emulator comment changes).

---

## Summary / key considerations

- Keep the change additive and fully opt-in: defaults are empty and should result in identical behaviour to today.
- Maintain generic naming throughout the code uplift to preserve reuse across projects.
- Follow existing Seeder patterns for CLI parsing and environment variable reading to minimize risk.
- Prioritize unit tests around path-to-key mapping and opt-in behaviour; integration verification can be done via local Aspire run against the emulator.
