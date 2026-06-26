# Work Package 046 — RulesWorkbench Checker

## 1. Overview

### 1.1 Purpose
Add a new `Checker` page to `RulesWorkbench` that runs rule checking against the local database already used by the `Evaluate` page.

The page is intended to help a human quickly answer:
- did the batch enrich as expected?
- which rules actually ran?
- which rules probably should have run?
- what data or rule JSON explains the failure?

### 1.2 Primary goals
The `Checker` page should:
- run rules using the same runtime behavior used by `IngestionServiceHost`
- load real batch data from the local database
- determine whether the result is acceptable using an initial success heuristic
- surface enough diagnostics for a human to spot the likely problem
- support fast iterative investigation without requiring a full ingestion rerun

### 1.3 Initial success heuristic
For the first iteration, a checker run can be considered `OK` if the final `CanonicalDocument` has at least:
- `Category`
- `Series`
- `Instance`

This heuristic should be treated as the initial policy, not the final extensibility model.

## 2. Problem statement

The current `Evaluate` page is useful for manually loading one payload and applying rules, but it is not yet optimized for bulk diagnosis across real database data.

We need an operator-focused page that can:
- inspect one batch or many batches
- identify failures quickly
- show enough context to debug the rule JSON
- handle a very large dataset (200,000+ batches) without trying to synchronously brute-force the entire database in the UI

## 3. User stories

### 3.1 Single-batch diagnosis
As a rule author, I want to enter a `batchId`, run the checker, and immediately see:
- whether the batch passed the checker
- the final enriched document
- which rules matched
- which candidate rules were relevant
- the rule JSON and payload data side by side

### 3.2 Filtered batch scan
As a rule author, I want to run the checker over a filtered subset of batches so I can find failures by:
- business unit
- date range
- explicit batch ids
- optional top/max row count

### 3.3 Rapid repair loop
As a rule author, I want to edit a rule draft, rerun it against the current failing batch, and continue investigating without reloading the full app or losing context.

### 3.4 Failure triage at scale
As an operator, I want to identify the first failing batches and the most common failure patterns without processing all 200,000+ batches in a single foreground UI action.

## 4. Functional requirements

### 4.1 Page and navigation
- `RulesWorkbench` shall add a new page named `Checker`.
- The page shall be reachable from the main navigation.
- The page shall clearly distinguish between:
  - single-batch checking
  - filtered multi-batch checking

### 4.2 Data source
- The page shall use the same local database connection already used by the `Evaluate` page.
- The page shall reuse the same database lookup/reference approach already established for loading batch payloads.
- The checker shall load at minimum:
  - batch id
  - batch business unit identifier and/or name
  - created timestamp
  - attributes
  - files
  - security token inputs needed for evaluation parity

### 4.3 Execution parity with ingestion
- The checker shall run the ingestion rules using the same runtime path used by `IngestionServiceHost`, not a simplified or separate rule interpreter.
- The checker shall reuse the shared ingestion rule engine and document mutation behavior already used by the ingestion runtime.
- For v1, the checker shall scope execution parity to the ingestion rules path only.
- For v1, the checker shall not require execution of the wider file-share enrichment chain.
- The UI shall make this scope explicit so users understand that rule checking is being performed without ZIP-dependent or other broader enrichment steps.
- This scoped approach is required because a large proportion of local test batches do not have ZIPs available.
- The wider enrichment chain may be added as a later enhancement.

### 4.4 Check result
For each checked batch, the system shall produce:
- a final result status: `OK`, `Warning`, or `Fail`
- the final `CanonicalDocument`
- a summary of key fields used for the pass/fail decision
- any execution errors or warnings

For v1, a batch shall be considered `OK` when:
- `Category` contains at least one value
- `Series` contains at least one value
- `Instance` contains at least one value

This v1 success heuristic is intentionally provisional.
- It should remain configurable in a later iteration.
- It should not be treated as proof that every rule set is expected to populate all three fields.
- The checker UI should present these fields as the current validation heuristic, not as a guaranteed contract of every rule.

For v1, a batch shall be considered `Fail` when one or more of those required fields is empty.

`Warning` may be used for partial or ambiguous outcomes, for example:
- rules matched but required fields still missing
- no rules matched, but the candidate rule subset could not be determined reliably
- business-unit-to-rule mapping heuristic is inconclusive

### 4.5 Candidate rule subset
The checker shall help diagnose which rules should have been relevant.

Initial approach:
- infer the business unit name from the rule id naming convention `bu-{businessunitname}-*`
- treat the rule id naming convention as lowercase
- join the `BusinessUnit` table when loading batch data and obtain the batch business unit name
- lowercase the batch business unit name before comparison
- compute the subset of rules whose inferred business unit name matches the batch business unit name

The UI shall display:
- all matched rules
- all candidate rules inferred for the batch business unit
- candidate rules that did not match
- the candidate rule list as the primary diagnostic view, without requiring automatic offender selection

The checker should also provide an optional `most likely offender` hint when it can do so using an explainable heuristic.

If such a hint is shown, the UI shall:
- keep the full candidate list visible
- clearly label the hint as a best-effort suggestion
- explain why that rule was highlighted, if a reason can be derived

The checker shall not depend on automatic offender identification in v1.

Because this is heuristic, the UI shall also state when:
- a rule id could not be parsed
- a batch business unit value is missing
- inferred candidate selection may be incomplete or unreliable

### 4.6 Diagnostics for a human reviewer
The checker page shall display enough information for a human to read the rule JSON and spot likely problems.

For a selected batch result, the UI shall show:
- batch summary (`batchId`, timestamp, business unit)
- a readable structured payload summary used for evaluation
- an option to expand and inspect the raw payload JSON
- final `CanonicalDocument` JSON
- matched rule ids in execution order
- candidate-but-unmatched rule ids
- the raw JSON for a selected rule
- v1 does not need a separate parsed rule summary above the JSON
- pass/fail explanation showing which required fields are missing

The page should also show helpful supporting signals such as:
- relevant properties and file MIME types
- security tokens used for evaluation
- counts of values added to key document fields if available
- runtime warnings returned by the evaluation flow

### 4.7 Bulk checking
Because the database contains more than 200,000 batches, the checker shall not require a full-table foreground scan as the default interaction.

The page shall support bounded execution by allowing one or more of:
- explicit max row count
- business unit filter
- explicit batch id list
- stop-after-first-failure mode
- continue-until-N-failures mode

For v1:
- business unit filtering shall be supported
- the user shall explicitly choose a single business unit to scan
- v1 shall not support scanning all business units in one run
- the business unit selector shall list all business units from the `BusinessUnit` table
- the selector shall not be restricted to active business units only
- each selector option shall display both the business unit name and the business unit id
- the business unit id shall be shown to avoid ambiguity where names are similar or repeated
- the selected business unit id shall be used as the database filter for batch selection
- the selected business unit name, normalized to lowercase, shall be used for candidate rule subset matching
- date range filtering is not required
- business unit filtering is intended to help users fix rules in batches for the same area of ownership
- batch ordering does not need to be user-configurable in v1
- batches shall be processed in a deterministic order so scan/resume behavior is stable
- the default processing order shall be `CreatedOn` ascending, then `BatchId` ascending as a tie-breaker

Bulk runs shall produce a summary including:
- number of batches checked
- number passed
- number failed
- number warned
- top failure reasons
- common missing fields
- business unit breakdown

For v1, the default and primary scan behavior shall be `stop at first failure`.

When the first failing batch is found, the checker shall:
- stop processing further batches
- keep the failing batch loaded in the UI
- present the matched rules, candidate rules, payload, and selected rule JSON needed for diagnosis
- allow the user to edit the relevant rule JSON
- focus the UI on the failing batch only
- not retain or display a running history of previously passed batches in v1

After the user edits a rule, the checker shall:
- validate the edited rule JSON before it is accepted
- rerun the current failing batch against the edited in-memory rule set
- only allow continuation to the next batch when the edited rule JSON is valid and the current batch passes the checker
- resume scanning from the next batch in the current filtered sequence after the current batch passes

If the edited rule JSON is invalid, the checker shall:
- refuse to continue the scan
- display validation errors clearly
- preserve the current failing batch context for further correction

### 4.8 Iterative edit-and-rerun
The checker should support rule editing for diagnosis.

Initial diagnostic editing behavior should be:
- allow editing a selected rule in memory
- rerun the current batch using the edited rule set
- compare before/after outcome
- reload the validated edited rule into the current in-memory rule set before continuing the scan

Optional follow-on behavior:
- if the edited rule produces an acceptable result, replace the currently loaded in-memory rule with the edited version for the rest of the current checker session
- continue scanning subsequent batches using the updated in-memory rule set from the next item onward

Out of scope for the first slice unless explicitly approved:
- automatically persisting checker edits back to Azure App Configuration
- automatically changing production rules without human approval

### 4.9 Automation opportunities
The checker may include limited automation to reduce manual effort, but automation must remain explainable.

Potential automation for consideration:
- auto-suggest likely candidate rules from business unit
- auto-highlight one most likely offending rule when an explainable heuristic is available
- auto-highlight missing predicate inputs referenced by selected rule JSON
- auto-highlight payload paths referenced by the rule but absent from the batch
- auto-rank failures by most likely cause category, such as:
  - no candidate rule subset
  - candidate rules exist but none matched
  - rules matched but required fields still missing
  - path/value mismatch likely

The system shall not attempt opaque automatic rule rewrites in v1.

## 5. Non-functional requirements

### 5.1 Performance and scale
- Single-batch checks should feel interactive.
- Multi-batch runs shall be bounded and cancellable.
- The page shall avoid trying to render extremely large result sets all at once.
- Long-running scans should use paging, batching, or background processing semantics appropriate for the workbench host.

### 5.2 Transparency
- Results must be explainable to a human reviewer.
- Heuristic decisions must be labeled as heuristic.
- The UI must distinguish facts from guesses.

### 5.3 Safety
- Checker edits should default to session-only changes.
- Bulk runs must not alter source database records.
- Any future persistence action must be explicit and user-initiated.

## 6. Suggested v1 scope

Recommended first slice:
1. single-batch checker using real DB payload
2. same shared ingestion rules execution path as ingestion/workbench engine
3. pass/fail based on `Category`, `Series`, `Instance`
4. candidate rule subset inferred from business unit
5. diagnostics view with payload, selected rule JSON, matched rules, candidate rules, final document
6. filtered multi-batch scan with hard result limits and summary counts
7. session-only edit-and-rerun for the selected batch

Recommended later slices:
- background scan jobs
- resumable scans
- compare result sets across rule revisions
- save approved rule fixes back to App Configuration
- richer predicate/path tracing

## 7. Acceptance criteria
- A user can enter a `batchId` and run a checker evaluation against local database data.
- The checker uses the same shared rule execution behavior as the ingestion runtime path it claims to emulate.
- The result clearly states whether `Category`, `Series`, and `Instance` were populated.
- The result view shows matched rules and candidate-but-unmatched rules.
- A human can inspect selected rule JSON and enough batch data to diagnose likely mismatches.
- A user can run bounded filtered scans without attempting a full unbounded 200,000+ batch pass in the browser.
- In v1, scans stop at the first failing batch.
- A user can edit a rule draft in memory, rerun the current batch, and continue only after the edited rule JSON validates and the batch passes.

## 8. Open decisions

### 8.1 Execution parity scope
Decision recorded:
- `Checker` v1 will execute only the ingestion rules path.
- It will not execute the broader file-share enrichment chain in the first iteration.

Rationale:
- the current requirement is specifically about checking rules
- many locally available test batches do not have ZIPs
- requiring ZIP-dependent enrichment would reduce usefulness of the checker against local data

Future direction:
- broader enrichment-chain execution may be added later as an optional mode

### 8.2 Business unit mapping reliability
Decision recorded:
- candidate-rule selection in v1 shall use the rule id naming convention `bu-{businessunitname}-*`
- matching shall be performed using the lowercase business unit name
- the checker shall obtain the business unit name by joining the `BusinessUnit` table for the batch being checked

Future direction:
- explicit rule metadata may still be preferable later if the naming convention stops being reliable

### 8.3 Session override behavior
Decision recorded:
- when a scan stops on a failing batch, the user may edit the relevant rule JSON in memory
- the checker must validate the edited rule JSON before accepting it
- if the edited rule is valid and the current batch passes, the checker shall reload that edited rule into the current in-memory rule set
- subsequent scanning shall continue from the next item using the updated in-memory rule set for the remainder of the session

### 8.4 Scan resume behavior
Decision recorded:
- after a successful fix and rerun, the checker shall resume from the next batch in the current filtered sequence
- it shall not restart the entire run by default

Future direction:
- a manual `restart from beginning` action may be added later if needed

## 9. Recommendation

Recommended product direction:
- start with a highly explainable diagnostic tool, not a full automation engine
- optimize for finding the first useful failure quickly
- support bounded scans and session-only overrides
- add automation only where it reduces obvious manual effort and remains easy to understand
