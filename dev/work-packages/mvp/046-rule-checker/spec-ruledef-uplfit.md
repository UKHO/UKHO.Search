# Specification — Rule Definition Uplift

**Target output path:** `dev/work-packages/mvp/046-rule-checker/spec-ruledef-uplfit.md`

## 1. Overview

### 1.1 Purpose
Introduce an explicit `Context` field into the ingestion rule definition model and use it as the authoritative business-unit discriminator for `file-share` rules.

This change removes the current ambiguity in candidate-rule selection caused by inferring business unit names from rule file names such as `bu-{businessunitname}-x-*`, where:
- `{businessunitname}` may itself contain one or more hyphens, and
- `x` is an integer segment within the file naming convention.

### 1.2 Problem statement
The existing RulesWorkbench `Checker` candidate-rule selection logic assumes that the business unit can be safely parsed from the rule file name prefix. This is not reliable.

Example ambiguity:
- `bu-adds-s100-4-base-exchange-set-product-type`

This can be interpreted incorrectly as:
- business unit = `adds`

when the intended business unit is actually:
- business unit = `adds-s100`

This means the file name alone is not a reliable source of truth for business unit selection.

### 1.3 Goal
Make rule context explicit in the rule definition so that:
- ingestion rule loading and validation understand the new field,
- rule JSON documents under `./Rules/file-share` include the field,
- RulesWorkbench `Checker` uses the field for candidate-rule matching,
- existing tests are uplifted to cover the new behavior.

## 2. Scope

### 2.1 In scope
1. Introduce a new `Context` field into the rule definition model.
2. For rules under `./Rules/file-share`, define `Context` as the business unit name.
3. Update existing `file-share` rule JSON files to include `Context`.
4. Update ingestion rule code to load, validate, and expose the new field as needed.
5. Update existing tests affected by the rule definition change.
6. Update RulesWorkbench `Checker` candidate-rule selection to use `Context` instead of inferring the business unit from the file name.

### 2.2 Out of scope
1. Changing the wider rule execution semantics beyond introducing and carrying the `Context` field.
2. Changing the full file naming convention as part of this work.
3. Persisting checker edits back to Azure App Configuration.
4. Changing broader enrichment-chain behavior.

## 3. Functional requirements

### 3.1 Rule definition model
- A rule definition SHALL support a new string field named `Context`.
- The field SHALL be treated as optional at the model-binding/deserialization layer only if needed for compatibility during uplift, but SHALL be required for `file-share` rule definitions after migration is complete.
- For `file-share` rules, `Context` SHALL represent the business unit name.
- `Context` matching for `file-share` candidate-rule selection SHALL be case-insensitive by normalizing both sides to lowercase.

### 3.2 File-share rule JSON content
- All existing rule JSON files under `./Rules/file-share` SHALL be updated to include `Context`.
- The `Context` value SHALL be set to the intended business unit name for that rule.
- For this uplift, the business unit MAY be inferred from the existing file name pattern `bu-{bu name}-x-*`, where:
  - `x` is an integer,
  - `{bu name}` may itself contain a hyphen.
- Because `{bu name}` may contain hyphens, the migration logic/process SHALL identify the business unit by locating the integer segment and treating the preceding portion after `bu-` and before `-{integer}-` as the business unit name.

Examples:
- `bu-adds-4-something.json` => `Context = "adds"`
- `bu-adds-s100-4-base-exchange-set-product-type.json` => `Context = "adds-s100"`
- `bu-avcs-bespokeexchangesets-12-foo.json` => `Context = "avcs-bespokeexchangesets"`

### 3.3 Ingestion rule loading and validation
- The ingestion rule code SHALL be updated to support the new `Context` field.
- Validation SHALL ensure that `Context` is available for `file-share` rules once the rule set has been uplifted.
- Any rule-definition DTOs, validated rule types, and related mapping code SHALL be updated consistently.
- Existing behavior for rule predicates and actions SHALL remain unchanged unless required to carry the new field through the pipeline.

### 3.4 RulesWorkbench Checker candidate selection
- RulesWorkbench `Checker` SHALL stop using rule file names as the source of truth for business unit candidate matching.
- Candidate-rule selection SHALL use the rule definition `Context` value.
- For checker scans:
  - the selected business unit id SHALL still be used to select batches from the database,
  - the selected business unit name lowercased SHALL be compared against the rule `Context` lowercased.
- Candidate-rule selection SHALL therefore become independent of ambiguous file name parsing.

### 3.5 Backward compatibility
- The implementation MAY temporarily support rules that do not yet contain `Context` only if required to enable an orderly uplift.
- If temporary compatibility support is added, it SHALL be clearly treated as transitional behavior, not the target design.
- The target steady state is that `file-share` rules explicitly define `Context` and RulesWorkbench relies on that field.

## 4. Technical requirements

### 4.1 Rule model changes
The following code areas are expected to require uplift:
- rule DTO/model types under `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/*`
- validated rule types under `src/UKHO.Search.Infrastructure.Ingestion/Rules/Validation/*`
- any rule loading / mapping / validation code that currently projects rule metadata
- any reporting path that returns rule details to RulesWorkbench

### 4.2 RulesWorkbench changes
The following code areas are expected to require uplift:
- `tools/RulesWorkbench/Services/RuleCheckerService.cs`
- any rule snapshot store or rule entry type used to expose rule JSON / metadata to the checker
- any candidate-rule parsing helper that currently depends on filename conventions

### 4.3 Rule file migration
- Existing files under `./Rules/file-share` SHALL be edited in place to add `Context`.
- The migration SHALL preserve existing rule ids, descriptions, predicates, and actions.
- The migration SHALL not rely on the currently ambiguous “split on hyphen” approach.
- The migration process SHALL identify the integer segment after the business unit token and use that as the delimiter point when deriving the business unit from the legacy filename.

## 5. Data and naming rules

### 5.1 Authoritative business unit source
After this uplift:
- the rule file name SHALL be treated as a human-readable artifact,
- the rule `Context` field SHALL be the authoritative business-unit indicator for `file-share` rules.

### 5.2 Normalization
- `Context` values for `file-share` rules SHOULD be stored in lowercase for consistency.
- If legacy casing is retained in some files, comparison logic SHALL normalize to lowercase before matching.

### 5.3 Filename convention after uplift
- Existing filenames MAY remain unchanged in this work package.
- The business unit SHALL no longer be inferred from the filename during checker candidate selection.
- A later naming-convention cleanup MAY still be desirable, but it is not required once `Context` is authoritative.

## 6. Testing requirements

### 6.1 Ingestion rule tests
Existing tests SHALL be uplifted as necessary to cover:
- deserialization of rule JSON containing `Context`
- validation behavior for `file-share` rules with and without `Context`
- propagation of `Context` through any validated rule representation if applicable

### 6.2 RulesWorkbench tests
RulesWorkbench tests SHALL be added or updated to prove that:
- candidate-rule selection uses `Context`, not filename parsing
- a rule with filename `bu-adds-s100-4-*` and `Context = "adds-s100"` does not match business unit `adds`
- a rule with filename `bu-adds-s100-4-*` and `Context = "adds-s100"` does match business unit `adds-s100`

### 6.3 Regression focus
Regression coverage SHALL explicitly include the ambiguity case that motivated this uplift:
- business unit names that are prefixes of other hyphenated business unit names
- example: `adds` vs `adds-s100`

## 7. Acceptance criteria

1. A new `Context` field exists in the rule definition model.
2. Existing `file-share` rule JSON files include `Context`.
3. The uplift correctly identifies `adds-s100` from filenames like `bu-adds-s100-4-*` during migration.
4. Ingestion rule loading/validation supports the new field without changing existing predicate/action behavior.
5. RulesWorkbench `Checker` candidate-rule selection uses `Context` as the source of truth.
6. Candidate selection no longer incorrectly includes `adds-s100` rules when `adds` is selected.
7. Existing and new tests pass after the uplift.

## 8. Risks and considerations

### 8.1 Legacy filename ambiguity
The current file naming convention is ambiguous when business unit names contain hyphens. This uplift resolves the operational problem by removing filename parsing from candidate-rule selection, but the historical ambiguity still makes migration sensitive.

### 8.2 Migration accuracy
The one-time addition of `Context` to existing JSON files must be reviewed carefully, especially for business units whose names contain hyphens or prefixes of other business units.

### 8.3 Future simplification
Once `Context` is present and authoritative, future tooling should avoid any dependency on file name parsing for business unit logic.

## 9. Recommendation

Recommended direction:
- introduce `Context` now as the authoritative business-unit field,
- migrate existing `file-share` rules to include it,
- update RulesWorkbench `Checker` to use it exclusively for candidate selection,
- treat filename parsing only as a one-off migration aid for existing files, not as an ongoing runtime rule-selection mechanism.
