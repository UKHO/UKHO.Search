# Workbench shell

The `083-workbench-model` bootstrap slice introduces the first runnable Workbench shell under `src/workbench/server/WorkbenchHost`.

## What the bootstrap slice delivers

- a desktop-like shell rendered by Blazor Server
- a full-width menu bar, active-tool toolbar, activity rail, explorer, central tool surface, and status bar
- a host-owned exemplar tool (`Workbench overview`) that opens in the center region
- singleton tool activation, so reopening the same tool focuses the existing instance instead of duplicating it
- shell layout built with `UKHO.Workbench.Layout` grid and splitter primitives

## What the dynamic module-loading slice adds

- a host-owned `modules.json` file under `src/workbench/server/WorkbenchHost` that declares approved probe roots and per-module enablement flags
- reflection-based discovery of assemblies named `UKHO.Workbench.Modules.*` before the host finalizes the DI container
- a bounded `IWorkbenchModule` registration contract in `UKHO.Workbench` so modules can register services and tools without direct shell access
- structured discovery and load logging, plus buffered user-safe startup notifications shown by the interactive shell
- the initial module map from `UKHO.Workbench.Modules.Search`, `UKHO.Workbench.Modules.PKS`, `UKHO.Workbench.Modules.FileShare`, and `UKHO.Workbench.Modules.Admin`, all of which open through the same singleton shell activation path as host-owned tools

## What the command-and-runtime-contribution slice adds

- explorer items are now declarative Workbench contributions backed by command ids and activation targets rather than direct component activation in the layout
- commands are now the shared action abstraction across explorer buttons, menu items, toolbar buttons, and hosted tool interactions
- the shell now composes menu, toolbar, and status-bar surfaces from static contributions plus runtime contributions published by the active tool instance only
- `ToolContext` now provides bounded runtime capabilities for command invocation, tool activation requests, title/icon/badge updates, notifications, selection publication, and runtime shell contribution updates
- the dummy `Search query` tool now demonstrates active-tool runtime menu, toolbar, and status-bar participation, while `Search ingestion`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration` prove the first repository-specific multi-module tool map

## Project responsibilities

| Project | Responsibility |
|---|---|
| `src/workbench/server/UKHO.Workbench` | Shared shell contracts and models such as shell regions, tool definitions, tool instances, activation targets, and shell state. |
| `src/workbench/server/UKHO.Workbench.Services` | Shell orchestration, including command routing, explorer composition, runtime contribution composition, fixed context projection, tool activation, and the host-facing `WorkbenchShellManager`. |
| `src/workbench/server/UKHO.Workbench.Infrastructure` | `modules.json` reading, probe-root scanning, bounded reflection-based module loading, and composition root extensions. |
| `src/workbench/server/WorkbenchHost` | Blazor host composition, shell UI, startup bootstrap, module discovery orchestration, and host-owned notifications. |
| `src/Workbench/modules/UKHO.Workbench.Modules.Search` | Dynamic Search module assembly contributing the dummy `Search ingestion`, `Search query`, and `Ingestion rule editor` tools. |
| `src/Workbench/modules/UKHO.Workbench.Modules.PKS` | Dynamic PKS module assembly contributing the dummy `PKS operations` tool. |
| `src/Workbench/modules/UKHO.Workbench.Modules.FileShare` | Dynamic File Share module assembly contributing the dummy `File Share workspace` tool. |
| `src/Workbench/modules/UKHO.Workbench.Modules.Admin` | Dynamic Admin module assembly contributing the dummy `Administration` tool. |

## Startup flow for the Workbench shell

1. `WorkbenchHost` registers the Workbench infrastructure and service-layer dependencies.
2. `WorkbenchHost` reads `modules.json`, resolves probe roots, and scans for enabled `UKHO.Workbench.Modules.*` assemblies.
3. Valid modules register services and tool definitions through the bounded `IWorkbenchModule` contract before DI finalization.
4. Host startup registers the `Workbench overview` tool and then applies module-contributed tools to the singleton shell manager.
5. The shell manager selects the bootstrap explorer and activates the first enabled module tool when one is available, otherwise it falls back to the host-owned overview tool.
6. `MainLayout` renders the desktop-like shell chrome and replays any buffered startup notifications.
7. `Index` renders the active tool into the center working surface through `DynamicComponent`.

## Runtime interaction flow

1. A user selects an explorer item, menu item, toolbar button, or hosted tool button.
2. The interaction resolves to a registered `CommandContribution`.
3. The shell manager executes the command, which either opens a declarative activation target or runs a bounded tool/host handler.
4. The active tool instance can update its title, icon, badge, selection, notifications, and runtime shell contributions through `ToolContext`.
5. The shell recomposes menu, toolbar, and status-bar surfaces so only the active tool contributes runtime items.

## Verification

Use the targeted commands from `docs/083-workbench-model/implementation-plan.md` for this slice:

- `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Infrastructure.Tests/UKHO.Workbench.Infrastructure.Tests.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
- `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
- `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`

When the host starts, browse to `/` and confirm the shell loads with the enabled module map visible in the explorer, that `Search ingestion`, `Search query`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration` open in the center region, and that reopening them re-focuses the existing singleton tool instance. Disable one or more modules in `modules.json` and restart to confirm the disabled tools disappear from the explorer.
