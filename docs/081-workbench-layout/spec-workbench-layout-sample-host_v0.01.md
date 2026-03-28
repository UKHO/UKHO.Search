# Work Package: `081-workbench-layout` — Layout sample extraction into `LayoutSample`

**Target output path:** `docs/081-workbench-layout/spec-workbench-layout-sample-host_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for preserving the existing Workbench layout sample in `LayoutSample` before the current host is replaced.
- `v0.01` — Captures that a new standalone Blazor Server sample project named `LayoutSample` must be created under `src/Workbench/samples`.
- `v0.01` — Captures that the code for the existing layout demo route `/layout/splitter-column` must be moved exactly as it is and hosted as the home page and only page in the new sample project.
- `v0.01` — Captures that the new sample must not be added to Aspire orchestration and must remain runnable stand-alone.
- `v0.01` — Captures that the new sample project must be added to `Search.slnx`, with solution-folder placement unimportant.
- `v0.01` — Captures that the Workbench layout wiki page must be updated to reference `LayoutSample` as the runnable showcase location.
- `v0.01` — Confirms that any required page code, styles, helpers, or other assets must be copied into `LayoutSample`.
- `v0.01` — Confirms that the wiki update should include the explicit `dotnet run --project src/Workbench/samples/LayoutSample/LayoutSample.csproj` command.
- `v0.01` — Confirms that the wiki should explicitly replace old sample-host references with `LayoutSample` and leave no references to `WorkbenchHost`.
- `v0.01` — Confirms that `LayoutSample` is intended to become the standalone canonical example of how the Workbench layout works.
- `v0.01` — Confirms that `LayoutSample` should contain only the extracted sample page as it exists now, with no navigation or template shell chrome.
- `v0.01` — Confirms that sensible defaults are acceptable for unspecified details, including keeping the current page title and presentation unchanged unless a minimal hosting change is required.
- `v0.01` — Confirms that the draft is now sufficiently clarified and has no remaining open questions.

## 1. Overview

### 1.1 Purpose

This specification defines a small preservation change for the Workbench layout sample experience.

The purpose is to keep the existing layout showcase available as a standalone canonical example of how the Workbench layout works after the current host is replaced by a new version that will no longer contain the current sample page.

### 1.2 Scope

This specification includes:

- creating a new Blazor Server sample project at `src/Workbench/samples/LayoutSample`
- moving the existing layout showcase code into that new sample
- hosting the moved sample as the home page and only page in the new project
- adding the new project to `Search.slnx`
- updating the Workbench layout wiki page to reference the sample project as the canonical example

This specification excludes:

- adding the sample to Aspire orchestration
- redesigning, simplifying, or extending the layout sample content
- changing the behaviour of the sample beyond what is necessary to host it in a standalone sample project
- broader host replacement work beyond the layout sample preservation described here

### 1.3 Stakeholders

- Workbench developers maintaining layout components
- developers using the layout showcase as a runnable reference
- maintainers of the current sample host
- repository maintainers responsible for solution structure and wiki guidance

### 1.4 Definitions

- `LayoutSample`: the new standalone Blazor Server sample project to be created under `src/Workbench/samples`
- `layout showcase`: the existing demo currently served from `/layout/splitter-column`
- `stand-alone sample`: a project that can be run directly without Aspire orchestration

## 2. System context

### 2.1 Current state

The repository currently documents the Workbench layout showcase as living in the current host project and being runnable at `/layout/splitter-column`.

The wiki page `wiki/Workbench-Layout.md` currently points developers to the existing host for the runnable showcase.

A forthcoming replacement of `WorkbenchHost` will remove this sample page, which would otherwise leave the layout showcase without a stable runnable home.

### 2.2 Proposed state

After this work package:

- a new standalone Blazor Server project named `LayoutSample` exists at `src/Workbench/samples/LayoutSample`
- the code currently used for the route `/layout/splitter-column` is moved into `LayoutSample`
- the moved page is hosted at the sample project's home route `/` as its only page
- the sample remains runnable directly as an isolated sample project
- no Aspire orchestration changes are introduced for this sample
- `Search.slnx` includes the new sample project
- `wiki/Workbench-Layout.md` references `LayoutSample` as the runnable layout sample and contains no references to `WorkbenchHost`
- `LayoutSample` is treated as the standalone canonical example of how the layout works
- `LayoutSample` may begin from the default Blazor project structure, but any extra template pages, navigation, or shell chrome are removed so only the layout sample remains
- any supporting assets needed from the current host are copied into `LayoutSample`
- the wiki guidance includes the explicit `dotnet run --project src/Workbench/samples/LayoutSample/LayoutSample.csproj` command for running the sample

### 2.3 Assumptions

- the user expects the layout sample code to be moved without redesign or feature changes
- "move the code exactly as it is" means the showcase content, structure, and behaviour should remain functionally equivalent after relocation, with only the minimal host/bootstrap changes needed for the new project
- the new sample project may depend on existing Workbench projects as needed to compile and run the moved page
- the new sample is intended as a developer sample and not as part of the main application runtime topology
- if the sample currently relies on page-local support assets in the current host, those assets should be copied into `LayoutSample`
- documentation and presentation should position `LayoutSample` as the canonical runnable reference for Workbench layout behaviour
- the current page title and presentation should remain unchanged unless a minimal hosting adaptation is required

### 2.4 Constraints

- the new project must be a Blazor Server project
- the new project must be created under `src/Workbench/samples`
- the project name must be `LayoutSample`
- the moved layout sample must be hosted at the home route `/` and be the only page in the new project
- the sample must not be added to Aspire orchestration
- the project must be added to `Search.slnx`
- the wiki update must explicitly point developers to `LayoutSample`
- the wiki update must leave no references to `WorkbenchHost`
- any default template pages, navigation, or shell chrome not needed for the layout sample must be removed from `LayoutSample`
- any supporting styles, helper classes, or other assets required by the moved sample must be copied into `LayoutSample`
- `LayoutSample` must be presented as the canonical standalone example of Workbench layout behaviour
- the extracted page title and presentation should remain as currently authored unless a minimal hosting adaptation is required

## 3. Component / service design (high level)

### 3.1 Components

1. `LayoutSample project`
   - a new Blazor Server sample host under `src/Workbench/samples/LayoutSample`
   - owns the runnable sample entrypoint and routing surface

2. `Moved layout showcase page`
   - the existing sample currently served from `/layout/splitter-column`
   - becomes the home page and only page in `LayoutSample`

3. `Solution registration`
   - the `Search.slnx` entry required so the sample is discoverable in the solution

4. `Workbench layout wiki page`
   - updated documentation describing where developers can find and run the layout sample

### 3.2 Data flows

#### Developer discovery flow

1. A developer opens the repository wiki page for Workbench layout guidance.
2. The wiki points the developer to `LayoutSample` as the runnable showcase.
3. The developer runs the sample directly without needing Aspire orchestration.

#### Sample execution flow

1. A developer starts `LayoutSample` directly.
2. The sample host renders its home page.
3. The home page displays the moved layout showcase content that previously lived in `WorkbenchHost`.

### 3.3 Key decisions

- the sample is preserved by relocation rather than by keeping it in the evolving `WorkbenchHost`
- the sample is preserved by relocation into `LayoutSample`
- the new sample is intentionally standalone and excluded from Aspire orchestration
- the moved sample should preserve the existing page content and behaviour rather than being refactored into a new demonstration
- solution registration and wiki guidance are part of the same work so the sample remains easy to discover and run

## 4. Functional requirements

1. The system shall create a new Blazor Server project named `LayoutSample` under `src/Workbench/samples`.
2. The system shall move the existing layout sample code currently used for `/layout/splitter-column` into `LayoutSample`.
3. The system shall host the moved sample at the home route `/` of `LayoutSample`.
4. The system shall not add any other user-facing pages to `LayoutSample` as part of this work.
5. The system shall preserve the moved sample code exactly as it exists today, except for the minimum project, bootstrap, routing, and dependency changes required to host it in the new sample project.
6. The system shall allow `LayoutSample` to run stand-alone without Aspire orchestration.
7. The system shall not add `LayoutSample` to Aspire orchestration.
8. The system shall add `LayoutSample` to `Search.slnx`.
9. The system may place the `Search.slnx` entry in any suitable solution folder because exact solution location is not important for this work item.
10. The system shall update `wiki/Workbench-Layout.md` to reference `LayoutSample` as the runnable layout sample project.
11. The system shall update the wiki so no references to `WorkbenchHost` remain.
12. The system shall include the explicit command `dotnet run --project src/Workbench/samples/LayoutSample/LayoutSample.csproj` in the wiki run guidance.
13. The system shall preserve the existing sample content by copying any required project-local code or assets into `LayoutSample`.
14. The system may use the default Blazor project structure as the starting point for `LayoutSample`.
15. The system shall remove any extraneous template pages, navigation, or shell chrome created by the default project template so `LayoutSample` only displays the extracted layout sample page.
16. The system shall copy any supporting page code, styles, helper classes, or other assets needed by the moved sample into `LayoutSample`.
17. The system shall present `LayoutSample` in documentation as the standalone canonical example of how the Workbench layout works.
18. The system shall keep `LayoutSample` focused on the single extracted page and shall not add a navigation menu, sidebar, header shell, or other template chrome around it.
19. The system shall retain the current page title and overall page presentation as they exist today, except where minimal host-level adaptation is required to run the extracted page as a standalone sample.

## 5. Non-functional requirements

1. The resulting sample shall remain easy for developers to run locally with normal .NET project execution tooling.
2. The sample relocation shall minimize behavioural drift from the current layout showcase.
3. The change shall keep repository documentation aligned with the new sample location.
4. The sample project structure shall fit the existing repository layout conventions for Workbench-related code.

## 6. Data model

No new persistent data model is required for this work item.

The work is limited to project structure, sample-page hosting, and documentation updates.

## 7. Interfaces & integration

- `LayoutSample` will integrate with the existing Workbench layout component library needed by the moved sample page.
- `Search.slnx` must reference the new project so it appears in the main solution.
- `wiki/Workbench-Layout.md` must reference `LayoutSample` as the runnable example and contain no references to `WorkbenchHost`.
- Aspire orchestration is intentionally not part of the integration surface for this work item.
- any supporting assets currently local to the existing sample host must be duplicated into `LayoutSample` if needed by the moved sample.

## 8. Observability (logging/metrics/tracing)

No new observability requirements are introduced by this work item beyond whatever is normally present in a default runnable sample host.

## 9. Security & compliance

No new security or compliance requirements are introduced by this work item.

The sample remains an internal developer-facing runnable example inside the repository.

## 10. Testing strategy

- verify the new sample project can build successfully
- verify the sample can run directly as a standalone Blazor Server project
- verify the home page renders the moved layout showcase content
- verify the wiki guidance points to `LayoutSample` and contains no references to `WorkbenchHost`
- verify `Search.slnx` includes the new project

## 11. Rollout / migration

1. Create `LayoutSample` under `src/Workbench/samples`.
2. Move the existing layout showcase code from `WorkbenchHost` into the new project.
3. Configure the moved sample as the home page and only page.
4. Add the project to `Search.slnx`.
5. Update `wiki/Workbench-Layout.md` to reference the new sample.
6. Validate that the sample runs standalone.

## 12. Open questions

None currently.
