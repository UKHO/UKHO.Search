# Work Package: `055-rule-exists-semantics` — Ingestion Rule `exists` Boolean Semantics

**Target output path:** `docs/055-rule-exists-semantics/spec-domain-rule-exists-semantics_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for completing the boolean semantics of the ingestion-rules `exists` operator.

Today the rule schema validator accepts `"exists": true` and `"exists": false`, but runtime evaluation only behaves as a positive existence check. As a result, authored rules that rely on `"exists": false` do not behave as intended.

This work will align rule-engine runtime behavior, tests, developer tooling, and documentation so that `exists` behaves consistently as a real boolean operator.

### 1.2 Scope

This specification covers:

- ingestion rule-engine runtime semantics for `exists`
- rule-evaluation behavior for both `exists: true` and `exists: false`
- updates to existing tests where current expectations depend on the incomplete implementation
- addition of thorough new tests for the new semantics
- explicit review of `RulesWorkbench` behavior and targeted updates if needed
- updates to relevant repository wiki pages under `./wiki`
- updates to current rule-authoring guidance in repository docs where `exists` semantics are described

This specification does not require a new rules schema version. The work remains within `schemaVersion` `1.0`.

### 1.3 Stakeholders

- ingestion developers
- rule authors
- RulesWorkbench users
- maintainers of repository documentation and wiki content
- test maintainers for ingestion and tooling projects

### 1.4 Definitions

- `exists` operator: a leaf predicate operator in the ingestion rules DSL used to test whether a path resolves to one or more retained values.
- retained value: a resolved value that is not `null`, empty, or whitespace-only after current runtime normalization rules are applied for existence checks.
- `exists: true`: a predicate requesting that at least one retained value is present.
- `exists: false`: a predicate requesting that no retained values are present.
- RulesWorkbench: the Blazor-based developer tool under `tools/RulesWorkbench` that uses the shared rules engine and checker flows for local rule diagnosis.

## 2. System context

### 2.1 Current state

The current implementation already validates `exists` as a boolean operator value during rule validation.

However, runtime evaluation currently behaves as follows:

- if a path resolves to no values, evaluation returns `false` before considering the boolean operator value
- `exists` therefore behaves only as a positive existence check
- `exists: false` is accepted by validation but is not honored by evaluation

The current public rule-authoring documentation describes only the positive `exists: true` case.

Repository evidence also shows that this is not an isolated edge case. Multiple rule files under `./rules/file-share` already use `"exists": false` as authored intent.

### 2.2 Proposed state

The system will be updated so that `exists` is treated as a real boolean operator with complete semantics:

- `exists: true` matches when the path resolves to one or more retained values
- `exists: false` matches when the path resolves to zero retained values
- missing paths and present-but-empty/whitespace-only values shall be treated as non-existent for `exists` evaluation
- validator, runtime behavior, tests, documentation, and RulesWorkbench shall all align with the same semantics

This shall make currently authored `exists: false` rules behave according to their stated meaning without requiring rule authors to rewrite them using `not` wrappers.

### 2.3 Assumptions

- the existing rule validator behavior that accepts boolean `exists` values is correct and should be preserved
- the intended semantics of `exists: false` are equivalent to `not { path: ..., exists: true }` when applied to the same path and retained-value rules
- a retained value for existence checks excludes `null`, empty-string, and whitespace-only results
- no rules schema version bump is required because the schema already permits boolean `exists`
- RulesWorkbench should continue using the shared runtime path wherever possible rather than implementing separate checker-only semantics

### 2.4 Constraints

- `schemaVersion` remains `1.0`
- fail-fast validation behavior for malformed JSON, unsupported operators, and invalid paths shall remain unchanged
- this work must not change the broader rule-evaluation model beyond the `exists` operator semantics and any directly required test/tooling/documentation alignment
- relevant wiki pages under `./wiki` must be updated as part of Definition of Done

## 3. Component / service design (high level)

### 3.1 Components

This work affects the following areas:

1. Rules engine evaluation
   - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/IngestionRulesOperatorEvaluator.cs`
   - ensure `exists` honors both boolean values

2. Predicate evaluation behavior
   - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/IngestionRulesPredicateEvaluator.cs`
   - confirm no surrounding runtime behavior prevents `exists: false` from matching as intended

3. Rule validation and authoring guidance
   - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Validation/IngestionRulesValidator.cs`
   - `docs/ingestion-rules.md`
   - keep validator semantics and documentation aligned with runtime behavior

4. Repository rule assets
   - `./rules/file-share/*.json`
   - existing rules that already use `exists: false` shall be re-verified under the corrected runtime semantics

5. RulesWorkbench
   - `tools/RulesWorkbench/...`
   - examine evaluate/checker/reporting flows to confirm they remain aligned because they use the shared rules engine; modify any page text, warnings, or tests if current behavior or wording becomes misleading

6. Tests
   - ingestion unit tests for operators and predicates
   - rules-engine integration/regression tests
   - RulesWorkbench tests where behavior or documentation assumptions depend on `exists` semantics

7. Wiki
   - relevant pages under `./wiki`
   - update rule-authoring and tooling guidance to reflect the completed boolean semantics

### 3.2 Data flows

Updated rule-evaluation flow for `exists`:

1. a rule leaf predicate resolves a path against the current payload
2. resolved values are filtered using the existing retained-value concept for existence checks
3. if the operator is `exists` and the authored boolean is `true`, the predicate matches when at least one retained value remains
4. if the operator is `exists` and the authored boolean is `false`, the predicate matches when no retained values remain
5. matched values for `exists: true` continue to include retained resolved values
6. matched values for `exists: false` remain empty because the condition is absence-based rather than value-producing
7. any tool or test harness using the shared engine reflects the same result

### 3.3 Key decisions

- `exists` shall be treated as a true boolean operator, not a positive-only shorthand
- `exists: false` shall mean absence of retained values, not merely absence of a successfully resolved path object
- present-but-empty and present-but-whitespace-only values shall count as not existing for this operator, to remain consistent with current `exists: true` behavior
- `exists: false` shall be semantically equivalent to wrapping the same leaf in `not { ... exists: true }`, but the direct authored form shall be supported explicitly
- matched values for `exists: false` shall be empty rather than synthetic placeholder values
- RulesWorkbench shall not introduce its own special-case implementation; it shall remain aligned with the shared rule-engine semantics

## 4. Functional requirements

### FR-001 Boolean `exists` semantics

The ingestion rules engine shall support both `exists: true` and `exists: false` as valid runtime behaviors.

### FR-002 `exists: true` behavior

A leaf predicate using `exists: true` shall match when the referenced path resolves to at least one retained value.

A retained value for `exists` evaluation shall be a resolved value that is not `null`, empty, or whitespace-only.

### FR-003 `exists: false` behavior

A leaf predicate using `exists: false` shall match when the referenced path resolves to zero retained values.

This shall include cases where:

- the path does not resolve at runtime
- the path resolves only to `null`
- the path resolves only to empty strings
- the path resolves only to whitespace-only strings

### FR-004 Equivalence with explicit `not`

For the same payload and path, `exists: false` shall produce the same match outcome as:

```json
{
  "not": {
    "path": "<same path>",
    "exists": true
  }
}
```

This equivalence shall be treated as a core semantic compatibility requirement.

### FR-005 Matched values for `exists`

When `exists: true` matches, matched values shall continue to include the retained resolved values.

When `exists: false` matches, matched values shall be empty.

The engine shall not invent placeholder values for absence-based matches.

### FR-006 Validation/runtime consistency

The runtime behavior of `exists` shall be consistent with the validator contract that already accepts boolean `exists` values.

The system shall not accept `exists: false` during validation while silently ignoring the `false` value during runtime evaluation.

### FR-007 Existing authored rules

Existing repository rules that currently use `exists: false` shall be re-evaluated under the corrected runtime semantics.

Any repository rule whose intended behavior becomes clearer or whose overlap becomes exposed under the corrected semantics shall be updated if needed as part of this work.

### FR-008 No schema-version uplift

The completed `exists` semantics shall remain within rule `schemaVersion` `1.0`.

No schema migration or alternate compatibility mode shall be introduced solely for this feature.

### FR-009 Existing test updates

Existing tests shall be reviewed and updated where their assumptions currently reflect the incomplete positive-only `exists` implementation.

This explicitly includes any test suites that exercise:

- operator evaluation
- predicate evaluation
- rules-engine integration
- repository rule behavior
- RulesWorkbench rule-checker or evaluation flows

### FR-010 New unit tests for operator semantics

New tests shall be added to cover `exists` operator behavior thoroughly, including at minimum:

- `exists: true` matches when a non-empty value is present
- `exists: true` does not match when the path is missing
- `exists: true` does not match when only empty/whitespace values are present
- `exists: false` matches when the path is missing
- `exists: false` matches when only empty/whitespace values are present
- `exists: false` does not match when a retained value is present
- matched-values behavior for both boolean forms

### FR-011 New predicate-level tests

New tests shall be added to prove `exists: false` works correctly inside the full predicate model, including at minimum:

- leaf predicates
- `all` combinations
- `any` combinations
- `not` combinations
- predicates over normal properties and wildcard paths where applicable

### FR-012 New integration/regression tests

New or updated integration tests shall prove that rules authored with `exists: false` can now match and mutate `CanonicalDocument` as intended.

This shall include at least one representative regression for repository-like rule shapes that previously failed to match.

### FR-013 Repository rule coverage review

The repository rule set under `./rules/file-share` shall be reviewed specifically for existing `exists: false` usage.

The implementation work shall verify that these rules continue to represent intended business behavior once the runtime semantics are corrected.

### FR-014 RulesWorkbench review

`RulesWorkbench` shall be thoroughly examined to determine whether any of the following require change:

- evaluation flows
- checker/reporting logic
- page text or warnings that describe rule behavior
- test coverage
- documentation in the tool-facing wiki pages

If no code changes are required in a given RulesWorkbench area, the implementation work shall still add or update tests where useful to prove runtime alignment.

### FR-015 Wiki and docs update

Relevant wiki pages under `./wiki` and the current practical rule-authoring guide under `docs/ingestion-rules.md` shall be updated to describe the completed boolean `exists` semantics.

The updated documentation shall explicitly state the meaning of both:

- `exists: true`
- `exists: false`

## 5. Non-functional requirements

### NFR-001 Deterministic behavior

The completed `exists` semantics shall remain deterministic for the same payload, path, and rule set.

### NFR-002 Minimal scope of change

The implementation shall be limited to the operator semantics and directly affected tests, tooling alignment, rules review, and documentation.

It shall not introduce unrelated changes to path parsing, templating, or non-`exists` operators.

### NFR-003 Backward compatibility for valid intent

Existing rules authored with `exists: true` shall continue to behave as they do today.

The behavioral change introduced by this work shall be the activation of the already-authored `exists: false` intent.

### NFR-004 Observable and diagnosable behavior

Where current logging, checker output, or test diagnostics depend on rule-match outcomes, the updated semantics shall remain observable and diagnosable without reducing current troubleshooting quality.

### NFR-005 Documentation completeness

The work shall be considered incomplete unless code, tests, and relevant documentation are all updated together.

## 6. Data model

No new persisted domain data model is required.

The relevant logical model change is semantic rather than structural:

- `exists` remains a boolean-valued operator in rule JSON
- the runtime meaning of `false` is completed so that it becomes first-class rather than ignored

Representative rule shape:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "example-rule",
    "context": "adds-s100",
    "title": "Example title",
    "if": {
      "all": [
        {
          "path": "properties[\"product code\"]",
          "exists": false
        }
      ]
    },
    "then": {
      "category": {
        "add": [
          "data product"
        ]
      }
    }
  }
}
```

## 7. Interfaces & integration

### 7.1 Rule JSON contract

The external rule JSON contract remains:

- operator name: `exists`
- value type: boolean

No new operator name or schema field is introduced.

### 7.2 Rules engine integration

The rules engine shall expose the corrected semantics automatically through the existing evaluation path.

Consumers such as ingestion runtime and RulesWorkbench shall receive the new behavior by continuing to use the shared engine.

### 7.3 Tooling integration

RulesWorkbench shall be verified against the corrected semantics in both:

- direct evaluation flows
- checker flows that depend on matched-rule outcomes

## 8. Observability (logging/metrics/tracing)

This work does not require a new metrics surface.

However:

- existing logs and diagnostics that report matched rules shall remain accurate under the corrected semantics
- any changed test diagnostics should make it easy to distinguish positive-existence and negative-existence cases
- if repository rule behavior changes materially for checker outcomes, the resulting RulesWorkbench output should remain understandable to rule authors

## 9. Security & compliance

This work introduces no new security boundary, authentication behavior, or compliance-sensitive storage.

The main governance concern is correctness of classification behavior:

- a rule that should match on missing data must not be silently skipped
- documentation must accurately describe the rule semantics relied on by rule authors

## 10. Testing strategy

Testing for this work shall be thorough and explicit.

### 10.1 Unit tests

Add or update focused tests for:

- `IngestionRulesOperatorEvaluator`
- `IngestionRulesPredicateEvaluator`
- any helper logic directly involved in retained-value handling for `exists`

### 10.2 Integration tests

Add or update integration/regression tests for:

- rules-engine behavior using representative repository-style rules with `exists: false`
- end-to-end rule application into `CanonicalDocument`
- any currently failing or misleading S-100-style rule pattern that motivated this work

### 10.3 Repository rule review tests

Where appropriate, update or add tests that validate checked-in repository rules or their known matching behavior under the corrected semantics.

### 10.4 RulesWorkbench tests

Review `RulesWorkbench` tests and add/update coverage where needed so the tool remains aligned with shared rule-engine behavior.

This shall include checker-oriented tests if match outcomes or user-facing warnings/statuses change as a result of the corrected semantics.

### 10.5 Documentation verification

As part of completion, verify that:

- `docs/ingestion-rules.md` describes both boolean forms
- relevant `./wiki` pages are updated consistently
- RulesWorkbench wiki guidance remains aligned with actual behavior

## 11. Rollout / migration

No schema migration is required.

Implementation rollout should follow this order:

1. update runtime `exists` evaluation semantics
2. add/adjust unit tests for operator and predicate behavior
3. add/adjust integration/regression tests
4. review repository rules that currently use `exists: false`
5. examine RulesWorkbench and modify if needed
6. update docs and wiki

Because this is a behavioral correction with existing authored usage in the repository, rollout should assume that some previously non-matching rules will begin matching.

That outcome is expected and shall be treated as part of the intended correction rather than as an accidental side effect, provided the updated tests and rule reviews confirm the authored rule intent.

## 12. Open questions

1. Are there any repository rules currently using `exists: false` where the authored intent was actually compensating for the incomplete runtime semantics rather than expressing true missing-value logic?
2. Should repository docs include a short authoring note that `exists: false` is preferred over `not { exists: true }` when the absence check is the primary intent, for readability?
3. Do any RulesWorkbench pages need explicit UX wording changes to help users understand negative existence matches, or is shared-engine alignment and test coverage sufficient?
