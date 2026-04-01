# Workbench troubleshooting

Read this page when the Workbench shell starts but behaves unexpectedly, when a module or tool does not appear where you expected it, or when output, commands, and tab behavior do not line up with the model described in the rest of the guide.

Use this page symptom-first. The aim is not just to list errors, but to help you decide which ownership layer to inspect next.

## Start with the right ownership question

Most Workbench issues become easier once you classify them correctly.

- If the problem is about discovery, probe roots, or enabled modules, start with host startup and infrastructure.
- If the problem is about explorer content, commands, or runtime contribution visibility, start with Workbench services and module registration.
- If the problem is about what the active tool publishes, start with the tool component and its `ToolContext` usage.
- If the problem is about what the user can see historically, start with the output panel and shell-owned output services.

That framing mirrors the architecture on purpose. The fastest troubleshooting path is usually the one that respects the ownership boundary first.

## Symptom: a module never appears in Workbench

### Likely causes

- the module project was not built, so the probe root contains no assembly
- `modules.json` does not include the correct probe root
- `modules.json` marks the module as disabled
- the assembly name does not begin with `UKHO.Workbench.Modules.`
- the module failed during `assembly-load`, `module-entry`, or `registration`

### What to check

1. Open `src/workbench/server/WorkbenchHost/modules.json` and verify both the probe root and the module id.
2. Build the module project so the configured output directory actually contains the `.dll`.
3. Open the Workbench output panel and look for `Module loader` entries.
4. If needed, inspect application logs for the full exception recorded by `ModuleLoader`.

### Why this works

`ModuleAssemblyScanner` only scans approved probe roots and only for assemblies with the approved prefix. `ModuleLoader` then records failures with explicit startup stages. The output panel mirrors that startup information so you do not have to guess which part of startup failed.

## Symptom: the module is discovered, but no tool appears in the explorer

### Likely causes

- the module registered the tool but not the explorer, section, or explorer item
- ids conflict with an existing contribution and registration failed
- the explorer item points to the wrong explorer or section id
- the command id referenced by the explorer item was never registered

### What to check

1. Re-read the module's `Register(...)` method and compare it to `SearchWorkbenchModule`.
2. Confirm the explorer, section, explorer item, tool, and command ids are all stable and internally consistent.
3. Check startup output for registration-stage failures caused by duplicate ids.

### Why this works

Workbench does not infer explorer structure from tool definitions alone. Explorer contributions are explicit, and duplicate ids are rejected by the contribution registry to preserve deterministic behavior.

## Symptom: double-clicking an explorer item does nothing useful

### Likely causes

- the explorer item's `CommandId` is wrong
- the referenced command was not registered
- the command does not define a valid activation target or execution handler
- the shell surfaced a recoverable failure and wrote the details to logs or output

### What to check

1. Confirm the explorer item and command ids match.
2. Confirm the command resolves to an activation target, an execution handler, or both.
3. Check the output panel and application logs for `Workbench action failed` behavior.

### Why this works

Explorer activation is command-driven. If the command path is broken, the visible explorer surface cannot compensate for it.

## Symptom: Workbench opens a new tab when you expected reuse

### Likely causes

- the activation target identity changed
- a `parameterIdentity` was supplied when you expected singleton reuse
- the logical tab key differs from the one you intended

### What to check

1. Inspect the `ActivationTarget` creation path.
2. Confirm whether `logicalTabKey` or `parameterIdentity` changed between requests.
3. Remember that titles and icons do not control tab reuse; identity does.

### Why this works

Tab reuse is based on `ActivationTarget.CreateTabIdentity()`, not on visible tab text.

## Symptom: Workbench focuses an existing tab when you expected a separate one

### Likely causes

- the tool id and logical tab key are identical across both requests
- no distinguishing `parameterIdentity` was supplied

### What to check

1. Decide whether the two cases are really the same logical target.
2. If not, provide a stable differentiator through `parameterIdentity` or an explicit logical tab key.

## Symptom: menu or toolbar actions do not appear for the active tool

### Likely causes

- the tool never published runtime contributions through `ToolContext`
- the wrong tool owns the contributions
- the tool is not actually the active tab
- the actions were registered as static contributions when you really expected runtime ones, or vice versa

### What to check

1. Inspect the active tool component's `OnParametersSet()` or equivalent initialization path.
2. Confirm `ToolContext.SetRuntimeMenuContributions(...)` and `SetRuntimeToolbarContributions(...)` are actually called.
3. Switch tabs and confirm the expected tool is active.

### Why this works

Runtime contributions are active-tool scoped. If the wrong tool owns them, or they were never published, the shell has nothing correct to compose.

## Symptom: the output panel looks empty or incomplete

### Likely causes

- the panel is collapsed
- the minimum visible level is filtering out the entries you expected
- the panel was cleared during the current session
- you are expecting a toast-only event to remain visible without checking the output history

### What to check

1. Open the panel from the status bar.
2. Change the minimum visible level to `Debug`.
3. Check whether the output stream was cleared.
4. Trigger the action again and watch both the toast and the output history.

### Why this works

The output service retains entries, but the panel can show a filtered subset. The hidden unseen-severity indicator also follows the current visible threshold.

## Symptom: you see a toast, but cannot find the event later

### Likely causes

- the panel is filtered above the notification level
- the output stream was cleared after the toast
- you are inspecting the wrong shell session

### What to check

1. Lower the minimum visible level.
2. Confirm the current session still contains the relevant output history.
3. Remember that the output stream is in-memory and session-scoped.

## Symptom: a tool updates its own UI but not the shell title, badge, or status

### Likely causes

- the tool updated local component state but never called the relevant `ToolContext` method
- the tool context belongs to a different runtime instance than expected
- the shell rejected an update because the tool instance is no longer tracked

### What to check

1. Confirm the component uses the injected `ToolContext` for title, badge, status, selection, or notifications.
2. Check logs for â€œtool instance was not tracked by the shell.â€
3. Verify the update happens while the tab is still open.

### Why this works

The shell only trusts updates from tracked runtime instances. Local component changes alone do not modify shell metadata.

## When to leave this page

- Return to [Workbench architecture](Workbench-Architecture) if you realize the problem is really about startup ownership or module loading boundaries.
- Return to [Workbench commands and tools](Workbench-Commands-and-Tools) if the failure is really command registration or tool-scoped ownership.
- Return to [Workbench output and notifications](Workbench-Output-and-Notifications) if the problem is about historical visibility rather than action execution itself.
