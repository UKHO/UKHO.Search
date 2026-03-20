---
description: Assess repository test coverage with Coverlet and produce a single gap-analysis specification without changing code.
mode: agent
context:
  language: markdown
---

# Test Coverage Prompt (`test.coverage.prompt.md`)

## Purpose
You are a senior test engineer working in this repository.

Your job is to:
1. Ensure every test project in scope is evaluated with **Coverlet** using a **no-repository-code-change** approach.
2. Run the **full automated test suite** across all discovered test projects.
3. Gather **machine-readable coverage output** from Coverlet.
4. Create a **new Work Package** under `./docs/` using the naming strategy defined by `./.github/prompts/spec.research.prompt.md` and the documentation instructions.
5. Write **exactly one markdown specification document** that describes:
   - current coverage baseline,
   - missing or weakly covered areas,
   - blockers preventing coverage collection,
   - and a specification for tests that should be added to close the gaps.

## Non-negotiable guardrail
This prompt must **not change ANY code**.

That means:
- Do **not** modify source files.
- Do **not** modify test files.
- Do **not** modify `.csproj`, `.props`, `.targets`, `.sln`, `appsettings*`, pipeline files, or any other repository code/configuration.
- Do **not** add/remove NuGet package references in the repository.
- Do **not** create helper scripts inside the repository.

Allowed outputs only:
- coverage artifacts under `./artifacts/test-coverage/`
- exactly one new Work Package folder under `./docs/`
- exactly one markdown spec file inside that Work Package folder

## Required preparation
Before doing any work, read and follow:
- `./.github/copilot-instructions.md`
- `./.github/instructions/documentation.instructions.md`
- `./.github/instructions/testing.instructions.md`
- `./.github/prompts/spec.research.prompt.md`

Use the **Work Package naming/location rules** from `spec.research.prompt.md`, but for this workflow you must create **one single markdown spec only**. Do **not** create overview + component split documents.

## Scope discovery
1. Identify all test projects in the solution.
2. Also inspect the `./test/` tree for test projects that may exist even if they are not currently loaded into the solution.
3. Treat each discovered test project as an in-scope coverage target unless you can prove it is intentionally excluded.
4. Work sequentially and verify each command succeeds before moving on.

## Coverlet requirement
You must use **Coverlet**, following Microsoft Learn guidance for .NET code coverage.

### Preferred execution strategy
For each test project, prefer a repository-safe approach in this order:

1. **If the project already references `coverlet.msbuild`**
   - Run the project with MSBuild coverage enabled.
   - Emit both `json` and `cobertura` formats.
   - Use a project-specific output directory under `./artifacts/test-coverage/<project-name>/`.

   Preferred command shape:
   - `dotnet test <path-to-test-csproj> --no-restore /p:CollectCoverage=true /p:CoverletOutput=<output-path> /p:CoverletOutputFormat=json,cobertura`

2. **If the project does not reference `coverlet.msbuild` but does reference `coverlet.collector`**
   - Run it with `XPlat Code Coverage`.
   - Preserve the raw coverage artifact(s) under that project's artifact folder.
   - If JSON is not emitted directly, parse the resulting Coverlet-generated Cobertura XML into the consolidated JSON summary.

   Preferred command shape:
   - `dotnet test <path-to-test-csproj> --no-restore --collect:"XPlat Code Coverage" --results-directory <output-path>`

3. **If the project references neither `coverlet.msbuild` nor `coverlet.collector`**
   - Do **not** edit the project.
   - Use a **non-repository-changing fallback** only, such as an already-available or globally installed `coverlet.console` tool, if that can be done without modifying repository files.
   - If no such fallback is available, record the project as a **coverage tooling blocker** and continue.

### Important rule
For **every** discovered test project, do one of the following:
- successfully collect Coverlet coverage, or
- explicitly record why coverage could not be collected without changing repository code.

Do not silently skip projects.

## Full test-suite execution requirement
You must execute the full automated test suite for all discovered test projects.

Guidance:
- Run test projects individually so coverage and failures are isolated.
- Continue through the full set even if one project fails.
- Record per-project outcome as one of:
  - `PassedWithCoverage`
  - `FailedDuringTestExecution`
  - `BlockedCoverageTooling`
  - `BlockedBuildOrRestore`

## Coverage artifacts
Store raw artifacts under:
- `./artifacts/test-coverage/<project-name>/`

Create a consolidated machine-readable summary at:
- `./artifacts/test-coverage/coverage-summary.json`

The summary should include, at minimum:
- generation timestamp
- repository root
- list of test projects analyzed
- coverage tool used per project (`coverlet.msbuild`, `coverlet.collector`, `coverlet.console`, or `none`)
- execution outcome per project
- overall and per-project totals for line/branch/method coverage when available
- major uncovered assemblies/namespaces/classes/files when derivable
- explicit blockers and failed projects

If some projects only emit Cobertura XML, parse that into the summary JSON rather than changing the repository to force a different output format.

## Documentation / Work Package requirement
After collecting coverage data, create a **new Work Package** in `./docs/` using the numbering and folder naming strategy from `./.github/prompts/spec.research.prompt.md`.

### Folder rules
- Create the **next incremental** folder under `./docs/`
- Use a descriptor aligned to this work, for example: `test-coverage-gaps`
- Keep **all documentation for this task** in that one Work Package folder

### Spec rules
Create **exactly one markdown specification document** in the new Work Package folder.

Recommended filename:
- `spec-domain-test-coverage-gaps_v0.01.md`

Follow repository documentation conventions where applicable, including versioning fields for an initial spec.

Do **not** create multiple spec files.
Do **not** create separate overview/component specs.
Do **not** overwrite an existing spec.

## Required contents of the spec
The single markdown spec must be evidence-based and should include:

1. **Title / metadata**
   - version
   - status/draft marker if appropriate
   - date
   - source inputs used

2. **Objective**
   - why this coverage assessment was run
   - what “gap” means in this repository context

3. **Methodology**
   - how test projects were discovered
   - how Coverlet was invoked
   - artifact locations
   - any limitations or fallbacks used

4. **Coverage baseline**
   - per-project coverage table
   - overall coverage summary
   - failures/blockers summary

5. **Coverage gap analysis**
   - low-coverage or unexecuted projects
   - important assemblies/classes/features with weak coverage
   - notable branches/error paths/edge cases not exercised
   - any infrastructure or tooling gaps preventing trustworthy coverage

6. **Test specification to close gaps**
   - grouped by subsystem/project/class or feature area
   - clear proposed tests or test groups
   - intended test type (`unit`, `integration`, `e2e` only where justified)
   - expected behavior to verify
   - priority/order of implementation
   - suggested target test project/file locations when inferable

7. **Non-goals / guardrails**
   - explicitly state that this assessment did not modify code
   - note any areas intentionally not covered in this run

8. **Blockers and follow-up decisions**
   - missing Coverlet enablement without repo edits
   - failing tests blocking trustworthy coverage
   - environmental dependencies or unstable areas

9. **Acceptance criteria for a future implementation prompt**
   - what a later test-implementation pass must achieve to close the documented gaps

## Collaboration behavior
- Do **not** start a clarification interview unless absolutely blocked.
- Prefer proceeding from repository evidence.
- Ask at most one clarification question only if a hard blocker prevents completion.

## Output requirements
At the end, provide a concise summary containing:
- the discovered test projects
- where raw coverage artifacts were written
- the path to `coverage-summary.json`
- the path to the new Work Package folder
- the path to the single spec markdown file
- key blockers, if any

## Success criteria
This prompt is successful only if all of the following are true:
- all test projects were inspected
- the full test suite was attempted across all discovered test projects
- Coverlet coverage was collected wherever possible without repository code changes
- a consolidated machine-readable coverage summary exists
- a new Work Package folder was created under `./docs/`
- exactly one markdown spec was created in that Work Package folder
- no repository code or project files were changed
