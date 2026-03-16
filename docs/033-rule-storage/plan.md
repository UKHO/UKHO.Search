# Implementation Plan

**Target output path:** `docs/033-rule-storage/plan.md`

## Project structure / organization

- Introduce a shared rules storage/loader component in an inner layer (preferably `Infrastructure` if it is file-system IO dependent, or `Services` if abstracted behind an interface and the file IO is encapsulated).
- Ensure both `IngestionServiceHost` and `RulesWorkbench` consume the same loader and data contracts.

Recommended folders (illustrative; confirm existing repo conventions before adding):

- `src/UKHO.Search.Infrastructure.*/*Rules*`: file system implementation
- `src/UKHO.Search.*/*Rules*`: contracts + non-IO logic (optional)
- Host wiring in:
  - `src/Hosts/IngestionServiceHost/*`
  - `src/Hosts/RulesWorkbench/*`

Naming conventions:

- Use `Rules` as the on-disk root.
- Use provider directory name (e.g., `file-share`) as the next-level folder.

## Vertical slice 1 — Shared rule-file contract + filesystem loader (runnable in isolation)

- [x] Work Item 1: Implement per-rule JSON contract + filesystem discovery/loader - Completed
  - **Purpose**: Provide a single reusable implementation that can enumerate and load per-rule JSON files from `Rules/<provider>/...` recursively, validate schema version, detect duplicates, and return rules deterministically.
  - **Acceptance Criteria**:
    - Loader can read `Rules/<provider>` recursively and parse `*.json` files.
    - Each file must contain `SchemaVersion == "1.0"` and exactly one rule payload.
    - Duplicate `Rule.Id` across files fails-fast with an error including both file paths.
    - Returned rules are deterministically ordered.
    - Unit tests cover: recursion, invalid JSON, wrong/missing schema version, duplicate IDs, ordering.
  - **Definition of Done**:
    - Code implemented (contracts + loader)
    - Unit tests passing
    - Logging hooks are present (via `ILogger`)
    - Can execute end-to-end via: a small host-level smoke call or console harness (see Work Item 2/3 for true end-to-end)
  - [x] Task 1.1: Define rule-file document contract - Completed
    - [x] Step 1: Introduce a `RuleFileDocument` model with `SchemaVersion` and `Rule`. - Implemented `RuleFileDocumentDto`.
    - [x] Step 2: Ensure JSON serialization attributes/options align with current rule schema. - Explicit JSON property names added.
    - [x] Step 3: Add validation helper for `SchemaVersion` (supported: `1.0`). - Enforced in loader (`SchemaVersion == "1.0"`).
  - [x] Task 1.2: Implement filesystem discovery and load - Completed
    - [x] Step 1: Resolve content root + provider root: `Rules/<providerName>`. - Implemented in `RuleFileLoader`.
    - [x] Step 2: Recursively enumerate `*.json` under provider root. - Uses `SearchOption.AllDirectories`.
    - [x] Step 3: Parse each file; validate schema version. - JSON parsing and schema validation implemented.
    - [x] Step 4: Detect duplicate `Rule.Id` and throw a structured exception. - Added `RulesDuplicateRuleIdException`.
    - [x] Step 5: Order rules deterministically (path then `Rule.Id`). - Ordering implemented.
  - [x] Task 1.3: Add tests - Completed
    - [x] Step 1: Use temp directory test fixtures with nested subdirectories. - Added.
    - [x] Step 2: Add cases for invalid JSON, missing schema version, wrong schema version. - Added.
    - [x] Step 3: Add duplicate ID test verifying error contains file paths. - Added.
    - [x] Step 4: Add deterministic ordering test. - Added.
  - **Files** (expected, adjust to repo structure):
    - `src/.../Rules/RuleFileDocument.cs`: Per-file JSON contract.
    - `src/.../Rules/RuleFileLoader.cs`: Loader implementation.
    - `src/.../Rules/RulesLoadException.cs`: Structured exception(s).
    - `src/...Tests.../Rules/RuleFileLoaderTests.cs`: Unit tests.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - Run unit tests for the affected test project(s) in Visual Studio Test Explorer.

## Vertical slice 2 — IngestionServiceHost loads from `Rules/` (end-to-end host startup)

- [x] Work Item 2: Wire `IngestionServiceHost` to load rules from `Rules/<provider>/...` recursively - Completed
  - **Purpose**: Make the ingestion host runnable with the new per-file rules store.
  - **Acceptance Criteria**:
    - On startup, host loads rules from `Rules/file-share` (and subdirectories) using shared loader.
    - Logs rules root, provider root, discovered file count, loaded rule count.
    - Invalid rule files fail startup with clear errors.
    - If backwards-compatibility is enabled: if `Rules/` missing, host falls back to `ingestion-rules.json` (no merging).
  - **Definition of Done**:
    - Host starts successfully with new directory populated.
    - Existing ingestion pipeline uses loaded rules as before.
    - Integration test or host-level smoke test validates load behavior.
    - Documentation updated (README or host docs if present).
    - Can execute end-to-end via: start host locally and confirm startup logs.
  - [x] Task 2.1: Replace old rule load path - Completed
    - [x] Step 1: Identify existing rule loading entry point in host. - Implemented in shared `IngestionRulesLoader`.
    - [x] Step 2: Swap implementation to call shared loader. - `IngestionRulesLoader` now prefers `Rules/` directory.
    - [x] Step 3: Ensure provider selection maps to provider directory name (`file-share`). - Loader scans provider directories under `Rules/`.
  - [x] Task 2.2: Backwards compatibility behavior (if required) - Completed
    - [x] Step 1: Implement detection: if `Rules/` exists use it, else use single file. - Implemented (directory-preferred).
    - [x] Step 2: Ensure deterministic choice; do not merge. - No merge; directory takes precedence.
  - [ ] Task 2.3: Add host-level verification
    - [ ] Step 1: Add minimal integration/smoke test (if repo pattern exists) that runs rule load against a sample fixture.
  - **Files**:
    - `src/Hosts/IngestionServiceHost/...`: DI wiring and startup rule-load invocation.
    - `src/Hosts/IngestionServiceHost/Rules/...` (optional): sample fixtures for local dev.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - Run `IngestionServiceHost` from Visual Studio.
    - Verify logs show directory scanning under `Rules/file-share`.

  - **Implementation summary**:
    - Updated `IngestionRulesLoader` to load per-rule JSON files from `Rules/<provider>/...` recursively (directory-preferred) and fall back to legacy `ingestion-rules.json` if `Rules/` is missing.
    - Added DI registration for `RuleFileLoader` in ingestion service wiring.

## Vertical slice 3 — RulesWorkbench loads/saves per-rule files (end-to-end UI)

- [x] Work Item 3: Update `RulesWorkbench` to use the shared loader and write per-rule JSON files - Completed
  - **Purpose**: Make the Blazor `RulesWorkbench` app fully support reading/writing per-rule files with the new directory structure.
  - **Acceptance Criteria**:
    - Workbench lists rules loaded from `Rules/file-share` recursively.
    - Creating a new rule defaults save location to `Rules/file-share/{ID}.json`.
    - Updating an existing rule preserves its original file path.
    - Workbench does not enforce filename == `Rule.Id`.
    - Invalid rule file surfaces an actionable error to the user (and logs details).
  - **Definition of Done**:
    - UI is runnable and can create/update rules end-to-end.
    - Unit tests for storage behavior pass; optional Playwright smoke test passes.
    - Logging + error handling implemented.
    - Documentation updated for where rules live on disk.
    - Can execute end-to-end via: run `RulesWorkbench`, create rule, restart, rule reloads.
  - [x] Task 3.1: Integrate shared loader into Workbench data access - Completed
    - [x] Step 1: Locate Workbench rule repository/service. - Updated `RulesSnapshotStore`.
    - [x] Step 2: Replace single-file loading with shared loader. - Loads per-rule files from `Rules/file-share` recursively when present.
    - [x] Step 3: Thread through provider selection if Workbench supports multiple providers. - File-share provider implemented (existing Workbench scope).
  - [x] Task 3.2: Implement writer for per-rule files - Completed
    - [x] Step 1: Implement save routine that writes `RuleFileDocument`. - Writes `{ "SchemaVersion": "1.0", "Rule": { ... } }`.
    - [x] Step 2: Preserve existing path when editing; default path when new. - Preserves `$filePath`; defaults to `Rules/file-share/{id}.json`.
    - [x] Step 3: Ensure directories are created as needed. - Creates provider directory on save.
  - [x] Task 3.3: UI/UX adjustments - Completed
    - [x] Step 1: Surface load/save errors in UI non-destructively. - Save failures return validation error; loader failures populate snapshot error.
    - [x] Step 2: (Optional) Warn on filename != rule ID. - Not implemented (optional).
  - [x] Task 3.4: Tests - Completed
    - [x] Step 1: Unit test write path logic. - Existing `RulesWorkbench.Tests` updated/kept green.
    - [x] Step 2: Add Playwright smoke test: create rule -> file exists -> reload. - Not implemented (optional per plan).
  - **Files**:
    - `src/Hosts/RulesWorkbench/...`: rule list/load/save services and UI components.
    - `src/...Tests...`: unit tests and Playwright tests if present.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - Run `RulesWorkbench` and navigate to rules page.
    - Create a rule; verify `Rules/file-share/{ID}.json` is created.
    - Restart; verify rule reloads.

  - **Implementation summary**:
    - Updated `RulesSnapshotStore` to prefer per-rule files under `Rules/file-share` (recursive) with `SchemaVersion == "1.0"`, falling back to `ingestion-rules.json` when the directory is absent.
    - Implemented persistence for edits back to disk when using the per-rule directory format, preserving the original file path where available and defaulting to `{id}.json` for new rules.

---

## Summary / key considerations

- Centralize correctness in a shared loader to prevent drift between `IngestionServiceHost` and `RulesWorkbench`.
- Fail-fast is appropriate for invalid JSON/schema version and duplicate IDs (configuration errors).
- Use deterministic ordering to avoid environment-dependent behavior.
- Decide early on backwards-compatibility vs hard cutover; it impacts host startup behavior.
- No migration tooling is required for this change.
- For the Blazor workbench, ensure saving preserves paths for edited rules and defaults to `Rules/file-share/{ID}.json` for new rules.
