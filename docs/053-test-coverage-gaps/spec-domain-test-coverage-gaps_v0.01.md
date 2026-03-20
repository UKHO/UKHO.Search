# Specification: Repository Test Coverage Gap Analysis

Version: `v0.01`  
Status: `Draft`  
Date: `2026-03-20`  
Work Package: `docs/053-test-coverage-gaps/`  
Based on: `docs/026-s57-parser/spec-template_v1.1.md`  
Source Inputs:
- `./.github/prompts/test.coverage.prompt.md`
- `./.github/copilot-instructions.md`
- `./.github/instructions/documentation.instructions.md`
- `./.github/instructions/testing.instructions.md`
- `./.github/prompts/spec.research.prompt.md`
- `./artifacts/test-coverage/coverage-summary.json`
- raw Coverlet outputs under `./artifacts/test-coverage/<project-name>/`
- discovered test and source project inventories gathered from the current workspace

## 1. Objective

### 1.1 Purpose
This assessment establishes the current repository test coverage baseline using Coverlet, identifies the most important missing or weakly covered behaviors, and defines a focused specification for a later test-implementation pass.

### 1.2 Meaning of a coverage gap in this repository
For this repository, a coverage gap is any of the following:

- a discovered test project that does not execute trustworthy tests
- a subsystem with materially low line, branch, or method coverage
- an important class, service, pipeline node, or user flow that remains unexecuted
- missing negative-path, validation, routing, or edge-case tests in areas that already have nominal-path coverage
- tooling or project-layout issues that prevent a test project from representing its intended subsystem

## 2. Methodology

### 2.1 Discovery approach
Test projects were discovered from both sources required by the prompt:

- loaded solution projects
- recursive inspection of `./test/` for additional `*.csproj` files

In-scope test projects:

1. `test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`
2. `test/FileShareEmulator.Tests/FileShareEmulator.Tests.csproj`
3. `test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj`
4. `test/UKHO.Aspire.Configuration.Seeder.Tests/UKHO.Aspire.Configuration.Seeder.Tests.csproj`
5. `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
6. `test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj`
7. `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### 2.2 Coverlet execution
All discovered test projects were Coverlet-compatible at execution time and were run individually with MSBuild coverage enabled.

Command shape used:

- `dotnet test <test-project> --no-restore /p:CollectCoverage=true /p:CoverletOutput=<project-artifact-path>/coverage. /p:CoverletOutputFormat=json,cobertura`

Because Coverlet resolved the relative output path from each test project directory, the resulting raw outputs were normalized into the required repository-level artifact structure under `./artifacts/test-coverage/` after execution.

### 2.3 Artifact locations
Raw and consolidated artifacts produced by this assessment:

- `./artifacts/test-coverage/FileShareEmulator.Common.Tests/`
- `./artifacts/test-coverage/FileShareEmulator.Tests/`
- `./artifacts/test-coverage/RulesWorkbench.Tests/`
- `./artifacts/test-coverage/UKHO.Aspire.Configuration.Seeder.Tests/`
- `./artifacts/test-coverage/UKHO.Search.Ingestion.Tests/`
- `./artifacts/test-coverage/UKHO.Search.Query.Tests/`
- `./artifacts/test-coverage/UKHO.Search.Tests/`
- `./artifacts/test-coverage/coverage-summary.json`

### 2.4 Limitations and interpretation notes
- `FileShareEmulator.Tests` failed during test execution, so no trustworthy coverage artifact was available for that project.
- `UKHO.Search.Query.Tests` executed successfully but discovered zero tests.
- Coverlet outputs include generated `obj/` files; those were preserved in raw artifacts but were not prioritized when defining test gaps.
- Several test projects transitively cover assemblies outside their primary subsystem. Coverage percentages in this document are therefore interpreted as repository evidence, not as a strict ownership map.

## 3. Coverage baseline

### 3.1 Repository-wide baseline
Overall repository-wide union coverage from the successful Coverlet runs:

- Line coverage: `45.27%` (`6555/14479`)
- Branch coverage: `39.65%` (`1859/4689`)
- Method coverage: `54.65%` (`870/1592`)

### 3.2 Per-project baseline

| Test project | Outcome | Line % | Branch % | Method % | Notes |
| --- | --- | ---: | ---: | ---: | --- |
| `test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj` | `PassedWithCoverage` | 4.17 | 3.09 | 6.34 | Strong coverage of `FileShareEmulator.Common`, but very low incidental coverage of referenced search/ingestion assemblies |
| `test/FileShareEmulator.Tests/FileShareEmulator.Tests.csproj` | `FailedDuringTestExecution` | n/a | n/a | n/a | `IndexServiceTests.IndexBusinessUnitBatchesAsync_WhenProgressIsRequested_ReportsInitialAndPeriodicProgress` expected 3 progress updates and observed 2 |
| `test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj` | `PassedWithCoverage` | 8.75 | 6.75 | 16.03 | Service tests exist, but large UI and infrastructure regions remain unexecuted |
| `test/UKHO.Aspire.Configuration.Seeder.Tests/UKHO.Aspire.Configuration.Seeder.Tests.csproj` | `PassedWithCoverage` | 3.54 | 2.61 | 2.74 | Only a small portion of the seeder codebase is covered |
| `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj` | `PassedWithCoverage` | 52.30 | 46.99 | 62.87 | Strongest repository coverage area; remaining gaps are targeted and edge-case heavy |
| `test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj` | `PassedWithCoverage` | 0.00 | 0.00 | 0.00 | No tests were discovered |
| `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj` | `PassedWithCoverage` | 58.13 | 52.04 | 70.83 | `UKHO.Search` is strong; `FileShareImageBuilder` is weak |

### 3.3 Notable module baseline

#### Stronger areas
- `UKHO.Search.Ingestion`: `85.65/77.56/99.21`
- `UKHO.Search.Ingestion.Providers.FileShare`: `72.85/65.14/81.20`
- `UKHO.Search`: `84.77/72.14/93.93` from `UKHO.Search.Tests`
- `FileShareEmulator.Common`: `90.20/83.33/100.00`

#### Weak or unexecuted areas
- `UKHO.Aspire.Configuration.Seeder`: `4.37/3.00/4.76`
- `RulesWorkbench`: `27.25/18.57/41.49`
- `FileShareImageBuilder`: `10.54/7.02/8.65`
- `UKHO.Search.Query.Tests` covered no code and discovered no tests
- `FileShareEmulator.Tests` did not complete, so emulator coverage is not trustworthy

### 3.4 Failures and blockers summary
- Failing test project: `test/FileShareEmulator.Tests/FileShareEmulator.Tests.csproj`
- Failing test: `IndexServiceTests.IndexBusinessUnitBatchesAsync_WhenProgressIsRequested_ReportsInitialAndPeriodicProgress`
- Functional blocker: `test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj` contains no authored test files and discovered no tests

## 4. Coverage gap analysis

### 4.1 Critical trustworthiness gaps

#### 4.1.1 `FileShareEmulator.Tests` is unstable
The emulator test project failed before producing trustworthy coverage. Existing authored tests are currently limited to:

- `test/FileShareEmulator.Tests/BusinessUnitLookupServiceTests.cs`
- `test/FileShareEmulator.Tests/IndexServiceTests.cs`

Meanwhile, the emulator source project contains additional uncovered service and UI surfaces, including:

- `tools/FileShareEmulator/Services/BatchDownloadService.cs`
- `tools/FileShareEmulator/Services/IngestionQueueService.cs`
- `tools/FileShareEmulator/Services/StatisticsService.cs`
- `tools/FileShareEmulator/Services/IndexService.cs`
- `tools/FileShareEmulator/Components/Pages/Indexing.razor` (seen in coverage)

This is the highest-priority blocker because the project both fails and leaves several emulator workflows effectively unmeasured.

#### 4.1.2 `UKHO.Search.Query.Tests` is an empty execution shell
Evidence gathered during this run shows:

- no authored test files under `test/UKHO.Search.Query.Tests/`
- zero tests discovered during execution
- the test project currently references `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`
- `src/UKHO.Search.Query/` currently exposes no non-generated source files through project inventory

This indicates a scope and ownership gap rather than only a missing-assertions gap. A future implementation pass must first decide whether this project should test query-domain logic, be repointed to a different subsystem, or be retired.

### 4.2 High-priority missing coverage by subsystem

#### 4.2.1 Configuration seeding
`UKHO.Aspire.Configuration.Seeder.Tests` currently covers only:

- `AdditionalConfigurationKeyBuilder`
- `AdditionalConfigurationFileEnumerator`

Most of the seeder implementation is untested. High-value uncovered files include:

- `configuration/UKHO.Aspire.Configuration.Seeder/Json/ExternalServiceDefinitionParser.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/Services/ConfigurationService.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/Json/JsonStripper.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/Json/JsonFlattener.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/Services/LocalSeederService.cs`
- `configuration/UKHO.Aspire.Configuration.Seeder/Program.cs`

This area has both low structural coverage and an obvious mismatch between production-file count and test-file count.

#### 4.2.2 RulesWorkbench service and Blazor UI flows
The current test project contains strong service-oriented tests for mapper, snapshot, batch-scan, and checker scenarios, but the following remain weak or uncovered:

- `tools/RulesWorkbench/Services/BatchPayloadLoader.cs` is only lightly exercised
- `tools/RulesWorkbench/Components/Pages/Evaluate.razor`
- `tools/RulesWorkbench/Components/Pages/Rules.razor`
- `tools/RulesWorkbench/Components/Pages/Checker.razor`
- service paths that support those page-level flows, including rule evaluation and payload loading interactions

Given repository guidance, page behavior should be validated with Playwright-style end-to-end coverage rather than component-only tests where practical.

#### 4.2.3 FileShare image-builder tooling
`UKHO.Search.Tests` gives a strong baseline for the `UKHO.Search` pipeline/domain assembly, but `FileShareImageBuilder` remains under-tested. Important uncovered or weak files include:

- `tools/FileShareImageBuilder/MetadataImporter.cs`
- `tools/FileShareImageBuilder/ContentImporter.cs`
- `tools/FileShareImageBuilder/DataCleaner.cs`
- `tools/FileShareImageBuilder/ImageLoader.cs`
- `tools/FileShareImageBuilder/ImageExporter.cs`
- `tools/FileShareImageBuilder/ConfigurationReader.cs`
- `tools/FileShareImageBuilder/Program.cs`

Current authored test evidence in `test/UKHO.Search.Tests` shows only `FileShareImageBuilder/DataCleanerIngestionModeTests.cs`, which is insufficient for the size of the tool surface.

### 4.3 Targeted medium-priority gaps in otherwise healthy areas

#### 4.3.1 Ingestion infrastructure edge paths
`UKHO.Search.Ingestion.Tests` is the healthiest suite, but the remaining uncovered areas are high-value infrastructure paths rather than low-value noise. Notable files include:

- `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/FileShareIngestionGraph.cs` (`0%` line coverage in the contributing suite)
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs` (`19.42%` line coverage in the contributing suite)
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Validation/IngestionRulesValidator.cs` (`57.64%` line coverage in the contributing suite)

These gaps are important because they are likely to hide branch-path defects in routing, validation, and bulk indexing failure handling.

#### 4.3.2 Pipeline/framework utility gaps
Multiple raw project summaries report persistent uncovered pipeline base classes such as:

- `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`
- `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`
- `src/UKHO.Search/Pipelines/Nodes/RouteNode.cs`
- `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`

`UKHO.Search.Tests` already exercises many pipeline behaviors, so the remaining deficit is likely concentrated in edge branches, cancellation timing, multi-input interaction, and failure-routing combinations rather than missing happy-path tests.

### 4.4 Areas intentionally de-prioritized
The following were not prioritized for immediate gap closure despite appearing in raw coverage output:

- generated files under `obj/`
- generated OpenAPI support source
- incidental coverage of transitive assemblies when the owning subsystem already has a more appropriate test project

## 5. Test specification to close gaps

### 5.1 Priority order
1. Restore trustworthy execution for `FileShareEmulator.Tests`
2. Define and populate the intended scope for `UKHO.Search.Query.Tests`
3. Raise coverage for `UKHO.Aspire.Configuration.Seeder.Tests`
4. Raise coverage for `RulesWorkbench.Tests` UI and payload-loading flows
5. Raise coverage for `FileShareImageBuilder` under `UKHO.Search.Tests`
6. Add targeted infrastructure edge-case tests to `UKHO.Search.Ingestion.Tests`

### 5.2 Proposed test work packages

| Priority | Subsystem | Intended test type | Suggested target location | Proposed tests | Expected behavior to verify |
| --- | --- | --- | --- | --- | --- |
| P0 | FileShare emulator | `unit` | `test/FileShareEmulator.Tests/IndexServiceProgressTests.cs` or extend `IndexServiceTests.cs` | Stabilize and expand progress-report assertions for indexed business-unit workflows | initial progress, periodic progress, final progress, zero-item paths, cancellation, and no duplicate terminal updates |
| P0 | FileShare emulator | `unit` | `test/FileShareEmulator.Tests/BatchDownloadServiceTests.cs` | Add tests for download batching, empty results, duplicate handling, and failure reporting | correct batch grouping, result counts, partial failures, and deterministic summaries |
| P0 | FileShare emulator | `unit` | `test/FileShareEmulator.Tests/IngestionQueueServiceTests.cs` | Cover queue enqueue/dequeue/result reporting paths | queue status transitions, count reporting, and failure propagation |
| P1 | FileShare emulator UI | `e2e` | future Playwright coverage for emulator pages | Exercise `Indexing.razor` and other operator-facing flows | interactive page behavior, validation messages, progress updates, and action wiring |
| P1 | Query test suite | `unit` | `test/UKHO.Search.Query.Tests/` first authored test file to be created during implementation | Establish actual query-suite purpose before adding volume | either meaningful query-domain tests exist, or the project is intentionally re-scoped/removed |
| P1 | Configuration seeder | `unit` | `test/UKHO.Aspire.Configuration.Seeder.Tests/ExternalServiceDefinitionParserTests.cs` | Validate valid definitions, malformed JSON, missing fields, and normalization | parser accepts supported shapes and rejects invalid definitions deterministically |
| P1 | Configuration seeder | `unit` | `test/UKHO.Aspire.Configuration.Seeder.Tests/JsonStripperTests.cs` and `JsonFlattenerTests.cs` | Cover nested object flattening, arrays, ignored nodes, and formatting edge cases | flattening/stripping results are stable and loss rules are explicit |
| P1 | Configuration seeder | `unit` / `integration` | `test/UKHO.Aspire.Configuration.Seeder.Tests/ConfigurationServiceTests.cs` and `LocalSeederServiceTests.cs` | Cover write ordering, duplicate keys, dry-run or local-seed behavior, and failure handling | configuration writes are idempotent where intended and errors are surfaced clearly |
| P1 | Configuration seeder | `integration` | `test/UKHO.Aspire.Configuration.Seeder.Tests/ProgramStartupTests.cs` | Validate host wiring for seeder startup | startup registration fails fast on invalid config and boots on valid config |
| P2 | RulesWorkbench services | `unit` | `test/RulesWorkbench.Tests/BatchPayloadLoaderMoreCasesTests.cs` | Add malformed payload, oversized payload, missing file, and mixed batch-content cases | payload loading returns stable validation/reporting outcomes |
| P2 | RulesWorkbench services | `unit` | `test/RulesWorkbench.Tests/RuleEvaluationServiceTests.cs` | Cover empty rule sets, invalid inputs, matched/unmatched outputs, and report shaping | evaluation reports contain the expected matched rules and diagnostics |
| P2 | RulesWorkbench UI | `e2e` | future Playwright coverage for `RulesWorkbench` | Cover `Evaluate`, `Rules`, and `Checker` page flows | page renders, user inputs, validation, clipboard/save/evaluate interactions, and result summaries |
| P2 | FileShare image builder | `unit` | `test/UKHO.Search.Tests/FileShareImageBuilder/MetadataImporterTests.cs` | Cover metadata parsing, missing fields, malformed rows, and duplicate handling | importer produces stable documents and rejects malformed input correctly |
| P2 | FileShare image builder | `unit` | `test/UKHO.Search.Tests/FileShareImageBuilder/ContentImporterTests.cs` | Cover content import edge cases and missing file scenarios | content ingestion reports success/failure consistently |
| P2 | FileShare image builder | `unit` | `test/UKHO.Search.Tests/FileShareImageBuilder/ConfigurationReaderTests.cs` | Cover valid, invalid, and partial configuration cases | configuration defaults and validation are explicit |
| P2 | FileShare image builder | `integration` | `test/UKHO.Search.Tests/FileShareImageBuilder/ImageBuilderWorkflowTests.cs` | Cover end-to-end builder workflow with test fixtures | importer/exporter/cleaner composition behaves correctly on realistic sample input |
| P3 | Ingestion infrastructure | `unit` / `integration` | `test/UKHO.Search.Ingestion.Tests/Elastic/ElasticsearchBulkIndexClientFailureTests.cs` | Add bulk partial-failure, retryability, malformed payload, and mapping-error scenarios | client translates Elasticsearch failures into the expected repository behavior |
| P3 | Ingestion provider graph | `integration` | `test/UKHO.Search.Ingestion.Tests/Pipeline/FileShareIngestionGraphCoverageTests.cs` | Exercise graph construction and alternate routing paths not covered today | graph wiring dispatches to expected nodes and fails fast for invalid dependencies |
| P3 | Rules validation | `unit` | `test/UKHO.Search.Ingestion.Tests/Rules/IngestionRulesValidatorMoreCasesTests.cs` | Extend negative validation matrix for path syntax, operator combinations, and schema violations | invalid rules fail fast and valid edge cases remain accepted |
| P3 | Core pipeline nodes | `unit` | `test/UKHO.Search.Tests/Pipelines/*` additions alongside existing suites | Add multi-input, cancellation, and failure-routing branch cases for `MicroBatchNode`, `MultiInputNodeBase`, and related base classes | branch-heavy pipeline behavior remains deterministic under backpressure and cancellation |

### 5.3 Expected future emphasis
The next implementation pass should favor:

- high-signal tests on business-critical behavior and error handling
- extending existing test projects rather than creating redundant overlapping suites
- Playwright-driven UI validation for interactive Blazor surfaces where user interaction is the behavior under test
- branch and failure-path expansion in already healthy ingestion and pipeline suites

## 6. Non-goals and guardrails
- This assessment did not modify production code.
- This assessment did not modify test code.
- This assessment did not modify project files for the purpose of the spec itself.
- This assessment did not fabricate coverage for the failed emulator test project.
- This assessment did not prioritize generated `obj/` source as a manual test-design target.
- This assessment did not decide the long-term ownership of the query test project; it only records the present gap.

## 7. Blockers and follow-up decisions

### 7.1 Blockers
- `FileShareEmulator.Tests` must be made stable before the repository can claim trustworthy emulator coverage.
- `UKHO.Search.Query.Tests` needs an explicit product scope because it currently discovers zero tests.

### 7.2 Follow-up decisions required
- Decide whether `UKHO.Search.Query.Tests` should target a future query subsystem, be repointed, or be removed.
- Decide whether emulator and RulesWorkbench UI verification will be added to existing test projects or implemented as dedicated Playwright coverage.
- Decide whether generated-file filtering should be added to future coverage reporting so gap analysis remains focused on authored code only.

## 8. Acceptance criteria for a future implementation prompt
A future test-implementation prompt should be considered complete only when all of the following are true:

1. `test/FileShareEmulator.Tests/FileShareEmulator.Tests.csproj` passes reliably and produces coverage.
2. `test/UKHO.Search.Query.Tests/UKHO.Search.Query.Tests.csproj` either contains meaningful authored tests or is explicitly re-scoped with documented intent.
3. `UKHO.Aspire.Configuration.Seeder.Tests` includes direct tests for parser, JSON transformation, configuration service, and local seeding behavior.
4. `RulesWorkbench` gains coverage for payload-loading edge cases and interactive page workflows.
5. `FileShareImageBuilder` gains direct tests for importer, loader, exporter, and configuration paths, not only `DataCleaner`.
6. `UKHO.Search.Ingestion.Tests` gains targeted branch/failure-path coverage for graph wiring, rules validation, and bulk indexing failure handling.
7. The next full repository coverage run completes with no failed test projects.
8. The consolidated summary at `./artifacts/test-coverage/coverage-summary.json` can be regenerated from a clean workspace without repository edits.
9. Coverage increases are demonstrated in the affected target areas, with particular emphasis on currently empty or blocked suites rather than only already-strong projects.

## 9. Recommended implementation sequence
1. Fix and expand `FileShareEmulator.Tests`
2. Define `UKHO.Search.Query.Tests` purpose and add first real tests
3. Fill `UKHO.Aspire.Configuration.Seeder.Tests` unit-level gaps
4. Add `RulesWorkbench` UI and payload-loading coverage
5. Add `FileShareImageBuilder` tests
6. Add targeted ingestion infrastructure branch-path tests
