# Workbench tutorials

Read this page after [Workbench output and notifications](Workbench-Output-and-Notifications) when you want practical, code-oriented recipes for extending the current Workbench model.

This page is intentionally tutorial-led rather than purely conceptual. The earlier chapters explain why the design exists. This chapter shows how to work within that design without bypassing the shell.

## Tutorial 1: Read the Search module as the canonical example

Before adding anything new, study `src/Workbench/modules/UKHO.Workbench.Modules.Search/SearchWorkbenchModule.cs`.

That one file already demonstrates most of the model you need to follow:

- explorer registration
- section registration
- tool registration
- host-owned open commands
- tool-scoped runtime commands
- activation-target usage
- explorer-item registration that routes through the command system

If you begin from a random `.razor` file instead, you will usually miss how the registration story fits together.

## Tutorial 2: Add a new Workbench module correctly

Use this recipe when you need a new bounded module rather than just another tool inside an existing module.

### Step 1: create one concrete `IWorkbenchModule` entry point

Your module assembly must expose exactly one concrete `IWorkbenchModule` implementation. `ModuleLoader` enforces that rule during startup.

That means the module entry point is not optional boilerplate. It is the bounded startup contract the host expects.

### Step 2: keep startup registration declarative

Inside `Register(ModuleRegistrationContext context)`, declare only the things the shell needs to know at startup:

- services that genuinely belong in DI before container finalization
- tools
- commands
- explorers, sections, and items
- any static menu, toolbar, explorer-toolbar, or status contributions that are truly startup-scoped

Do not try to reach into layout markup or active-tool state from registration. That is runtime behavior, not startup behavior.

### Step 3: add discovery configuration

Update `src/workbench/server/WorkbenchHost/modules.json` so the host can discover the assembly under an approved probe root and enable the module id.

Today the host scans only configured roots and only for assemblies whose names begin with `UKHO.Workbench.Modules.`. If either condition is missing, the module will not be discovered.

### Step 4: build the module project before starting Workbench

The host scans built assemblies from the configured output directories. If the module project has not been built, the probe root may exist but contain no loadable assembly.

### Step 5: use the output panel during first startup

When Workbench starts, check the output panel for `Module loader` messages. Those entries tell you whether the module was discovered, loaded, skipped, or failed during `assembly-load`, `module-entry`, or `registration`.

## Tutorial 3: Add a tool to an existing module

If you do not need a new module, the cheaper path is to extend an existing module with one more bounded tool.

Use the Search module pattern.

1. Create an `ActivationTarget` for the new tool.
2. Register a `ToolDefinition` with stable id, display name, icon, explorer id, and description.
3. Register a host-owned open command for that tool.
4. Register an explorer item that points to the same command and activation target.
5. Add the tool component itself.

This pattern matters because it keeps explorer, command routing, and activation identity aligned from the beginning.

## Tutorial 4: Publish runtime menu and toolbar actions from a tool

The Search query tool is the current canonical example for runtime shell participation.

In `SearchQueryTool.razor.cs`, the component publishes menu and toolbar contributions during `OnParametersSet()` once the bounded `ToolContext` arrives. The tool does not manipulate shell markup. Instead it declares runtime contributions and lets the shell compose them.

Use that pattern when your tool needs task-specific actions that should appear only while that tool is active.

The flow is:

1. receive `ToolContext`
2. publish runtime contributions once
3. invoke tool-scoped commands through `ToolContext.InvokeCommandAsync(...)`
4. let command handlers update runtime title, badge, status, selection, and notifications through the same `ToolContext`

That sequence keeps the tool expressive without weakening shell boundaries.

## Tutorial 5: Open a related tool from the current tool

When one tool needs to send the user to another tool, do not navigate by reaching into shell internals. Use `ToolContext.OpenToolAsync(...)` with a new `ActivationTarget`.

This matters because the shell still needs the chance to apply normal activation rules:

- reuse an existing logical tab when identity matches
- create a new logical tab when identity differs
- update active-tool state consistently

Tool-to-tool navigation is therefore just another shell activation request, not a special case.

## Tutorial 6: Raise notifications and let the shell preserve them

When a tool completes an operation and needs to tell the user, use `ToolContext.NotifyAsync(...)`.

That does two useful things for you.

- the shell shows the user-safe toast immediately
- the shell mirrors the message into the output history for later review

That means you do not need to build a separate local notification log inside the tool.

## Tutorial 7: Decide whether a behavior belongs in startup or runtime

This is one of the most useful extension questions in the whole Workbench guide.

Put the behavior in **startup registration** when it describes what exists before any tool becomes active.

Put the behavior in **runtime tool logic** when it depends on the active tool instance, current selection, or current task.

Examples:

- explorer items belong in startup registration
- a tool-local `Run` button belongs in runtime command publication and command handling
- the explorer-to-center splitter belongs in host shell layout
- the current title of an active query tab belongs in runtime `ToolContext.SetTitle(...)`

If a change seems to belong in both places, the design usually wants a startup declaration plus a runtime update path, not direct shell mutation.

## Recommended next pages

- Continue to [Workbench troubleshooting](Workbench-Troubleshooting) for symptom-led help when these recipes do not behave as expected.
- Return to [Workbench modules and contributions](Workbench-Modules-and-Contributions) if you need the bounded registration surface again.
- Return to [Workbench commands and tools](Workbench-Commands-and-Tools) if you need to reason about command ownership and activation targets in more detail.
