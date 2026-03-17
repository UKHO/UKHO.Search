---
Title: Implementation Plan - `IngestionServiceHost` and `RulesWorkbench` rule sourcing via Azure App Configuration (with hot refresh)
Work Package: 041-ingestion-workbench-config-rules
Based on: `spec-hosts-ingestionservicehost-rulesworkbench-config-rules_v0.01.md`
Document Type: Implementation Plan
Version: v0.01
Status: Draft
Last Updated: 2026-03-17
---

# Implementation Plan

## Work Package Structure
- `docs/041-ingestion-workbench-config-rules/`
  - `spec-hosts-ingestionservicehost-rulesworkbench-config-rules_v0.01.md`
  - `plan-hosts-ingestionservicehost-rulesworkbench-config-rules_v0.01.md` (this document)

## Feature Slice 1: Rules hot refresh plumbing (sentinel-based) end-to-end
- [x] Work Item 1: Wire Azure App Configuration rule sourcing (startup load only; hot refresh deferred) - Completed
  - **Purpose**: Ensure both hosts can source rules from Azure App Configuration and load them on startup. Hot refresh via `auto.reload.sentinel` is deferred.
  - **Acceptance Criteria**:
    - Both hosts connect to Azure App Configuration via `builder.AddConfiguration(...)`.
    - Both hosts load rules from keys under `rules:` on startup.
    - (Deferred) Hot refresh via sentinel.
  - **Definition of Done**:
    - Startup load from App Configuration implemented
    - Logs show initial rule load occurred (counts per provider; rejected count)
    - Unit tests for load/reload behavior (where feasible)
    - Can execute end-to-end via: run host(s) and verify rules from App Config are loaded
  - [x] Task 1.1: Identify and reuse existing App Configuration refresh approach used by `IngestionServiceHost` - Completed
    - [x] Step 1: Locate the `IngestionServiceHost` App Configuration setup (startup/host builder) - Completed
    - [x] Step 2: Confirm sentinel registration capability and current refresh middleware usage (if web host) - Completed
    - [x] Step 3: Document baseline behavior in this plan (notes section) - Completed
  - [x] Task 1.2: Add Azure App Configuration wiring to `RulesWorkbench` (startup load) - Completed
    - [x] Step 1: Add Azure App Configuration provider configuration matching `IngestionServiceHost` - Completed
    - [x] Step 2: Ensure rules are loaded from App Configuration on startup - Completed
    - [ ] Step 3: (Deferred) Sentinel-based refresh activation
  - [x] Task 1.3: Implement a reloadable ruleset abstraction used by both hosts (startup load now; hot refresh later) - Completed
    - [x] Step 1: Introduce/identify a rules provider that can load from `IConfiguration`/App Config values - Completed
    - [x] Step 2: Implement thread-safe swap (e.g., `Volatile.Read` / `Interlocked.Exchange` on an immutable snapshot) so it is ready for later hot refresh - Completed
    - [x] Step 3: Emit structured logs on load/reload (counts per provider; rejected count) - Completed
  - **Files** (expected; validate actual paths during implementation):
    - `src/Hosts/IngestionServiceHost/*`: ensure sentinel/refresh usage is correct for rules reload
    - `src/Hosts/RulesWorkbench/*`: add/align App Config + refresh wiring
    - `src/UKHO.Search.Ingestion/*` or `src/UKHO.Search.Services.*/*`: reloadable rules provider + snapshot type (keep Onion dependencies)
    - `src/**/Tests/*`: unit tests around reload/snapshot swap
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - Run `IngestionServiceHost`
    - Run `RulesWorkbench`
    - Verify logs/UI indicate rules loaded from App Configuration
  - **User Instructions**:
    - Ensure local settings point both hosts at the same Azure App Configuration instance
    - (Deferred) In Azure App Configuration, create/modify `auto.reload.sentinel` to trigger refresh

### Notes (Work Item 1 baseline)
- `IngestionServiceHost` already connects to Azure App Configuration via `builder.AddConfiguration(...)`.
  - `configuration/UKHO.Aspire.Configuration/ConfigurationExtensions.cs` registers refresh sentinel `auto.reload.sentinel` (via `WellKnownConfigurationName.ReloadSentinelKey`) and sets refresh interval.
  - Full request-pipeline refresh middleware (`UseAzureAppConfiguration`) is not wired.
- `RulesWorkbench` already called `builder.AddConfiguration(...)`. As part of Work Item 1 it now also forces the ingestion rules engine to load at startup by invoking `IIngestionRulesCatalog.EnsureLoaded()`.
- Current rule browsing/editing UI in `RulesWorkbench` still loads per-rule JSON from the local `Rules/` folder via `RulesSnapshotStore`; migrating UI to App Configuration is deferred to Work Item 2.

**Summary of changes**:
- `tools/RulesWorkbench/Program.cs`: ensured ingestion rules engine startup-load occurs via `IIngestionRulesCatalog.EnsureLoaded()`; updated to block-scoped namespace / Allman style.

### Hot refresh implementation (added after Work Item 1)
- Implemented **automatic background refresh** for both `IngestionServiceHost` (queue-driven) and `RulesWorkbench` (Blazor) without relying on HTTP middleware.
- Added `AppConfigIngestionRulesRefreshService` hosted service which periodically calls Azure App Configuration `TryRefreshAsync()` and, when a refresh occurs (sentinel change), triggers an atomic rules reload.
- Updated `IngestionRulesCatalog` to support thread-safe reload via `Interlocked.Exchange` of an immutable snapshot.

### App Configuration rule value compatibility (added after Work Item 3)
- App Configuration `rules:{provider}:{ruleId}` values may be stored either as:
  - a raw rule object, or
  - a wrapped document `{ schemaVersion: "1.0", rule: { ... } }` (matches legacy per-file rule document format).
- Both ingestion runtime and `RulesWorkbench` unwrap the wrapped document automatically before validation/evaluation.


## Feature Slice 2: Rules read from App Configuration (no disk) end-to-end
- [x] Work Item 2: Remove/disable disk-based rule loading and load from `rules:{provider}:{rule-id}` keys - Completed
  - **Purpose**: Ensure only App Configuration is used as the source of truth for rules.
  - **Acceptance Criteria**:
    - `IngestionServiceHost` no longer loads rule JSON from disk.
    - `RulesWorkbench` no longer loads rule JSON from disk.
    - Rules are discovered from keys with prefix `rules:`.
    - Invalid rules are handled per host requirements:
      - `IngestionServiceHost`: reject invalid rules
      - `RulesWorkbench`: load and display invalid rules
  - **Definition of Done**:
    - All rule reads are from App Config
    - Legacy disk pathways removed or inert
    - Tests updated to reflect App Config sourcing
    - Documentation updated if any host config values changed
    - Can execute end-to-end via: run hosts and view/apply rules stored in App Config
  - [x] Task 2.1: Align rule key enumeration and parsing - Completed
    - [x] Step 1: Implement/confirm key parsing assumes exactly 3 segments `rules:{provider}:{ruleId}` - Completed
    - [x] Step 2: Implement enumeration by prefix `rules:` - Completed
    - [x] Step 3: Create a typed in-memory model representing a rule entry (key + provider + ruleId + raw JSON + validation status) - Completed
  - [x] Task 2.2: `IngestionServiceHost` - strict validation and rejection - Completed
    - [x] Step 1: Ensure invalid JSON/schema results in rejection (not loaded into active ruleset) - Completed
    - [x] Step 2: Ensure diagnostics include key and parse/validation error - Completed
  - [x] Task 2.3: `RulesWorkbench` - tolerant loading for invalid rules - Completed
    - [x] Step 1: Load all discovered rules regardless of validity - Completed
    - [x] Step 2: Display validation status and error details - Completed
    - [x] Step 3: Ensure UI continues to function when some rules are invalid - Completed
  - **Files** (expected; validate actual paths during implementation):
    - `src/Hosts/IngestionServiceHost/*`: remove disk rule loading
    - `src/Hosts/RulesWorkbench/*`: remove disk rule loading
    - `src/UKHO.Search.Ingestion/*`: rule parsing/validation pipeline integration

**Summary of changes (Work Item 2)**:
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/IngestionRulesLoader.cs`: switched to App Configuration sourcing only (`rules:{provider}:{ruleId}`); disk-based rules directory paths are now inert.
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`: added rule source abstractions (`IRulesSource`, `AppConfigRulesSource`, `IngestionRulesSource`) and shared DTO (`RuleEntryDto`).
- `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`: updated DI to use App Config rule source; removed `RuleFileLoader` registrations.
- `tools/RulesWorkbench/Services/*`: added `AppConfigRulesSnapshotStore` + `RulesWorkbenchRuleEntry` for tolerant App Config rule listing (includes invalid rules).
- `tools/RulesWorkbench/Components/Pages/Rules.razor`: now lists/edits rules from App Configuration (no disk reads); shows invalid status/error.
- Tests: ran `RulesWorkbench.Tests`.

## Feature Slice 3: `RulesWorkbench` save-back of valid rules to App Configuration end-to-end
- [x] Work Item 3: Add save-back of valid edited rules into Azure App Configuration - Completed
  - **Purpose**: Preserve existing editing workflow and add simple persistence (valid-only save) to App Configuration.
  - **Acceptance Criteria**:
    - User can edit rule JSON in `RulesWorkbench`.
    - `RulesWorkbench` validates the JSON; save is enabled only when valid.
    - Saving writes the JSON to the correct App Configuration key `rules:{provider}:{rule-id}`.
    - After save, `auto.reload.sentinel` is updated (or otherwise refresh is triggered) so both hosts observe the change.
  - **Definition of Done**:
    - Save UI present and functional
    - Backend logic writes to App Configuration
    - Errors surfaced to user on save failure (auth/connection/validation)
    - E2E verification steps documented
  - [x] Task 3.1: Identify `RulesWorkbench` current editing model - Completed
    - [x] Step 1: Locate the component/page that edits the rule JSON - Completed
    - [x] Step 2: Ensure edits are local until saved - Completed
    - [x] Step 3: Identify current validation hook points - Completed
  - [x] Task 3.2: Implement App Configuration write client - Completed
    - [x] Step 1: Add an infrastructure/service abstraction for setting key-values in App Configuration - Completed
    - [x] Step 2: Ensure Onion dependencies: host depends on interface; infrastructure implements - Completed
    - [x] Step 3: Implement `SetRule(provider, ruleId, json)` and `TouchSentinel()` (set `auto.reload.sentinel`) - Completed
  - [x] Task 3.3: Wire save action in Blazor UI - Completed
    - [x] Step 1: Add Save button (enabled only when rule is valid) - Completed
    - [x] Step 2: Call write client; show success/failure toast/message - Completed
    - [x] Step 3: After save, reload view model to reflect latest persisted value - Completed
  - [x] Task 3.4: Tests - Completed
    - [x] Step 1: Unit tests for key generation and validity gating - Completed
    - [x] Step 2: Integration tests against a test double for App Configuration client - Completed
    - [ ] Step 3: (Optional) Playwright smoke test for save UX if the repo has existing Playwright coverage for `RulesWorkbench`
  - **Files** (expected; validate actual paths during implementation):
    - `src/Hosts/RulesWorkbench/*`: UI save button + validation gating + error handling
    - `src/UKHO.Search.Services.*/*`: interface for rule config writer (if appropriate)
    - `src/UKHO.Search.Infrastructure.*/*`: App Configuration writer implementation
    - `src/**/Tests/*`: unit/integration tests
  - **Work Item Dependencies**: Work Item 1 and Work Item 2
  - **Run / Verification Instructions**:
    - Run `RulesWorkbench`
    - Open rule editor, paste valid JSON, click Save
    - Verify App Configuration key `rules:{provider}:{rule-id}` updated and sentinel `auto.reload.sentinel` changed
    - Verify `IngestionServiceHost` logs show reload and rule takes effect

**Summary of changes (Work Item 3)**:
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/IRuleConfigurationWriter.cs`: added writer abstraction for persisting rule JSON + touching sentinel.
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigRuleConfigurationWriter.cs`: Azure App Configuration implementation using `Azure.Data.AppConfiguration.ConfigurationClient` (connection string for local emulator; `DefaultAzureCredential` for non-local).
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/AppConfigEndpointResolver.cs`: resolves App Config endpoint from Aspire-provided `services__adds-configuration__http__0`.
- `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`: registers writer + endpoint resolver for both ingestion and workbench hosts.
- `tools/RulesWorkbench/Components/Pages/Rules.razor`: added Save button (valid-only) that writes `rules:{provider}:{ruleId}` and touches `auto.reload.sentinel`; displays success/failure message.
- `tools/RulesWorkbench/RulesWorkbench.csproj` + `src/UKHO.Search.Infrastructure.Ingestion/UKHO.Search.Infrastructure.Ingestion.csproj`: added `Azure.Data.AppConfiguration` package.
- `test/RulesWorkbench.Tests/RuleKeyParserTests.cs`: added unit test coverage for 3-segment key parsing.
- Tests: ran `RulesWorkbench.Tests`.

# Architecture

## Overall Technical Approach
- Centralize rule retrieval in a single reloadable rules provider backed by Azure App Configuration (`rules:` prefix) and wired into both hosts.
- Use Azure App Configuration refresh-on-access with sentinel key `auto.reload.sentinel` to trigger reload without restarts.
- Maintain an immutable ruleset snapshot in memory and swap atomically on refresh.

```mermaid
flowchart LR
  A[Azure App Configuration\nkeys: rules:* + auto.reload.sentinel] -->|IConfiguration + refresh| B(IngestionServiceHost)
  A -->|IConfiguration + refresh| C(RulesWorkbench)
  B --> D[Reloadable RulesProvider\n(immutable snapshot)]
  C --> D
  C --> E[Blazor UI\nList/Edit/Validate/Save]
  E -->|valid only| F[App Config Write Client\nSet rules:* + touch sentinel]
  F --> A
```

## Frontend
- `RulesWorkbench` (Blazor) provides:
  - List rules grouped by provider
  - View raw JSON + validation errors
  - Edit JSON
  - Save valid JSON back to Azure App Configuration
- Hot refresh: UI refreshes listing/status when rules reload is triggered by sentinel.

## Backend
- A shared rules provider abstraction is used by both hosts to:
  - enumerate `rules:` keys
  - parse/validate rule JSON
  - expose both “valid ruleset for runtime” and “all rules (including invalid) for tooling”
- An App Configuration writer abstraction supports `RulesWorkbench` save-back, including touching `auto.reload.sentinel` post-write.
