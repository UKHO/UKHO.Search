# 033 - Rule storage (per-rule JSON files)

**Target output path:** `docs/033-rule-storage/spec.md`

## 1. Introduction

### 1.1 Purpose

This document specifies a change to how ingestion rules are stored and loaded for both `IngestionServiceHost` and `RulesWorkbench`.

Currently, rules are loaded from a single `ingestion-rules.json` file at the root of each host/app. The desired outcome is a shared storage layout where **each rule is stored in its own JSON file** under a provider-scoped directory tree.

### 1.2 Scope

In scope:

- Define the new on-disk rules layout (directory structure + file naming defaults).
- Define the JSON shape changes required for per-rule storage (including `SchemaVersion`).
- Define discovery/loading behavior (recursive read under provider root).
- Define the default persistence behavior for rules created by `RulesWorkbench`.
- Define migration and compatibility strategy (what happens to the old single-file approach).

Out of scope:

- Changes to rule evaluation semantics.
- Changes to provider processing logic beyond how rules are discovered/loaded.
- Any UI redesign in `RulesWorkbench` unrelated to storage semantics.

### 1.3 Background / current state

- `IngestionServiceHost` loads rules from `ingestion-rules.json`.
- `RulesWorkbench` also loads/saves rules from/to `ingestion-rules.json`.
- This single-file approach is becoming restrictive for managing many rules and for organizing rules into groups.

## 2. Goals and non-goals

### 2.1 Goals

1. Store each rule in its own JSON file.
2. Use the same storage structure for both `IngestionServiceHost` and `RulesWorkbench`.
3. Support a provider-scoped root directory (e.g., `file-share`).
4. Allow rules to be organized into arbitrary subdirectories under the provider directory.
5. Add `SchemaVersion` at the rule-file level, defaulting to `"1.0"`.
6. Define deterministic behavior for loading when multiple files exist.

### 2.2 Non-goals

- No requirement that rule filename equals rule `Id`.
- No requirement to change rule ID format.
- No requirement to introduce a database or remote store.

## 3. Proposed solution overview

### 3.1 High-level approach

Replace the single-file rule store with a directory-based store:

- A single root directory named `Rules`.
- Under `Rules`, a provider directory per provider (e.g., `file-share`).
- Under the provider directory, any directory depth is allowed.
- Rule loader reads **all `*.json` rule files recursively** under the selected provider directory.

Both `IngestionServiceHost` and `RulesWorkbench` will be updated to:

- Read from this directory structure.
- Treat each JSON file as a single rule document.

### 3.2 New storage layout

Repository/app content root (for each application):

- `Rules/`
  - `<providerName>/`
    - one or more rule files anywhere under here

Example:

- `Rules/file-share/0001.json`
- `Rules/file-share/0002.json`
- `Rules/file-share/Admiralty/Warnings/critical-warning.json`

Notes:

- The provider name directory is the **next path level** under `Rules/`.
- For the file share provider, the provider directory name is `file-share`.
- The loader must include rules in all subdirectories under `Rules/file-share`.

### 3.3 Default file creation convention (RulesWorkbench)

- When `RulesWorkbench` generates a new rule file, by default it should create it at:
  - `Rules/file-share/{ID}.json`

Where:

- `{ID}` is the rule's `Id`.

The filename is a default convention only; it is not a constraint.

## 4. Functional requirements

### 4.1 Rule file JSON format

Each rule file:

- MUST contain exactly one rule.
- MUST contain a top-level `SchemaVersion` property with value `"1.0"`.

#### 4.1.1 Rule file schema (conceptual)

The document structure is conceptually:

- `SchemaVersion`: string
- `Rule`: object (the rule definition as currently used)

The exact shape of the `Rule` object should be compatible with the existing rule schema used inside the current `ingestion-rules.json` (i.e., do not change rule semantics; only re-home the rule into per-file storage).

Example (illustrative only):

```json
{
  "SchemaVersion": "1.0",
  "Rule": {
    "Id": "my-rule-id",
    "Provider": "file-share",
    "Enabled": true
  }
}
```

If the existing implementation today expects a list/collection file, the new per-file wrapper should be introduced as the stable unit of storage.

### 4.2 Rule discovery/loading

#### 4.2.1 Provider-root selection

- When loading rules for a given provider (e.g., `file-share`), the loader MUST treat the provider root as:
  - `Rules/<providerName>/`

#### 4.2.2 Directory traversal

- The loader MUST recursively enumerate all subdirectories under the provider root.
- The loader MUST consider all files with `.json` extension as candidate rule files.
- The loader MUST ignore non-JSON files.

#### 4.2.3 Parsing and validation

- The loader MUST parse each candidate file as a rule file document.
- The loader MUST validate that `SchemaVersion` is present.
- The loader MUST validate that `SchemaVersion == "1.0"`.

Validation behavior:

- If a file is invalid JSON or fails schema/version validation, it MUST be treated as a configuration error (fail-fast) because this indicates an invalid ruleset.
- If a valid rule file contains a rule definition that is valid structurally but references properties not present at runtime, this MUST NOT be treated as a load-time failure (runtime matching rules apply as per existing guidance).

#### 4.2.4 Duplicate rule IDs

When two or more loaded rule files contain the same `Rule.Id`:

- The system MUST detect the duplicate.
- The system MUST fail-fast at load time with a clear error that includes:
  - the duplicate `Rule.Id`
  - the file paths involved

Rationale: duplicates are almost always a deployment/configuration mistake.

#### 4.2.5 Ordering

- The loader MUST define a deterministic rule ordering for execution/evaluation.
- Default ordering SHOULD be stable and predictable across environments.

A recommended deterministic ordering:

1. Sort by file path (ordinal, case-insensitive) then
2. Sort by `Rule.Id` (ordinal, case-insensitive) for tie-break.

(Exact ordering can be refined during implementation, but must be deterministic.)

### 4.3 RulesWorkbench behavior

#### 4.3.1 Load

- `RulesWorkbench` MUST load rules using exactly the same discovery rules as `IngestionServiceHost`.

#### 4.3.2 Save

- When saving/updating an existing rule, `RulesWorkbench` SHOULD preserve the existing file path when that rule originated from a known file.
- When creating a new rule (no prior file path known), `RulesWorkbench` MUST default to:
  - `Rules/file-share/{ID}.json`

where `{ID}` is the rule `Id`.

#### 4.3.3 File name vs rule Id

- `RulesWorkbench` MUST NOT require the file name to equal the rule `Id`.
- `RulesWorkbench` MAY warn if the file name differs from the rule `Id` (optional, non-blocking).

### 4.4 IngestionServiceHost behavior

- `IngestionServiceHost` MUST load ingestion rules using the same directory-based discovery mechanism.
- The host MUST be configurable to select provider(s) to load (existing behavior should be preserved).

### 4.5 Backwards compatibility / migration

A compatibility strategy is required because existing deployments use `ingestion-rules.json`.

Options:

1. **Hard cutover (preferred for simplicity):**
   - Remove support for `ingestion-rules.json` and require the `Rules/` directory layout.

2. **Dual support (transition period):**
   - If `Rules/` exists, load from the new structure.
   - Otherwise, fall back to `ingestion-rules.json`.

This spec recommends **Option 2** if operationally required, but implementation should minimize ambiguity.

If dual support is implemented:

- The system MUST NOT merge rules from both sources by default.
- Selection MUST be deterministic (e.g., prefer `Rules/` when present).

## 5. Technical requirements

### 5.1 Shared implementation

- The rules loading capability SHOULD be implemented as a shared component/module used by both `IngestionServiceHost` and `RulesWorkbench`, to ensure identical behavior.

### 5.2 Path handling

- Paths MUST be resolved relative to each application’s content root (the same location the old `ingestion-rules.json` was resolved from).
- The rules root is always a directory named `Rules`.

### 5.3 Performance and IO considerations

- The loader SHOULD avoid re-reading unchanged files where possible (future enhancement).
- For now, a full directory scan at startup is acceptable.

### 5.4 Logging

- The loader MUST log:
  - rules root directory
  - provider root directory
  - count of discovered files
  - count of successfully loaded rules
- On failures, it MUST log file path and cause.

### 5.5 Error handling

- Invalid JSON / schema version issues MUST surface as startup/configuration errors.
- Duplicate IDs MUST surface as startup/configuration errors.

## 6. Data contracts

### 6.1 SchemaVersion

- `SchemaVersion` is a string.
- The only supported value for this work item is `"1.0"`.

### 6.2 Provider name

- The provider directory name is used for grouping and discovery.
- For the current provider, the directory is `file-share`.

## 7. Security and compliance considerations

- The rules directory is local configuration.
- Ensure that rule files are not accidentally treated as executable content.
- Ensure safe JSON parsing (no type name handling / polymorphic deserialization unless explicitly required).

## 8. Operational considerations

### 8.1 Deployment packaging

- Deployments must include the `Rules/` directory structure.
- For containerized deployments, ensure the `Rules/` directory is copied or mounted.

### 8.2 Troubleshooting

- Provide clear log messages listing:
  - which provider root was scanned
  - which rule file failed to load
  - duplicate ID details

## 9. Open questions / decisions

1. **Backwards compatibility:** Should `ingestion-rules.json` be supported for a transition period, or is this a hard cutover?
2. **Exact JSON shape:** Is the `Rule` wrapper acceptable, or do you prefer the rule properties at the root alongside `SchemaVersion`?
3. **Multi-provider loading:** Do we want a single scan of `Rules/` across all providers, or explicit provider selection per host?

---

## Appendix A: Acceptance criteria

1. Both `IngestionServiceHost` and `RulesWorkbench` load rules from `Rules/<provider>/.../*.json` recursively.
2. Each rule file includes `SchemaVersion: "1.0"`.
3. `RulesWorkbench` creates new rules at `Rules/file-share/{ID}.json` by default.
4. Rule filename does not have to match rule `Id`.
5. Duplicate rule IDs across files are detected and cause startup/load failure.
6. Loader behavior is deterministic and consistent between host and workbench.
