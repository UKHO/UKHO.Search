# Next-Gen Arc 10 Work Packages: Legacy UI Retirement And Operational Hardening

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 10 retires or deactivates old UI surfaces once replacements exist, then hardens observability, audit, authorization, documentation, and operational safety. It should not remove useful local development capability prematurely.

## Numbering

Arc 10 work packages use WP280-WP287.

Reserved buffer after Arc 10: WP288-WP299.

## Evidence Checked

- AppHost services mode includes Query, Ingestion, FileShareEmulator, RulesWorkbench, WorkbenchHost, Keycloak, Elasticsearch, storage, SQL, and configuration emulator support: [../../src/Hosts/AppHost/AppHost.cs](../../src/Hosts/AppHost/AppHost.cs).
- Current UI surfaces include [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs), [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs), [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs), [../../tools/FileShareEmulator/Program.cs](../../tools/FileShareEmulator/Program.cs), and [../../src/Workbench/server/WorkbenchHost/Program.cs](../../src/Workbench/server/WorkbenchHost/Program.cs).
- Workbench shell/modules are under [../../src/Workbench/](../../src/Workbench/). Retained Studio source exists under [../../src/Studio/](../../src/Studio/) and [../../src/Providers/UKHO.Search.Studio.Providers.FileShare/](../../src/Providers/UKHO.Search.Studio.Providers.FileShare/) but is not active in [../../Search.slnx](../../Search.slnx) or AppHost.

## WP280: Define Retirement Readiness Gates And Capability Mapping

Scope:
- Define evidence required before each old UI surface can be removed, disabled, left local-only, or retained temporarily.

Requirements carried:
- Retire Blazor/Razor developer surfaces and Workbench only after replacements exist.
- Do not remove FileShareEmulator local development controls as part of React consolidation.
- Configuration emulator remains out of scope.
- Map each old UI capability to a replacement API/UI capability or explicit local-only retention.

Validation anchors:
- Review against Arcs 04, 06, 07, 08, and 09 completion status.

## WP281: Retire QueryServiceHost Blazor UI

Scope:
- Remove, deactivate, or repurpose QueryServiceHost browser UI once Arc 04 APIs, Arc 07 workbench, and Arc 09 end-user search cover its workflows.

Validation anchors:
- Query API tests, React query-rule workbench tests, end-user search tests, and AppHost startup tests.

## WP282: Split Or Retire IngestionServiceHost Browser UI While Preserving Runtime

Scope:
- Remove/deactivate ingestion Blazor pages and keep ingestion runtime hosted-service behavior where required.

Requirements carried:
- Browser-host auth may become unnecessary if no browser pages remain.
- Runtime ingestion pipeline must continue to process provider queues.

Validation anchors:
- Ingestion hosted-service tests, queue processing tests, AppHost startup, and Arc 08 repair workspace tests.

## WP283: Retire Or Transition RulesWorkbench

Scope:
- Retire RulesWorkbench or mark it as transitional once Arc 06 APIs and Arc 08 workspace replace its workflows.

Requirements carried:
- Rule browsing/editing/saving/evaluation/checker/business-unit scan workflows move behind APIs and React views.

Validation anchors:
- Rules API tests, repair workspace tests, and clearly scoped transitional tests if any.

## WP284: Retire Workbench Shell, Dummy Modules, And Unneeded UI Infrastructure

Scope:
- Remove/deactivate Workbench shell machinery, dummy modules, module discovery, contribution registries, custom splitters, tabs, old hosts, samples, and demo material when no longer needed.

Requirements carried:
- Do not port Workbench shell mechanics unless a concrete product requirement appears.

Validation anchors:
- Solution build, AppHost startup, test cleanup, and no dangling project references.

## WP285: Resolve Retained Studio Source

Scope:
- Finish the retained Studio decision from Arc 02: revive/refactor, mine/delete, rename/reframe, or explicitly preserve as historical source.

Requirements carried:
- Studio contains useful API ideas but is detached and file-share-bound.
- Ambiguous retained source is a planning hazard.

Validation anchors:
- Search.slnx/AppHost/project reference checks and Studio tests either active/passing or intentionally removed/archived.

## WP286: Harden Authorization, Audit, Observability, And Local-Only Safety

Scope:
- Verify operational hardening across new APIs and retired surfaces.

Requirements carried:
- Rule promotion, repair replay, forced replay, destructive operations, and sensitive diagnostics require authorization and audit.
- Local-only destructive operations remain local-only.
- Forced replay, if allowed, is audited and explicitly controlled.
- Health/readiness/environment diagnostics remain available.

Validation anchors:
- API auth tests, audit tests, AppHost smoke, and route inventory checks.

## WP287: Update Documentation And Supersede Older UI Directions

Scope:
- Update docs/wiki to reflect React plus shadcn/ui, API-first, journal-backed repair, query diagnostics, and legacy retirement direction.

Requirements carried:
- Older Studio/Theia/PrimeReact/Workbench directions are explicitly superseded.
- FileShareEmulator local-only and configuration emulator out-of-scope boundaries remain clear.
- Documentation-pass requirements remain non-negotiable in later coding plans.

Validation anchors:
- Documentation review and link checks where available.

## Arc Requirement Cross-Check

- Retire old Blazor/Razor developer surfaces and Workbench in stages: WP280-WP284.
- Preserve FileShareEmulator local-only and configuration emulator out-of-scope boundaries: WP280, WP286-WP287.
- Avoid porting Workbench mechanics: WP284.
- Resolve retained Studio source status: WP285.
- Harden observability, audit, authorization, forced replay, repair actions, and local-only destructive boundaries: WP286.
- Update documentation and supersede older UI directions: WP287.

## Roadmap Completion Note

At the end of Arc 10, the consolidated React plus shadcn/ui application should be backed by stable APIs, the ingestion journal should be the accepted-input source of truth for repair workflows, end-user search should use production contracts, and legacy UI surfaces should be retired or deliberately retained inside documented local-only boundaries.