# Wiki Audit Working Notes

## Purpose

This working note records the current `wiki/` corpus audit, the agreed target information architecture for the overhaul, and the execution rules that Work Item 1 establishes for the rest of the work package.

## Audit scope

Current `wiki/` markdown pages reviewed for this audit:

1. `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`
2. `wiki/Documentation-Source-Map.md`
3. `wiki/FileShare-Provider.md`
4. `wiki/Home.md`
5. `wiki/Ingestion-Pipeline.md`
6. `wiki/Ingestion-Rules.md`
7. `wiki/Ingestion-Service-Provider-Mechanism.md`
8. `wiki/keycloak-workbench-integration.md`
9. `wiki/Metrics-in-the-Aspire-Dashboard.md`
10. `wiki/Project-Setup.md`
11. `wiki/Provider-Metadata-and-Split-Registration.md`
12. `wiki/Solution-Architecture.md`
13. `wiki/Tools-Advanced-FileShareImageBuilder.md`
14. `wiki/Tools-FileShareImageLoader-and-FileShareEmulator.md`
15. `wiki/Tools-RulesWorkbench.md`
16. `wiki/Workbench-Layout.md`
17. `wiki/Workbench-Shell.md`

## Current wiki inventory and mapping

| Current page | Topic | Strengths to preserve | Outdated wording / restructuring risk | Mermaid | Key links today | Outcome | Planned destination in final IA |
|---|---|---|---|---:|---|---|---|
| `Home.md` | Landing page and quick repo orientation | Concise repo summary, good entry-point list, helpful runtime entry-point snapshot | Too flat as a start-here page; mixes onboarding with historical lineage; reading order is not audience-guided | 1 | `Solution-Architecture`, `Project-Setup`, tool pages, ingestion pages, `Workbench-Shell`, source map | Rewrite | Keep as `Home.md` with guided reading paths, section summaries, and a single `current as of` note |
| `Solution-Architecture.md` | Current solution and runtime architecture | Strong project map, layering summary, useful runtime diagrams | Needs more narrative guidance and clearer newcomer path; should stop being the only architecture destination | 2 | Ingestion and tooling pages | Rewrite + pair | Keep as architecture overview and add `Architecture-Walkthrough.md` |
| `Project-Setup.md` | Local setup, AppHost modes, ACR image flow | Preserves critical commands and current local workflow accurately | Reads like an operational checklist more than a guided journey; Keycloak setup is separated elsewhere | 1 | Tool pages, ingestion pipeline | Rewrite + pair | Keep as setup overview and add `Setup-Walkthrough.md`, `Setup-Troubleshooting.md`, and `Appendix-Command-Reference.md` |
| `Ingestion-Pipeline.md` | Runtime flow and pipeline stages | Strong current-state runtime explanation, good stage ordering, useful diagrams | Needs to become the narrative entry point into a larger ingestion section rather than carrying the whole section alone | 2 | Metrics, provider, rules, canonical document pages | Rewrite + pair | Keep as ingestion overview and add `Ingestion-Walkthrough.md` and `Ingestion-Troubleshooting.md` |
| `Ingestion-Rules.md` | Rules engine authoring guide | Deepest rules content in the current wiki, preserves supported syntax and semantics | Too large for a single narrative page; should become a deep guide plus a quick reference companion | 1 | `Tools-RulesWorkbench`, ingestion pages, source map | Split | Keep as rules deep dive and add `Appendix-Rule-Syntax-Quick-Reference.md` |
| `CanonicalDocument-and-Discovery-Taxonomy.md` | Canonical discovery model and taxonomy | Clear explanation of canonical intent, normalization, and search surfaces | Works well as a supporting page but should be linked from ingestion and architecture paths more intentionally | 2 | Ingestion and provider pages | Rewrite | Retain as a focused supporting ingestion page |
| `Ingestion-Service-Provider-Mechanism.md` | Provider abstraction and runtime boundary | Good explanation of infrastructure/provider split and envelope context | Should sit inside a clearer ingestion reading path; current title is reference-heavy but still accurate | 2 | Provider and ingestion pages | Rewrite | Retain as a supporting ingestion architecture page |
| `Provider-Metadata-and-Split-Registration.md` | Shared provider model and split registration | Good explanation of metadata/runtime split and enablement rules | Reference style is useful, but page needs positioning inside a guided ingestion path | 0 | Provider mechanism, File Share provider, source map | Rewrite | Retain as a supporting ingestion/provider page |
| `FileShare-Provider.md` | Concrete File Share provider deep dive | Strong provider-specific implementation detail and handler overview | Better as a deeper supporting page under ingestion walkthrough/provider context rather than a peer to major top-level pages | 1 | Ingestion and rules pages | Rewrite | Retain as a supporting provider deep dive |
| `Tools-FileShareImageLoader-and-FileShareEmulator.md` | Loader and emulator workflow | Clear explanation of tool responsibilities and local indexing workflow | Better grouped under setup and ingestion journeys than as a loose tools list | 2 | Setup, image builder, ingestion, File Share provider | Absorb | Absorb into setup walkthrough, setup troubleshooting, and ingestion walkthrough content |
| `Tools-Advanced-FileShareImageBuilder.md` | Export/build data-image workflow | Preserves advanced export workflow and operational cautions | Better as advanced setup content and command reference support than a standalone top-level page | 1 | Setup, loader/emulator page | Absorb | Absorb into setup walkthrough and appendix command reference |
| `Tools-RulesWorkbench.md` | Stable `RulesWorkbench` capabilities | Valuable explanation of `Rules` and `Checker` pages and current save/checker behavior | Better grouped with ingestion rules and Workbench material; page title feels tool-centric rather than task-centric | 2 | `Home`, `Project-Setup`, `Ingestion-Rules`, `Ingestion-Pipeline`, `Solution-Architecture` | Split / absorb | Absorb rules authoring content into ingestion pages; absorb UI/runtime concepts into new Workbench guide pages |
| `Workbench-Shell.md` | Current Workbench shell history and capability slices | Richest source for Workbench shell concepts, output panel, tabs, contributions, and runtime model | Strongly phase-oriented (`slice adds`, `bootstrap slice`, `first tabbed shell slice`); must be redistributed into newcomer-friendly guide pages | 0 | None | Absorb + retire | Redistribute into `Workbench-Introduction.md`, `Workbench-Architecture.md`, `Workbench-Shell-Guide.md`, `Workbench-Commands-and-Tools.md`, `Workbench-Tabs-and-Layout.md`, `Workbench-Output-and-Notifications.md`, and `Workbench-Tutorials.md`; then retire this page |
| `Workbench-Layout.md` | `UKHO.Workbench.Layout` component guide | Good detailed authoring and troubleshooting reference for the layout library | Reads as a component reference, not a guided Workbench page; should support layout/tabs documentation without owning the whole Workbench story | 0 | None | Absorb / partial retain | Fold shell-layout concepts into `Workbench-Tabs-and-Layout.md`; retain detailed reference content there or in closely linked subsections |
| `keycloak-workbench-integration.md` | Keycloak and Workbench auth setup | Preserves operationally sensitive local auth guidance and export workflow | Too isolated from setup flow; exact commands matter and must remain verbatim when moved | 0 | None | Absorb | Absorb into setup walkthrough, setup troubleshooting, and appendix command reference |
| `Metrics-in-the-Aspire-Dashboard.md` | Ingestion metrics and Aspire usage | Useful observability guidance with concrete meter/instrument details | Needs a clearer place in the architecture/ingestion journey; can stay focused if cross-linked well | 0 | Ingestion pipeline, setup, source map | Rewrite | Retain as a focused observability/supporting page linked from architecture and ingestion |
| `Documentation-Source-Map.md` | Historical source-map into `dev/work-packages/mvp/` | Useful for tracing design history during implementation work | Conflicts with the specification decision to avoid a dedicated documentation-lineage page in the final wiki | 0 | Many current pages | Retire / absorb | Retire as a standalone wiki page; preserve any still-useful historical context through selective links in working notes or page-level references where needed |

## Workbench shell redistribution baseline

`wiki/Workbench-Shell.md` is explicitly marked for redistribution and retirement.

Useful content to preserve during redistribution:

- Workbench shell purpose and audience framing
- module discovery and `modules.json` composition model
- command and runtime contribution model
- tab lifecycle, activation, overflow, and singleton activation behavior
- output panel, notifications, and status-bar interactions
- host, service, infrastructure, and module project responsibilities
- startup flow and runtime interaction flow
- practical verification behaviors that can be rephrased into tutorials and troubleshooting

Content to remove from the main narrative while preserving substance:

- phase-history labels such as `bootstrap slice`, `what the ... slice adds`, and work-package-specific chronology
- implementation-order storytelling that is no longer needed once the guide is reorganized by concept

## Target naming convention

The overhaul will use an area-led, book-like naming convention:

- root entry page stays `Home.md`
- major narrative sections use stable area-first names such as `Solution-Architecture.md`, `Project-Setup.md`, `Ingestion-Pipeline.md`, and `Workbench-Introduction.md`
- deeper guided pages use `*-Walkthrough.md`, `*-Troubleshooting.md`, or a similarly explicit area-specific title
- appendix/reference support pages use the `Appendix-*.md` prefix
- central shared terminology uses `Glossary.md`

This keeps related pages grouped naturally in file listings while making reading order obvious from the page names.

## Target top-level reading paths from `Home.md`

### Start here

1. `Home.md`
2. `Glossary.md`
3. `Solution-Architecture.md`
4. `Architecture-Walkthrough.md`

### Setup path

1. `Project-Setup.md`
2. `Setup-Walkthrough.md`
3. `Setup-Troubleshooting.md`
4. `Appendix-Command-Reference.md`

### Ingestion path

1. `Ingestion-Pipeline.md`
2. `Ingestion-Walkthrough.md`
3. `Ingestion-Rules.md`
4. `Appendix-Rule-Syntax-Quick-Reference.md`
5. `Ingestion-Troubleshooting.md`
6. Supporting pages: `CanonicalDocument-and-Discovery-Taxonomy.md`, `Ingestion-Service-Provider-Mechanism.md`, `Provider-Metadata-and-Split-Registration.md`, `FileShare-Provider.md`, `Metrics-in-the-Aspire-Dashboard.md`

### Workbench path

1. `Workbench-Introduction.md`
2. `Workbench-Architecture.md`
3. `Workbench-Shell-Guide.md`
4. `Workbench-Modules-and-Contributions.md`
5. `Workbench-Commands-and-Tools.md`
6. `Workbench-Tabs-and-Layout.md`
7. `Workbench-Output-and-Notifications.md`
8. `Workbench-Tutorials.md`
9. `Workbench-Troubleshooting.md`

### Maintenance path

1. `.github/instructions/wiki.instructions.md`
2. `.github/prompts/spec.execute.prompt.md`
3. `.github/prompts/spec.plan.prompt.md`

## Overview and walkthrough pairing decisions

Major topics that require paired overview and walkthrough pages:

- Architecture: `Solution-Architecture.md` + `Architecture-Walkthrough.md`
- Setup: `Project-Setup.md` + `Setup-Walkthrough.md`
- Ingestion: `Ingestion-Pipeline.md` + `Ingestion-Walkthrough.md`
- Workbench: `Workbench-Introduction.md` as the guide entry page plus `Workbench-Tutorials.md` for worked usage and extension flows

Topics that remain best as single focused supporting pages after rewrite:

- `CanonicalDocument-and-Discovery-Taxonomy.md`
- `Ingestion-Service-Provider-Mechanism.md`
- `Provider-Metadata-and-Split-Registration.md`
- `FileShare-Provider.md`
- `Metrics-in-the-Aspire-Dashboard.md`
- `Glossary.md`

Topics that should become appendix/reference support:

- `Appendix-Command-Reference.md`
- `Appendix-Rule-Syntax-Quick-Reference.md`

## Narrative-depth and missing-content baseline

The overhaul must now treat depth preservation and missing-content review as explicit completion gates, not as optional polish.

### Major subjects that require chapter-like treatment

The following subjects must remain developed, book-like narrative pages rather than collapsing into short landing pages, thin tables, or bullet-heavy reference notes:

- **Architecture**: explain not only project boundaries, but also why the Onion layering, canonical-document boundary, provider edge model, AppHost orchestration, and Workbench composition are arranged this way.
- **Setup**: explain why the local environment is split into import, services, and export loops; why the data image workflow exists; how local auth and local tools fit into the developer journey; and what a contributor should verify after startup.
- **Ingestion runtime foundations and workflows**: explain the graph runtime, envelopes, lanes, backpressure, supervision, rules, and indexing flow in narrative form before dropping into quick-reference syntax.
- **Workbench**: explain the shell, contribution model, module boundaries, tool activation, tabs, output, and extension path as a connected guide rather than isolated capability notes.
- **Maintenance instructions and prompts**: explain the expected workflow and reporting obligations clearly enough that future work packages cannot miss the wiki review step.

### Missing-content review checkpoints for later rewrites

Every rewritten major page must be checked against both the page it replaces and any narrower pages it absorbs so that valid current-state content is not accidentally dropped.

| Target slice | Source pages to compare | Content that must not go missing during rewrite |
|---|---|---|
| Architecture path | `Solution-Architecture.md`, related architecture links from `Home.md`, supporting observability/tool pages | Onion rationale, host/composition-root responsibilities, provider-model boundary, canonical-query separation, developer-tooling role, observability links, and newcomer guidance on where to read next |
| Setup path | `Project-Setup.md`, `Tools-FileShareImageLoader-and-FileShareEmulator.md`, `Tools-Advanced-FileShareImageBuilder.md`, `keycloak-workbench-integration.md` | Run-mode rationale, image naming and import lifecycle, loader/emulator responsibilities, ACR pull/push flow, Keycloak/Workbench auth expectations, realm import/export caveats, and post-start verification steps |
| Ingestion path | `Ingestion-Pipeline.md`, `Ingestion-Rules.md`, provider/mechanism/canonical pages, `Tools-RulesWorkbench.md`, metrics page | Current supported rule syntax, runtime terminology, canonical-document meaning, provider registration boundaries, diagnostics, and the distinction between full-pipeline behavior and tool-assisted rule experimentation |
| Workbench path | `Workbench-Shell.md`, `Workbench-Layout.md`, relevant sections from `Tools-RulesWorkbench.md` | Shell/runtime concepts, commands, tabs, output, module discovery, contribution boundaries, troubleshooting cues, and extension tutorials |

When a rewrite concludes that no content from an absorbed page belongs in the new destination, that decision should be explicit in the work-package notes rather than implicit by omission.

## Preservation and rewriting rules

To preserve valid information while removing phase-oriented wording:

1. Rewrite all major pages into present-tense current-state guidance.
2. Preserve exact operational commands verbatim where syntax accuracy matters.
3. Move implementation history out of the main narrative unless the history directly explains the current design.
4. Prefer absorbing narrow operational pages into stronger guided sections rather than leaving isolated top-level pages.
5. Preserve useful Mermaid concepts, but refresh diagrams so they match the rewritten guided structure.
6. Replace disconnected tool-centric navigation with task-based reading paths from `Home.md`.
7. Retire standalone pages only after all useful content has been preserved elsewhere in the new structure.

## Execution rules baseline for the work package

1. This overhaul remains markdown-only except for the required markdown instruction and prompt updates in Work Item 6.
2. No source-code behavior changes are planned anywhere in this work package.
3. If incidental source-code work becomes unavoidable, `./.github/instructions/documentation-pass.instructions.md` remains a hard gate in full.
4. The final work item for this work package must explicitly record the mandatory wiki review result, including which pages were updated, created, retired, or intentionally left unchanged.
5. `wiki/Home.md` is the final verification anchor for the overhauled reading paths.
6. `Documentation-Source-Map.md` and `Workbench-Shell.md` are both planned retirement pages, but only after their valid content has been absorbed into the new structure.
