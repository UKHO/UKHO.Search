---
Title: Update `IngestionServiceHost` and `RulesWorkbench` to load ingestion rules from Azure App Configuration
Work Package: 041-ingestion-workbench-config-rules
Document Type: Specification
Version: v0.01
Status: Draft
Last Updated: 2026-03-17
---

# 1. Overview

## 1.1 Purpose
This specification defines the required changes to the `IngestionServiceHost` and `RulesWorkbench` hosts so that ingestion rule JSON definitions are no longer read from local disk files and are instead retrieved from Azure App Configuration.

## 1.2 Scope
In scope:
- Rules are loaded from Azure App Configuration by both `IngestionServiceHost` and `RulesWorkbench`.
- Rules are stored in App Configuration using a defined key pattern and are retrieved as raw JSON strings.
- `RulesWorkbench` uses the same App Configuration-based rule source approach as `IngestionServiceHost`.

Out of scope:
- Any changes to the rule JSON schema/DSL itself.
- Changes to rule evaluation behavior beyond the source of rule content.
- UI redesign of `RulesWorkbench` unrelated to rule sourcing.

## 1.3 Drivers / Motivation
- Remove reliance on local disk for rule deployment and updates.
- Centralize rule management to a configurable source consistent with cloud deployments.
- Align `RulesWorkbench` rule source with the approach already used by `IngestionServiceHost`.

# 2. System Context

## 2.1 Affected components
- Host: `IngestionServiceHost`
- Host: `RulesWorkbench`
- Configuration source: Azure App Configuration

## 2.2 External dependencies
- Azure App Configuration instance accessible to the running hosts
- Identity/credentials to access App Configuration (implementation-specific; assumed to be already established for `IngestionServiceHost` and added for `RulesWorkbench`)

## 2.3 Data shape (rules in configuration)
Rules are stored as individual key-values in Azure App Configuration.

- Key pattern:
  - `rules:{provider-name}:{rule-id}`
- Value:
  - The JSON string representing the rule definition.
  - Supported value shapes:
    - **Raw rule object** (preferred): the JSON object that contains `if`/`match` and `then`.
    - **Wrapped rule document** (supported for compatibility with existing per-file rule format):
      - `{ "schemaVersion": "1.0", "rule": { ...raw rule... } }`

Example:
- Key: `rules:file-share:adp-ingestion-rule`
- Value: `{ ...rule json... }`

# 3. High-level Design

## 3.1 Rule discovery and retrieval
### 3.1.1 Key selection
The hosts must enumerate and load all rule entries in App Configuration using the key prefix:
- `rules:`

The expected parsing behavior is:
- Split the key by `:`
- Interpret parts as:
  - Part 0: literal `rules`
  - Part 1: `{provider-name}`
  - Part 2+: `{rule-id}` (see note below)

Note on `{rule-id}` containing `:`:
- Decision: `{rule-id}` MUST NOT contain `:` (it is derived from a file name when configuration is seeded). Therefore keys are always exactly `rules:{provider-name}:{rule-id}` with 3 segments.

### 3.1.2 Provider name
The rule provider name is the logical ingestion provider identifier (e.g., `file-share`). It is used for namespacing rules and for filtering in tooling.

### 3.1.3 Rule value retrieval
For each matched key, the value is treated as the source-of-truth rule JSON.

If the value is stored in the wrapped document form `{ schemaVersion, rule }`, the hosts must unwrap to the inner `rule` payload before passing it to the rule parsing/validation pipeline.

Failure handling expectations:
- If a rule value is missing/empty for a discovered key, that rule must be skipped and surfaced as a validation error/diagnostic message.
- If a rule JSON value cannot be parsed/validated, that rule must be rejected, and the error must be visible in:
  - `IngestionServiceHost` logs
  - `RulesWorkbench` UI/diagnostics

### 3.1.4 Hot refresh (mandatory)
Azure App Configuration supports dynamic configuration refresh. Rule configuration MUST support hot refresh so that changes to rule key-values take effect without restarting either host.

Minimum expected behavior:
- Detect configuration changes using the sentinel key `auto.reload.sentinel`.
- When the sentinel changes, reload rule key-values under the `rules:` prefix.
- Reload the in-memory ruleset when a change is detected.
- Ensure reload is safe for concurrent operations (thread-safe swap of the active ruleset).

## 3.2 `IngestionServiceHost` behavior
- `IngestionServiceHost` must load rules from Azure App Configuration rather than from the file system.
- The existing App Configuration integration already present in `IngestionServiceHost` remains the standard approach.
- Any legacy disk-based configuration for rules must be removed or made inert (no longer wired).

Hot refresh requirement:
- Rule changes in Azure App Configuration MUST be picked up without host restart.

Operational considerations:
- `IngestionServiceHost` must log:
  - Count of rules loaded per provider
  - Count of rules rejected (parse/validation failure)
  - Key identifiers for rejected rules (do not log full JSON unless current logging policy permits)

## 3.3 `RulesWorkbench` behavior
- `RulesWorkbench` must load and display rules from Azure App Configuration.
- `RulesWorkbench` must be able to:
  - List discovered rules grouped by provider
  - Display the raw JSON of a selected rule
  - Validate/parse rules using the same pipeline used by ingestion (where possible)
  - Edit rule JSON
  - Save valid rules back to Azure App Configuration (simple extension)

Loading requirement:
- `RulesWorkbench` MUST load and display all rules discovered in Azure App Configuration, including rules with invalid/unparseable JSON, so that they can be inspected and fixed.

Hot refresh requirement:
- Rule changes in Azure App Configuration MUST be picked up without host restart, and UI listings/validation status must reflect the updated rules.

Diagnostics:
- For each rule, `RulesWorkbench` should show:
  - Key (or provider + rule id)
  - Parse/validation status
  - Error message(s) when invalid

## 3.4 Configuration and environment
### 3.4.1 App Configuration connection
- Both hosts must be configured to connect to the same Azure App Configuration instance(s) appropriate for the environment.
- Configuration source setup (connection string vs managed identity) is environment-specific and expected to be handled by existing host configuration patterns.

### 3.4.2 Key naming contract
The following key naming contract is mandatory for all rules stored in App Configuration:
- `rules:{provider-name}:{rule-id}`

This contract allows:
- Enumeration of all rules via prefix `rules:`
- Filtering by provider via prefix `rules:{provider-name}:`

# 4. Functional Requirements

## 4.1 Rule source
1. Both `IngestionServiceHost` and `RulesWorkbench` MUST load rule definitions from Azure App Configuration.
2. Rules MUST NOT be read from disk-based JSON files.

## 4.2 Key pattern
3. Rules MUST be stored under keys matching `rules:{provider-name}:{rule-id}`.
4. The configuration value for a rule key MUST be treated as the rule JSON.

## 4.3 Enumeration and filtering
5. The system MUST support enumerating all rules by querying keys with prefix `rules:`.
6. The system SHOULD support enumerating rules for a specific provider by querying keys with prefix `rules:{provider-name}:`.

## 4.4 Validation and error reporting
7. If a rule JSON value cannot be parsed or validated, the rule MUST be rejected.
8. Rejected rules MUST produce actionable diagnostic output (logs in `IngestionServiceHost`, visible status in `RulesWorkbench`).

# 5. Technical Requirements

## 5.1 Integration approach
- Use the existing Azure App Configuration integration used by `IngestionServiceHost` as the baseline.
- `RulesWorkbench` must use the same configuration source and access pattern to ensure parity.

## 5.2 Performance
- Rule enumeration should be performed at startup and cached in-memory for:
  - `IngestionServiceHost` runtime ingestion
  - `RulesWorkbench` UI browsing

Refresh behavior is mandatory for rule configuration (see Hot refresh requirements above).

## 5.3 Security
- Access to Azure App Configuration must use established secure configuration practices (managed identity preferred where available).
- Do not emit secrets or full rule JSON in logs unless required for diagnostics and approved by existing logging policy.

# 6. Acceptance Criteria

1. Starting `IngestionServiceHost` with an App Configuration containing keys such as `rules:file-share:adp-ingestion-rule` results in those rules being loaded and used for ingestion.
2. When local disk rule files are present, they are ignored and do not affect loaded rules.
3. `RulesWorkbench` lists the same set of rules as `IngestionServiceHost` (from the same App Configuration instance).
4. Invalid rule JSON stored in App Configuration is reported:
   - In `IngestionServiceHost` logs with the offending key
   - In `RulesWorkbench` with a visible invalid status and parse error

# 7. Open Questions / Decisions

1. Decision: Rule IDs never contain `:` characters (derived from file names during seeding), so keys are always exactly `rules:{provider-name}:{rule-id}`.
2. Decision: Hot refresh is mandatory and will use Azure App Configuration refresh on access (sentinel `auto.reload.sentinel`) rather than a bespoke background polling loop.
3. Decision: `RulesWorkbench` supports editing rule JSON and saving rules back to Azure App Configuration, but only when the rule is valid.
