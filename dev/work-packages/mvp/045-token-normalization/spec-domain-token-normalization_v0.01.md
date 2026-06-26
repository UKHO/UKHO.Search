# Token normalization and `CanonicalDocument` mutator rationalization

Version: `v0.01`
Work Package: `045-token-normalization`
Target output path: `dev/work-packages/mvp/045-token-normalization/spec-domain-token-normalization_v0.01.md`

## Change Log

- `v0.01`
  - initial draft covering `CanonicalDocument` mutator consolidation and proposed `TokenNormalizer`
  - records current evidence from `CanonicalDocument`, rule action application, and ingestion tests
  - records domain confirmation that `TokenNormalizer.NormalizeToken(...)` must return lowercase-only values because everything indexed is lowercase
  - records domain confirmation that invalid `TokenNormalizer` input returns an empty sequence
  - records requirement that `CanonicalDocument.AddKeyword(...)` must continue to ignore null/empty/whitespace tokens and be covered by tests
  - records domain confirmation that special token aliasing applies only to `s-` followed by digits
  - records domain confirmation that `CanonicalDocument` must ensure anything it stores is lowercased
  - records domain confirmation that `TokenNormalizer` is mandatory at every keyword call site, even when no alias expansion is expected
  - records domain confirmation that alias output order is irrelevant and the implementation should prefer the fastest approach
  - records domain confirmation that dedicated benchmark coverage is not required; efficient implementation plus normal tests is sufficient
  - records domain confirmation that surrounding whitespace should be trimmed before lowercase normalization and alias evaluation
  - records domain confirmation that `AddKeywordToken(...)` should be removed, while `AddKeywordsFromTokens(...)` should remain as a convenience API that loops through `AddKeyword(...)`
  - records domain confirmation that `AddKeywordsFromTokens(...)` should split on punctuation separators as well as whitespace
  - records domain confirmation that hyphen must be excluded from punctuation splitting so tokens such as `s-100` remain intact
  - records domain confirmation that `AddKeywordsFromTokens(...)` should use a fixed explicit delimiter set rather than treating all punctuation except hyphen as separators
  - records domain confirmation that the fixed explicit delimiter set for `AddKeywordsFromTokens(...)` is comma and semicolon, in addition to whitespace
  - records domain confirmation that `AddKeywordsFromTokens(...)` must invoke `TokenNormalizer` for each split token so alias expansion is preserved through the convenience API
  - records domain confirmation that `TokenNormalizer` should live at `src/UKHO.Search/Query/TokenNormalizer.cs`
  - records domain confirmation that `TokenNormalizer` tests should live in `test/UKHO.Search.Tests`
  - records domain intent that the same `TokenNormalizer` implementation should be reusable from `QueryServiceHost` in a later work item
  - records domain confirmation that `CanonicalDocument.AddKeywords(IEnumerable<string?>?)` should remain a simple loop over `AddKeyword(...)`, with callers responsible for prior normalization
  - records domain confirmation that alias expansion is one-way only: compact input such as `s100` must not emit `s-100`
  - records domain confirmation that repeated delimiters and empty token segments in `AddKeywordsFromTokens(...)` should be ignored
  - records domain confirmation that `TokenNormalizer` should be a normal instantiable class with an instance method only and no additional static helper API
  - records domain confirmation that no additional well-known token patterns are in scope for this work item beyond the `s-xxx` rule
  - records domain confirmation that `TokenNormalizer` tests must be in their own dedicated test class and should use explicit representative data-driven coverage for values such as `s-57`, `s-63`, `s-100`, and `s-101`
  - records domain confirmation that `TokenNormalizer` tests must live at `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs` and must not be mixed into unrelated test classes
  - records domain confirmation that the dedicated test namespace must be `UKHO.Search.Tests.Query`
  - records domain confirmation that existing `Set...()`-focused tests should be rewritten to assert the corresponding `Add...()` methods directly
  - records domain confirmation that `TokenNormalizer.NormalizeToken(...)` does not need to preserve the original string instance; returning a newly normalized value is acceptable so long as behavior is correct
  - records domain confirmation that the public API may be `IEnumerable<string> NormalizeToken(string? token)` so null input can be represented explicitly
  - records domain confirmation that lowercase normalization should continue to use invariant semantics, matching the current `CanonicalDocument` behavior

## 1. Overview

### 1.1 Purpose

Define the required domain and ingestion changes to:

- remove semantically misleading `Set...()` mutators from `CanonicalDocument`
- introduce a dedicated `TokenNormalizer` in the `UKHO.Search.Query` namespace within the `UKHO.Search` project
- ensure keyword token normalization is explicit, deterministic, performant, and testable
- preserve or improve search recall for well-known domain tokens such as `s-100`

### 1.2 Scope

In scope:

- `CanonicalDocument` mutator API rationalization
- call site updates for all production and test code using removed `Set...()` members
- new `TokenNormalizer` design and expected behavior
- keyword normalization responsibilities and boundaries
- unit/integration test expectations impacted by the normalization change
- performance expectations for normalization on hot paths

Out of scope:

- unrelated canonical field model changes
- dependency injection registration for `TokenNormalizer`
- non-keyword normalization behavior unless required by impacted call sites
- search schema changes

### 1.3 Stakeholders

- Search domain maintainers
- Ingestion pipeline maintainers
- Query/search behavior consumers
- Test and quality owners for ingestion/query behavior

### 1.4 Definitions

- `CanonicalDocument`: canonical search payload assembled during ingestion
- `Set...()` mutator: existing `CanonicalDocument` method whose current implementation appends rather than replaces
- `TokenNormalizer`: proposed domain utility responsible for deriving normalized token variants
- well-known domain token: token with explicit domain-aware aliasing rules, initially `s-xxx`

## 2. System context

### 2.1 Current state

Observed evidence:

- `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs` contains additive `Add...()` mutators and matching `Set...()` aliases that currently delegate to `Add...()` rather than replacing values.
- `CanonicalDocument.AddKeyword(...)` currently lowercases and trims internally via a private `NormalizeToken(...)` helper.
- `CanonicalDocument.AddSearchText(...)` and `CanonicalDocument.AddContent(...)` also lowercase and trim internally.
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/IngestionRulesActionApplier.cs` currently calls `document.SetSearchText(...)` and `document.SetContent(...)`.
- ingestion tests currently assert lowercase behavior inside `CanonicalDocument` and exercise several `Set...()` APIs directly.
- the `src/UKHO.Search/UKHO.Search.csproj` domain project does not currently contain a `Query` folder or a `TokenNormalizer` implementation.

### 2.2 Proposed state

The proposed target state is:

- `CanonicalDocument` exposes only additive mutators for additive fields; all `Set...()` mutators are removed.
- all existing production/test callers are updated to use the relevant `Add...()` member.
- a new `TokenNormalizer` class is added under the `UKHO.Search.Query` namespace in the `UKHO.Search` project.
- `TokenNormalizer` remains the explicit source of domain-aware keyword expansion, but `CanonicalDocument` remains responsible for ensuring values it stores are lowercased.
- `TokenNormalizer.NormalizeToken(string token)` returns one or more normalized tokens for indexing/search use.
- `TokenNormalizer.NormalizeToken(string? token)` returns one or more normalized tokens for indexing/search use.
- for well-known `s-xxx` tokens, both the hyphenated and compact forms are produced.
- `TokenNormalizer` is instantiated directly and is not registered in DI.
- all keyword-producing call sites must invoke `TokenNormalizer`, even when a given token does not trigger special alias expansion.
- `AddKeywordsFromTokens(...)` preserves this rule by invoking `TokenNormalizer` for each split token before storing results.
- `TokenNormalizer` is placed in the shared domain project so it can be reused by future `QueryServiceHost` work without duplication.
- `TokenNormalizer` exposes only its normal instance API; no additional static helper API is required.

### 2.3 Assumptions

- additive semantics are correct for keyword, taxonomy, search-text, and content population.
- removing misleading `Set...()` APIs is safe provided all call sites and tests are updated.
- lowercased token normalization remains the canonical search/index matching strategy.
- all values emitted by `TokenNormalizer` for index use are lowercase only; the literal caller input is not preserved when it differs by casing.
- invalid token input such as null, empty, or whitespace returns an empty sequence rather than throwing.
- domain-aware aliasing is initially limited to `s-` followed by digits, such as `s-57`, `s-63`, `s-100`, and `s-101`.
- `CanonicalDocument` acts as the final safeguard and must ensure any stored string value is lowercased.
- alias output ordering is not semantically important; implementation and tests should prefer the most efficient approach rather than enforcing a fixed order.
- `TokenNormalizer` is intended to be allocation-conscious because keyword processing is on a hot ingestion path.

### 2.4 Constraints

- the repository uses Onion Architecture; the new normalizer must live in the domain project and not depend on infrastructure.
- the design must not require DI registration.
- the normalization design must be easy to unit test in isolation.
- performance is explicitly important.

## 3. Component / service design (high level)

### 3.1 Components

1. `CanonicalDocument` (`src/UKHO.Search.Ingestion`)
   - remove all `Set...()` mutators
   - retain additive APIs only
   - continue lowercasing defensively for stored string values
2. `TokenNormalizer` (`src/UKHO.Search` / `UKHO.Search.Query` namespace)
   - provide `IEnumerable<string> NormalizeToken(string? token)`
   - lowercase input deterministically
   - emit primary token plus domain aliases
   - live at `src/UKHO.Search/Query/TokenNormalizer.cs` for later reuse by `QueryServiceHost`
3. ingestion/query call sites
   - ensure callers normalize tokens before calling `AddKeyword(...)`
   - update code/tests to use `Add...()` APIs only
4. test suites
   - add rigorous unit coverage for `TokenNormalizer`
   - place `TokenNormalizer` tests in `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs`
   - keep `TokenNormalizer` tests in their own dedicated test class and do not mix them into unrelated test files
   - use namespace `UKHO.Search.Tests.Query`
   - update existing `CanonicalDocument` and integration tests to match the new responsibility split

### 3.2 Data flows

1. caller obtains raw token text
2. caller invokes `new TokenNormalizer().NormalizeToken(token)`
3. caller passes each returned token variant into `CanonicalDocument.AddKeyword(...)`
4. `CanonicalDocument` stores tokens in lowercase as a final defensive safeguard
5. downstream search/index behavior benefits from both canonical and domain alias forms

For `AddKeywordsFromTokens(...)`:

1. caller supplies token text containing whitespace and/or configured punctuation delimiters
2. `CanonicalDocument` splits using the fixed delimiter set while preserving hyphenated tokens
3. each split token is passed through `TokenNormalizer`
4. each normalized output token is added through `AddKeyword(...)`

### 3.3 Key decisions

- normalization logic is extracted from `CanonicalDocument` into a dedicated reusable type
- `Set...()` APIs are removed rather than retained as misleading aliases
- `TokenNormalizer` remains concrete and directly instantiated
- the initial well-known aliasing rule is limited to `s-xxx` style tokens
- performance must be considered in API and implementation choices

## 4. Functional requirements

### FR1. `CanonicalDocument` mutator consolidation

- Remove all `Set...()` methods from `CanonicalDocument`.
- Update all production callers and tests to use the corresponding `Add...()` method.
- No remaining source or test code should reference removed `Set...()` members.
- Remove `AddKeywordToken(...)` as redundant API surface.
- Retain `AddKeywordsFromTokens(...)` as a convenience API that splits tokens and delegates to `AddKeyword(...)` for each value.
- `AddKeywordsFromTokens(...)` must support token splitting on punctuation separators as well as whitespace.
- `AddKeywordsFromTokens(...)` must not split on hyphen; hyphenated domain tokens such as `s-100` must remain intact for downstream normalization.
- `AddKeywordsFromTokens(...)` must use a fixed explicit delimiter set.
- The fixed explicit delimiter set is whitespace, comma, and semicolon.
- `AddKeywordsFromTokens(...)` must invoke `TokenNormalizer` for each split token before adding the resulting normalized tokens.
- Repeated delimiters and empty token segments must be ignored.

### FR2. New `TokenNormalizer`

- Add a `TokenNormalizer` class in the `UKHO.Search.Query` namespace within the `UKHO.Search` project.
- Expose a method `IEnumerable<string> NormalizeToken(string? token)`.
- The method must be callable via direct construction and must not depend on DI.
- No additional static helper API is required.

### FR3. Baseline token normalization

- `NormalizeToken(...)` must lowercase input token values deterministically using invariant lowercasing semantics.
- `NormalizeToken(...)` must trim surrounding whitespace before applying lowercase normalization and alias rules.
- The normalized primary token must always be included in the returned values.
- The method must return lowercase-only values; it must not preserve original caller casing.
- The method does not need to preserve the original string instance when the input is already lowercase and non-special.
- For null, empty, or whitespace-only input, the method must return an empty sequence.
- The method must not return null, blank, or duplicate values.

### FR4. Well-known domain aliasing

- When the normalized token matches the `s-` followed by digits pattern, the returned values must include:
  - the hyphenated form
  - the compact form with the hyphen removed
- Example target behavior: `s-100` yields both `s-100` and `s100`.
- Tokens merely beginning with `s-` but not followed by digits are not subject to this aliasing rule.
- Compact input such as `s100` is not expanded back to `s-100`; alias expansion applies only when the input is already hyphenated.
- No additional well-known token patterns are included in scope for this work item.

### FR5. Keyword normalization responsibility

- `CanonicalDocument.AddKeyword(...)` must ensure stored keyword values are lowercased.
- Keyword-producing call sites must use `TokenNormalizer` before adding tokens so domain-aware expansion is applied consistently.
- The specification must explicitly verify all call sites that add keywords and ensure they normalize first.
- `CanonicalDocument.AddKeyword(...)` must still reject null, empty, and whitespace-only tokens so invalid values are never added to `Keywords`.
- This requirement applies universally to keyword additions; callers must not bypass `TokenNormalizer` merely because a token appears simple.
- `AddKeywordsFromTokens(...)` is not an exception; it must apply `TokenNormalizer` internally for every split token.
- `AddKeywords(IEnumerable<string?>?)` remains a simple loop over `AddKeyword(...)` and does not apply `TokenNormalizer` internally; callers using this API remain responsible for prior normalization.

### FR6. Canonical lowercase enforcement

- `CanonicalDocument` must ensure that any string value it stores is lowercased.
- This lowercase enforcement applies even when callers already provide lowercase values.
- This requirement is defensive and prevents incorrect casing from entering the index if a caller bypasses the intended normalization flow.
- `CanonicalDocument` should continue to use invariant lowercasing semantics, matching current behavior.

### FR7. Existing additive behaviors

- `CanonicalDocument.Add...()` APIs remain additive.
- Search text and content append semantics remain additive unless implementation evidence requires otherwise.

### FR8. Testing

- Add a rigorous test suite for `TokenNormalizer`.
- Update existing tests impacted by removal of `Set...()` methods or by the shift of keyword lowercasing responsibility.
- Include coverage for duplicate elimination, whitespace handling, casing normalization, and `s-xxx` alias generation.
- Ensure there is explicit test coverage proving `CanonicalDocument.AddKeyword(...)` ignores null, empty, and whitespace-only input; add such a test if coverage does not already exist.
- Ensure there is coverage proving `CanonicalDocument` lowercases stored string values defensively.

## 5. Non-functional requirements

### NFR1. Performance

- Token normalization must be optimized for hot-path use during ingestion.
- Avoid unnecessary allocations where practical.
- Avoid regex if a simpler character-based implementation satisfies the rules more efficiently.
- Do not introduce DI overhead for this component.

### NFR2. Determinism

- Returned token variants must be reproducible for identical input, but callers must not rely on a specific alias ordering where multiple normalized variants are produced.

### NFR3. Maintainability

- Domain-specific token rules must be centralized in `TokenNormalizer`.
- Future well-known token aliases should be extensible without reintroducing normalization logic into multiple callers.

### NFR4. Compatibility

- Production and test projects must compile cleanly after `Set...()` removal.
- Existing additive ingestion behavior must remain intact apart from the intended normalization responsibility shift.
- The `TokenNormalizer` location and dependencies must allow later reuse from `QueryServiceHost` without moving the type or duplicating logic.

## 6. Data model

No persistent schema change is proposed.

In-memory behavioral changes:

- `CanonicalDocument.Keywords` remains a `SortedSet<string>`.
- token normalization output becomes a sequence of candidate keyword values produced prior to document mutation.

## 7. Interfaces & integration

### 7.1 Public API changes

- Remove `CanonicalDocument.SetAuthority(...)`
- Remove `CanonicalDocument.SetRegion(...)`
- Remove `CanonicalDocument.SetFormat(...)`
- Remove `CanonicalDocument.SetMajorVersion(...)`
- Remove `CanonicalDocument.SetMinorVersion(...)`
- Remove `CanonicalDocument.SetCategory(...)`
- Remove `CanonicalDocument.SetSeries(...)`
- Remove `CanonicalDocument.SetInstance(...)`
- Remove `CanonicalDocument.SetKeyword(...)`
- Remove `CanonicalDocument.SetKeywordsFromTokens(...)`
- Remove `CanonicalDocument.SetSearchText(...)`
- Remove `CanonicalDocument.SetContent(...)`
- Remove `CanonicalDocument.AddKeywordToken(...)`
- Keep `CanonicalDocument.AddKeywordsFromTokens(...)` as a convenience API delegating through `AddKeyword(...)`
- Add `TokenNormalizer.NormalizeToken(string? token)`

### 7.2 Integration impact

Impacted areas expected from current evidence:

- ingestion rule action application
- file-share enrichment flows that add keywords
- `CanonicalDocument` unit tests
- `test/UKHO.Search.Tests` for `TokenNormalizer` unit coverage
- ingestion integration tests asserting canonical keyword behavior

## 8. Observability (logging/metrics/tracing)

No new logging, metrics, or tracing is required for the initial change.

If implementation risk emerges, temporary benchmark/test evidence is preferred over runtime logging because performance is a primary concern.

## 9. Security & compliance

No direct security or compliance requirement change is identified.

The change is limited to in-process normalization and domain document mutation semantics.

## 10. Testing strategy

### 10.1 `TokenNormalizer` unit tests

At minimum, cover:

- use a dedicated `TokenNormalizer` test class; do not mix this coverage into unrelated test classes
- place the dedicated test class in `test/UKHO.Search.Tests/Query/TokenNormalizerTests.cs`
- lowercase normalization of simple tokens
- trimming behavior for surrounding whitespace
- deterministic single output for non-special tokens
- `s-` plus digits outputs containing both hyphenated and compact forms
- explicit representative data-driven cases for `s-57`, `s-63`, `s-100`, and `s-101`
- compact `s100`-style input proving reverse expansion is not applied
- non-matching `s-` prefixed tokens proving aliasing is not applied when the suffix is not numeric
- duplicate suppression when a derived alias would equal the primary token
- case-insensitive handling of `S-100`-style input
- assertions that returned values are lowercase-only in all scenarios
- empty-sequence behavior for null/empty/whitespace input
- assertions should validate set membership for multi-output aliases rather than depend on a fixed returned order
- performance-sensitive representative test scenarios, without a mandatory dedicated benchmark suite
- place these tests in `test/UKHO.Search.Tests` so the shared domain implementation is validated independently of ingestion-specific test projects
- use namespace `UKHO.Search.Tests.Query`

### 10.2 `CanonicalDocument` tests

Update tests to reflect:

- `CanonicalDocument` lowercases stored string values internally
- additive semantics remain unchanged
- existing `Set...()`-focused tests are rewritten to assert the corresponding `Add...()` methods directly
- removed `AddKeywordToken(...)` is no longer referenced
- `AddKeywords(IEnumerable<string?>?)` remains a simple additive wrapper over `AddKeyword(...)`
- `AddKeywordsFromTokens(...)` remains supported and delegates through `AddKeyword(...)`
- `AddKeywordsFromTokens(...)` splits expected punctuation-delimited token input correctly
- `AddKeywordsFromTokens(...)` preserves hyphenated tokens such as `s-100` as single tokens
- `AddKeywordsFromTokens(...)` follows the agreed fixed explicit delimiter set
- `AddKeywordsFromTokens(...)` invokes `TokenNormalizer` so alias outputs such as `s100` are added when appropriate
- `AddKeywordsFromTokens(...)` ignores repeated delimiters and empty segments
- null, empty, and whitespace-only inputs are still ignored by `AddKeyword(...)`

### 10.3 Integration/regression tests

Update impacted tests so that:

- rule application paths still populate `SearchText` and `Content` correctly via `Add...()` APIs
- keyword-producing flows explicitly normalize before adding in all cases, not only for special `s-` tokens
- search-recall expectations for `s-100` style values are covered where appropriate

## 11. Rollout / migration

1. introduce the new spec-approved API shape
2. update all source call sites from `Set...()` to `Add...()`
3. add `TokenNormalizer` and adopt it at all keyword mutation entry points
4. update and expand tests
5. verify no remaining `Set...()` references exist
6. validate performance-sensitive behavior before merge

## 12. Open questions

No further open questions.
