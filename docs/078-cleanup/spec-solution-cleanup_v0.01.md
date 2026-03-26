# Work Package: `078-cleanup` — Remove Theia runtime integration and documentation references

**Target output path:** `docs/078-cleanup/spec-solution-cleanup_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for the Theia cleanup work item.
- `v0.01` — Captures the intent to remove Theia runtime/build integration, purge Theia references from wiki and other non-`docs/` documentation, and remove `StudioServiceHost` from Aspire without deleting its code.
- `v0.01` — Confirms that the documentation cleanup scope includes both `./wiki` and `./docs/**`.
- `v0.01` — Confirms that `StudioServiceHost` code remains in the repository but should be removed from the solution `.slnx` file as part of the cleanup.
- `v0.01` — Confirms that this work item removes Theia references and integration first, while the physical deletion of `src/Studio/Server` is handled separately.
- `v0.01` — Confirms that all Theia-related projects and tests should be removed from the repository solution `.slnx` file, not only `StudioServiceHost`.
- `v0.01` — Confirms the documentation purge rule for mixed content: trim Theia-only sections from mixed documents and delete documents that are primarily Theia-specific.
- `v0.01` — Confirms that obsolete Theia- and Studio-specific Aspire parameters and configuration should also be removed when they exist only to support the discontinued workflow.
- `v0.01` — Confirms that Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks should be removed when they exist only to support the discontinued workflow.
- `v0.01` — Confirms that `UKHO.Search.Studio.*` and similar Theia-related provider code outside `src/Studio/Server` should be kept in the repository for later refactor, but removed from Aspire, the solution `.slnx` file, and active documentation.
- `v0.01` — Confirms that retained non-Theia projects must stop referencing `UKHO.Search.Studio.*` and other Studio/Theia code when those references exist only for the discontinued workflow.
- `v0.01` — Confirms that if removing Studio/Theia references creates feature gaps, implementation must stop and review each gap case-by-case rather than applying a blanket fallback rule.

## 1. Overview

### 1.1 Purpose

This specification defines a cleanup work item to remove the repository's active Theia-based client integration from local developer workflows while preserving code that is still needed for later refactoring.

The purpose of this work item is to stop Theia from participating in Aspire orchestration and the Visual Studio `F5` developer startup/build experience, remove the soon-to-be-deleted server-side Theia runtime folder from active repository assumptions, and clean repository documentation so it no longer directs contributors toward Theia-based tasks.

### 1.2 Scope

This specification currently includes:

- removing Theia-related Aspire integration
- removing Theia-related Visual Studio `F5` build/startup integration
- removing Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks that existed only to support the discontinued workflow
- removing obsolete Theia- and Studio-specific Aspire parameters and configuration when they exist only to support the discontinued workflow
- removing references to `src/Studio/Server` so it is no longer an active runtime, build, or documentation dependency ahead of separate physical deletion
- reviewing `wiki` and `docs/**` and purging Theia-related tasks or instructions
- removing `StudioServiceHost` from Aspire integration while explicitly keeping its code in the repository for later refactoring
- removing `StudioServiceHost` from the repository solution `.slnx` file so it no longer participates in normal solution build/load behavior
- removing all Theia-related projects and tests from the repository solution `.slnx` file so they no longer participate in normal solution build/load behavior
- removing Theia-related provider projects such as `UKHO.Search.Studio.*` from Aspire, the repository solution `.slnx` file, and active documentation while keeping their code in the repository for later refactoring
- removing references from retained non-Theia projects when those references exist only to support the discontinued Studio/Theia workflow

This specification currently excludes:

- refactoring or deleting `StudioServiceHost` code
- defining the replacement UI architecture

### 1.3 Stakeholders

- developers using Visual Studio `F5` and Aspire for local development
- maintainers of local orchestration and startup configuration
- maintainers of repository wiki and developer documentation
- future contributors who will replace the Theia-based experience with a different UI approach

### 1.4 Definitions

- `Theia runtime integration`: any active Aspire, startup, build, or configuration wiring that launches, builds, proxies to, or otherwise assumes the Theia-based client/runtime is present
- `Visual Studio F5 build process`: any project or solution build/startup behavior triggered by the normal Visual Studio debug/run path
- `documentation purge`: removing outdated Theia-specific instructions from `wiki` and `docs/**`, trimming Theia-only sections from mixed documents, and deleting documents that are primarily Theia-specific

## 2. System context

### 2.1 Current state

The repository has previously contained a Theia-based client/runtime under `src/Studio/Server`, together with Aspire integration, Visual Studio `F5` startup/build expectations, and associated documentation.

The direction has now changed:

- the Theia-based client will not continue
- `src/Studio/Server` is intended to be deleted, but only after references and integration have been removed
- `StudioServiceHost` code must remain in the repository for later refactoring, but it should no longer participate in Aspire
- repository documentation outside `docs/` should stop directing users toward Theia workflows

### 2.2 Proposed state

In the proposed state:

- Aspire no longer launches or depends on the Theia client/runtime
- obsolete Theia- and Studio-specific Aspire parameters and configuration are removed when they are no longer needed by any retained workflow
- Visual Studio `F5` no longer builds or assumes the Theia client/runtime
- Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks are removed when they are no longer needed by any retained workflow
- `StudioServiceHost` is removed from Aspire integration but retained in source control for later work
- `StudioServiceHost` is also removed from the repository solution `.slnx` file
- all other Theia-related projects and tests are also removed from the repository solution `.slnx` file
- `UKHO.Search.Studio.*` and similar Theia-related provider projects outside `src/Studio/Server` are retained in source control for later refactoring but removed from Aspire, the solution `.slnx` file, and active documentation
- retained non-Theia projects no longer reference `UKHO.Search.Studio.*` or other Studio/Theia code when those references only supported the discontinued workflow
- `src/Studio/Server` is no longer treated as an active runtime or build dependency and can then be deleted separately
- `wiki` and other non-`docs/` documentation no longer contain active Theia-oriented setup, build, verification, or operational guidance

### 2.3 Assumptions

- mixed documentation can be retained when non-Theia content remains valuable after Theia-specific sections are removed
- fully Theia-centric documents within the confirmed cleanup scope can be deleted as part of the purge
- code preservation for `StudioServiceHost` means its source remains present even though its active orchestration wiring is removed
- any feature gaps created by removing Studio/Theia references require explicit review on a case-by-case basis rather than an automatic replacement strategy
- the requested cleanup is focused on active integration and documentation, not on implementing the replacement UI

### 2.4 Constraints

- remove `StudioServiceHost` from Aspire integration without deleting its code
- remove `StudioServiceHost` from the repository solution `.slnx` file without deleting its code
- remove all other Theia-related projects and tests from the repository solution `.slnx` file
- remove Theia-related provider projects such as `UKHO.Search.Studio.*` from Aspire and the repository solution `.slnx` file without deleting their code
- remove references from retained non-Theia projects when those references exist only for discontinued Studio/Theia features
- stop and review with the user on a case-by-case basis if removing Studio/Theia references creates a feature gap in a retained non-Theia project
- remove Theia integration from Visual Studio `F5` build/startup behavior
- remove Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks when they only exist for the discontinued workflow
- remove obsolete Theia- and Studio-specific Aspire parameters and configuration when they are only present for the discontinued workflow
- remove references and integration before any separate physical deletion of `src/Studio/Server`
- purge Theia tasks from `wiki` and `docs/**`
- trim mixed documents rather than deleting them when they still contain useful non-Theia guidance
- delete documents that are primarily Theia-specific when they fall within the confirmed cleanup scope
- leave the actual manual deletion of historical work-package folders to the user where they have stated they will handle manual cleanup separately

### 2.5 Open questions

1. Documentation scope is confirmed as `./wiki` and `./docs/**`.
2. `StudioServiceHost` is confirmed to stay in source control but be removed from the repository solution `.slnx` file.
3. The current work item removes Theia references and integration first; physical deletion of `src/Studio/Server` is handled separately.
4. All Theia-related projects and tests should be removed from the repository solution `.slnx` file.
5. Documentation handling is confirmed as a combination approach: trim mixed documents and delete documents that are primarily Theia-specific.
6. Obsolete Theia- and Studio-specific Aspire parameters and configuration should be removed when they only support the discontinued workflow.
7. Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks should be removed when they only support the discontinued workflow.
8. `UKHO.Search.Studio.*` and similar Theia-related provider code outside `src/Studio/Server` should stay in source control for later refactoring, but be removed from Aspire, the solution `.slnx` file, and active documentation.
9. Retained non-Theia projects should stop referencing `UKHO.Search.Studio.*` and other Studio/Theia code when those references exist only for the discontinued workflow.
10. If removing Studio/Theia references creates a feature gap, implementation must stop and review that gap case-by-case with the user because no blanket safe rule is approved.

## 3. Component / service design (high level)

### 3.1 Components in scope

1. `Aspire AppHost integration`
   - remove active Theia-related resources and `StudioServiceHost` registration from Aspire
   - remove obsolete Theia- and Studio-specific parameters and configuration that only supported the discontinued workflow

2. `Visual Studio F5/startup integration`
   - remove build/start behavior that assumes the Theia runtime under `src/Studio/Server`
   - remove Theia-only prebuild, postbuild, MSBuild, npm, and yarn hooks that existed only to support that workflow

3. `src/Studio/Server`
   - no longer treated as an active runtime/build dependency because references and integration are removed first
   - can then be deleted separately once the repository no longer depends on it

4. `StudioServiceHost`
   - retained in source but no longer wired into Aspire
   - removed from the repository solution `.slnx` file so it no longer participates in normal solution load/build flows

5. `Theia-related projects and tests`
   - removed from the repository solution `.slnx` file when they are part of the discontinued Theia workflow
   - no longer participate in normal solution load/build flows

6. `Theia-related provider projects outside src/Studio/Server`
   - retained in source control for later refactoring
   - removed from Aspire and the repository solution `.slnx` file when they are part of the discontinued Theia workflow
   - no longer described as active runtime dependencies in documentation

7. `Retained non-Theia projects with Studio/Theia references`
   - remove project references and runtime wiring when those references exist only for discontinued Studio/Theia features
   - keep the retained non-Theia projects active after they are decoupled from the discontinued workflow
   - stop for case-by-case review if decoupling creates a feature gap that needs a replacement decision

8. `wiki` and `docs/**`
   - remove or rewrite Theia-related setup, usage, and operational guidance within those locations
   - trim only the Theia-specific portions of mixed documents that still contain useful non-Theia guidance
   - delete documents that are primarily Theia-focused

### 3.2 High-level flows

#### Cleanup flow

1. active Aspire resources and dependencies are reviewed
2. Theia runtime and `StudioServiceHost` entries are removed from Aspire wiring
3. Visual Studio `F5` startup/build assumptions for Theia are removed
4. repository documentation outside `docs/` is reviewed and Theia-related tasks are purged
5. the repository no longer directs or requires contributors to use the Theia-based client flow

### 3.3 Key decisions recorded so far

- Theia-based client work is discontinued
- `src/Studio/Server` is expected to be deleted
- `StudioServiceHost` code stays in the repository for future refactoring
- `UKHO.Search.Studio.*` and similar Theia-related provider code outside `src/Studio/Server` also stays in the repository for future refactoring, but should no longer participate in active solution or runtime flows
- retained non-Theia projects must be decoupled from Studio/Theia references where those references only served the discontinued workflow
- any resulting feature gaps must be reviewed individually rather than handled through a single default rule
- mixed documentation should be trimmed while primarily Theia-specific documents within scope should be deleted
