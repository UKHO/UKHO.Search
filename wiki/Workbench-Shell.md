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

## What the first tabbed shell slice adds

- logical tab identity is now a bounded shell concept, so reopening the same logical target focuses the existing tab instead of replacing the current center-surface content
- the shell now tracks ordered open tabs, an active tab, explorer-item selection, and most-recently-active tab history for close behavior
- explorer single-click now selects an item without opening it, while explorer double-click routes through the shared command path and opens or focuses the matching tab immediately
- the center surface now renders a visible tab strip above the content area and keeps inactive tab components mounted so open tabs preserve in-memory state while they remain open
- closing the last remaining tab returns the shell to an explicit empty-state center surface and restores explorer focus

## What the tab lifecycle and metadata slice adds

- activation targets now carry bounded parameter identity, so the same tool can reuse a tab for matching parameters and open separate tabs for different parameter identities
- explorer-owned activation paths now seed initial tab title and icon metadata, and hosted views can replace that metadata immediately through `ToolContext` even while their tabs are inactive
- tab close now marks the runtime tool instance disposed as soon as the shell removes the tab, while Blazor continues to dispose the hosted component through normal render-tree removal
- the tab strip now exposes a basic right-click context menu with `Close` only, and that action routes through the same shell close path used by the visible tab-close button
- shell diagnostics now cover runtime title and icon updates as well as activation and close flows, so metadata changes remain traceable in logs

## What the overflow and tooltip slice adds

- the shell now tracks a bounded visible tab-strip window separately from the full logical open-tab order, so overflow activation can reveal hidden tabs with minimal strip movement instead of reordering the underlying tab collection
- the center tab strip now renders an always-visible overflow dropdown on the right and keeps its entries text-only, with active-tab indication but no close, filter, or search affordances in this first implementation
- both visible-strip tab titles and overflow entry titles now truncate with ellipsis and open a Radzen tooltip on every hover so long runtime titles remain readable without changing the stock Material look and feel
- overflow selection now flows through the shared shell activation path, which keeps diagnostics, active-tab composition, and visible-window adjustments aligned with the rest of the tab lifecycle

## What the shell style refinement slice adds

- the outer shell now renders flush with the browser viewport instead of using decorative outer padding, which makes the Workbench read more like a desktop surface than a padded web page
- the second-row toolbar no longer shows an `Active tab` eyebrow label and now surfaces the host-owned `Home` action in that leading position
- the activity rail now renders as an icon-only strip, while hover and focus use the shared Radzen tooltip service so explorer labels remain discoverable without persistent rail text
- the host-owned `Overview` menu and toolbar action is now labeled `Home` to match the refined shell chrome
- the working area now keeps the activity rail fixed at `64px` with no splitter between that rail and the explorer, leaving only the explorer-to-centre boundary resizeable
- the centre tab host now removes its extra top, bottom, and left shell padding so the tab strip sits flush with the content surface while the always-visible overflow affordance stays anchored to the right edge

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
4. Host startup registers the `Workbench overview` tool and then applies module-contributed tools to the tab-aware shell manager.
5. The shell manager selects the bootstrap explorer and activates the first enabled module tool when one is available, otherwise it falls back to the host-owned overview tool.
6. `MainLayout` renders the desktop-like shell chrome and replays any buffered startup notifications.
7. `Index` renders the open tab collection into the center working surface and shows only the active tab while inactive tabs remain mounted.

## Runtime interaction flow

1. A user selects or double-clicks an explorer item, or invokes a menu item, toolbar button, or hosted tool button.
2. The interaction resolves to a registered `CommandContribution`.
3. Explorer single-click updates shell selection only, while a command-driven activation request opens a new tab or focuses the existing logical tab.
4. The active tool instance can update its title, icon, badge, selection, notifications, and runtime shell contributions through `ToolContext`.
5. The shell recomposes menu, toolbar, and status-bar surfaces so only the active tool contributes runtime items.

## Verification

Use the targeted commands from `docs/084-workbench-tabs/implementation-plan.md` for this slice:

- `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Services.Tests/UKHO.Workbench.Services.Tests.csproj`
- `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
- `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`

When the host starts, browse to `/` and confirm the shell loads with the enabled module map visible in the explorer, that `Search ingestion`, `Search query`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration` open in the center region, and that reopening them re-focuses the existing singleton tool instance. Disable one or more modules in `modules.json` and restart to confirm the disabled tools disappear from the explorer.
