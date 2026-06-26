# Implementation Plan

Version: `v0.01`
Based on: `dev/work-packages/mvp/045-token-normalization/spec-domain-token-normalization_v0.01.md`
Target output path: `dev/work-packages/mvp/045-token-normalization/plan-domain-token-normalization_v0.01.md`

## Baseline
- `CanonicalDocument` still contains additive `Add...()` mutators alongside misleading `Set...()` aliases.
- Keyword normalization behavior is partially embedded inside `CanonicalDocument` rather than centralized in a reusable domain type.
- `TokenNormalizer` does not yet exist in `src/UKHO.Search/Query/`.
- Production and test call sites still reference `Set...()` members and do not consistently normalize keywords before adding them.
- Existing coverage validates current behavior, but it does not yet isolate `TokenNormalizer` responsibilities in a dedicated test class.

## Delta
- Introduce a concrete `TokenNormalizer` in `src/UKHO.Search/Query/TokenNormalizer.cs` with `IEnumerable<string> NormalizeToken(string? token)`.
- Refactor `CanonicalDocument` so additive APIs remain, `Set...()` and `AddKeywordToken(...)` are removed, and keyword convenience methods delegate through `TokenNormalizer` and `AddKeyword(...)`.
- Update all production and test call sites to use `Add...()` members only and to normalize keyword-producing inputs consistently.
- Add dedicated `TokenNormalizer` unit coverage plus updated `CanonicalDocument` and integration/regression tests.

## Carry-over
- DI registration for `TokenNormalizer` remains out of scope.
- Reuse from `QueryServiceHost` is intentionally deferred to a later work item.
- No search schema or non-keyword normalization changes are planned in this work package.

## Token normalization vertical slices
- [x] Work Item 1: Deliver reusable token normalization and canonical keyword ingestion path - Completed
  - **Purpose**: Establish the smallest end-to-end slice that proves normalized keyword inputs can be expanded, lowercased, and stored correctly through the domain document API.
  - **Completed Summary**: Added `TokenNormalizer`, routed `CanonicalDocument.AddKeywordsFromTokens(...)` through it with explicit delimiter handling, and added dedicated domain plus canonical keyword tests.
  - **Acceptance Criteria**:
    - `TokenNormalizer` exists at `src/UKHO.Search/Query/TokenNormalizer.cs` and exposes `IEnumerable<string> NormalizeToken(string? token)`.
    - `NormalizeToken(...)` trims surrounding whitespace, lowercases with invariant semantics, returns an empty sequence for invalid input, suppresses duplicates, and emits both `s-xxx` and `sxxx` for hyphenated numeric `s-` tokens only.
    - `CanonicalDocument.AddKeyword(...)` continues to reject null, empty, and whitespace-only values and lowercases anything it stores.
    - `CanonicalDocument.AddKeywordsFromTokens(...)` splits only on whitespace, comma, and semicolon, preserves hyphenated tokens, ignores repeated delimiters, invokes `TokenNormalizer` for every split token, and adds all normalized outputs through `AddKeyword(...)`.
    - Dedicated unit tests exist in `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs` using namespace `UKHO.Search.Tests.Query` with representative coverage for `s-57`, `s-63`, `s-100`, and `s-101`.
  - **Definition of Done**:
    - Code implemented in domain and ingestion-facing document model
    - Unit tests added and passing for normalizer and canonical keyword behavior
    - Error handling for invalid token input covered by tests
    - Documentation updated in this work package
    - Can execute end-to-end via: `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj --filter TokenNormalizer|CanonicalDocument`
  - [x] Task 1: Add the dedicated domain normalizer - Completed
    - **Completed Summary**: Created `src/UKHO.Search/Query/TokenNormalizer.cs` with trim, invariant lowercase, invalid-input guards, and one-way `s-###` alias expansion.
    - [x] Step 1: Create `src/UKHO.Search/Query/TokenNormalizer.cs` as a concrete instantiable class with an instance `NormalizeToken(string? token)` API.
    - [x] Step 2: Implement trim + invariant lowercase normalization before rule evaluation.
    - [x] Step 3: Return an empty sequence for null, empty, and whitespace-only input.
    - [x] Step 4: Implement the one-way `s-` followed by digits alias rule so hyphenated input emits both hyphenated and compact lowercase values.
    - [x] Step 5: Ensure output never contains null, blank, or duplicate values and does not rely on a fixed alias order.
  - [x] Task 2: Refactor canonical keyword ingestion to use the normalizer consistently - Completed
    - **Completed Summary**: Updated `CanonicalDocument` to keep `AddKeyword(...)` as the lowercase boundary and to split token strings on whitespace, comma, and semicolon before normalizing each token.
    - [x] Step 1: Remove any embedded keyword alias logic from `CanonicalDocument` that should now live in `TokenNormalizer`.
    - [x] Step 2: Keep `AddKeyword(...)` as the final lowercase and invalid-input safeguard for stored keywords.
    - [x] Step 3: Update `AddKeywordsFromTokens(...)` to split using the explicit delimiter set of whitespace, comma, and semicolon.
    - [x] Step 4: Preserve hyphenated tokens such as `s-100` during splitting.
    - [x] Step 5: Instantiate and invoke `TokenNormalizer` for each split token and route each normalized output through `AddKeyword(...)`.
  - [x] Task 3: Add focused automated coverage for the first runnable slice - Completed
    - **Completed Summary**: Added dedicated `TokenNormalizer` theory coverage and expanded canonical keyword tests for invalid input handling, delimiter behavior, and `s-100` alias storage.
    - [x] Step 1: Add `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs` as a dedicated test class.
    - [x] Step 2: Add data-driven tests covering simple lowercase normalization, whitespace trimming, null/empty behavior, non-matching `s-` prefixes, compact `s100` input, and representative `s-xxx` alias cases.
    - [x] Step 3: Update `CanonicalDocument` tests to assert `AddKeyword(...)` ignores invalid input and lowercases stored values defensively.
    - [x] Step 4: Add coverage proving `AddKeywordsFromTokens(...)` preserves hyphenated tokens, ignores repeated delimiters, and adds alias outputs such as `s100`.
  - **Files**:
    - `src/UKHO.Search/Query/TokenNormalizer.cs`: new reusable token normalization component
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: keyword normalization boundary and convenience API updates
    - `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs`: dedicated normalizer coverage
    - `test/UKHO.Search.Ingestion.Tests/...`: updated canonical document tests where existing coverage resides
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj --filter TokenNormalizer`
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj --filter CanonicalDocument`
  - **User Instructions**: None beyond running the targeted test commands.

- [x] Work Item 2: Remove misleading mutators and align all production callers to additive APIs - Completed
  - **Purpose**: Complete the domain API rationalization so production code uses additive semantics explicitly and keyword-producing paths consistently normalize before mutation.
  - **Completed Summary**: Removed deprecated `CanonicalDocument` mutators, migrated rule and file-share keyword/content call sites to additive APIs with `TokenNormalizer`, and updated regression tests for additive semantics.
  - **Acceptance Criteria**:
    - All `Set...()` members and `AddKeywordToken(...)` are removed from `CanonicalDocument`.
    - Production callers are updated to use `Add...()` members only.
    - No remaining source references to removed APIs exist.
    - All keyword-producing call sites normalize first, even when no special alias expansion is expected.
    - Search text and content behavior remain additive after the call-site migration.
  - **Definition of Done**:
    - Code implemented across impacted production projects
    - Regression coverage updated for changed call sites
    - Logging/error behavior remains unchanged except for intended normalization behavior
    - Documentation updated in this work package
    - Can execute end-to-end via: ingestion/domain tests proving rule application still populates canonical document fields correctly
  - [x] Task 1: Remove redundant and misleading `CanonicalDocument` APIs - Completed
    - **Completed Summary**: Deleted all `Set...()` aliases plus `AddKeywordToken(...)` and kept the public document surface additive-only.
    - [x] Step 1: Delete all `SetAuthority(...)`, `SetRegion(...)`, `SetFormat(...)`, `SetMajorVersion(...)`, `SetMinorVersion(...)`, `SetCategory(...)`, `SetSeries(...)`, `SetInstance(...)`, `SetKeyword(...)`, `SetKeywordsFromTokens(...)`, `SetSearchText(...)`, and `SetContent(...)` members.
    - [x] Step 2: Delete `AddKeywordToken(...)` and keep `AddKeywords(IEnumerable<string?>?)` as a simple loop over `AddKeyword(...)`.
    - [x] Step 3: Confirm public surface area and XML/docs reflect additive-only semantics.
  - [x] Task 2: Update all production call sites to the approved API and normalization flow - Completed
    - **Completed Summary**: Switched ingestion rule and file-share enrichment paths to additive APIs and normalized keyword-producing inputs with the shared `TokenNormalizer`.
    - [x] Step 1: Replace ingestion rule action applier usage of `SetSearchText(...)` and `SetContent(...)` with `AddSearchText(...)` and `AddContent(...)`.
    - [x] Step 2: Find every keyword-producing path in ingestion/file-share/rule flows and ensure each invokes `TokenNormalizer` before `AddKeyword(...)` or relies on `AddKeywordsFromTokens(...)` to do so.
    - [x] Step 3: Preserve existing additive behavior for authority, region, format, version, category, series, and instance fields while changing the API calls.
    - [x] Step 4: Verify no remaining references to removed members exist in source projects.
  - [x] Task 3: Update impacted tests to match the rationalized API - Completed
    - **Completed Summary**: Rewrote additive field tests, added coverage for non-normalizing `AddKeywords(...)`, and extended rule integration assertions for additive search text/content and `s-100` keyword expansion.
    - [x] Step 1: Rewrite `Set...()`-focused tests to assert the corresponding `Add...()` methods directly.
    - [x] Step 2: Update rule action applier and ingestion tests to validate search text/content still append as expected.
    - [x] Step 3: Add or update tests showing `AddKeywords(IEnumerable<string?>?)` remains a simple additive wrapper without internal normalization.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: remove deprecated mutators and redundant API
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/IngestionRulesActionApplier.cs`: switch to additive APIs
    - `src/UKHO.Search.Ingestion.Providers.FileShare/...`: normalize keyword-producing file-share flows where applicable
    - `test/...`: update all references to removed APIs and assert additive behavior directly
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Verify no references remain by searching for `SetSearchText(`, `SetContent(`, `SetKeyword(`, and `AddKeywordToken(` in the solution.
  - **User Instructions**: None beyond running the full test suite if desired.

- [x] Work Item 3: Add regression confidence for end-to-end ingestion and search-recall behavior - Completed
  - **Purpose**: Ensure the finished change preserves ingestion behavior while improving recall for well-known domain tokens such as `s-100`.
  - **Completed Summary**: Added regression coverage for rule-based and file-share enrichment flows, verified lowercase storage across all additive string fields, confirmed all keyword-producing call sites normalize first, and validated the full solution build/test pass.
  - **Acceptance Criteria**:
    - Integration/regression tests prove ingestion rule application still populates canonical fields correctly after the `Set...()` removal.
    - Keyword-producing flows are verified to normalize before mutation across all impacted call sites.
    - Search-recall expectations for `s-100` style values are covered where appropriate through canonical keyword assertions.
    - Full solution build and relevant tests pass without remaining compile errors from removed APIs.
  - **Definition of Done**:
    - Integration and regression tests implemented and passing
    - Build validation completed
    - End-to-end verification steps documented
    - Can execute end-to-end via: build + targeted ingestion/query regression tests demonstrating normalized token storage
  - [x] Task 1: Strengthen integration coverage around ingestion flows - Completed
    - **Completed Summary**: Extended rule and batch enrichment integration tests to assert `s-100`/`s100` recall behavior and lowercase storage across keywords, search text, content, and additive taxonomy fields.
    - [x] Step 1: Update ingestion integration tests that build canonical documents from rule application and enrichment flows.
    - [x] Step 2: Assert canonical keyword sets include both `s-100` and `s100` when hyphenated numeric input is provided.
    - [x] Step 3: Assert lowercase storage remains enforced across all additive `CanonicalDocument.Add...()` methods regardless of caller casing, including keywords, search text, content, and every other additive field stored by the document.
  - [x] Task 2: Validate repository-wide compatibility after API removal - Completed
    - **Completed Summary**: Updated the remaining `BasicEnricher` keyword path to use `TokenNormalizer`, verified all `AddKeyword(...)` production call sites normalize first, and passed full build plus solution-wide tests.
    - [x] Step 1: Build the solution and resolve any compilation failures caused by removed mutators.
    - [x] Step 2: Run relevant domain, ingestion, and infrastructure test projects affected by canonical document mutation.
    - [x] Step 3: Confirm no deferred code path still bypasses normalization for keyword additions.
  - [x] Task 3: Finalize work package documentation for implementation readiness - Completed
    - **Completed Summary**: Confirmed the plan remains aligned to `spec-domain-token-normalization_v0.01.md`; no scope-expanding spec updates were required, and final verification commands remain `dotnet build` plus `dotnet test`.
    - [x] Step 1: Ensure this plan remains aligned to the source spec version.
    - [x] Step 2: Record any implementation discoveries as updates to the spec or a later version if scope changes.
    - [x] Step 3: Capture final run/verification commands for maintainers.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/...`: ingestion integration/regression updates
    - `test/UKHO.Search.Tests/...`: shared domain regression assertions if needed
    - `dev/work-packages/mvp/045-token-normalization/plan-domain-token-normalization_v0.01.md`: implementation plan maintenance
  - **Work Item Dependencies**: Work Item 2
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
    - Execute targeted regression tests covering canonical keyword generation for `s-100` inputs.
  - **User Instructions**: None.

## Overall approach summary
This plan delivers the change in three vertical slices: first establish the reusable normalization engine and canonical keyword path, then remove misleading mutators and migrate production callers, and finally harden regression coverage across ingestion flows. The key implementation considerations are preserving additive semantics, keeping lowercase enforcement inside `CanonicalDocument` as a defensive boundary, centralizing alias logic in `TokenNormalizer`, and proving search-recall improvements through focused automated tests.