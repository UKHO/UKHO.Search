# Workbench Tool Model

## Overview and Specification Basis

**Status:** Draft architecture overview  
**Purpose:** Standalone conceptual model for a modular workbench that hosts independently authored utility applications (“Tools”) discovered from assemblies and integrated into a shared shell  
**Audience:** Architects, platform engineers, UI framework authors, and tool authors  
**Scope:** Conceptual and structural design only. No code, API signatures, or implementation details are defined here.

---

## 1. Executive Summary

This document defines a conceptual model for a **modular workbench** that hosts **independently authored utility applications**, referred to as **Tools**. Tools are discovered from assemblies, loaded by reflection, and integrated into a common user interface shell.

The shell is heavily inspired by workbench-style environments such as Theia: it contains explorers, menus, toolbars, tabbed content areas, auxiliary panels, and shared workbench services. However, the workbench is **not** merely a UI shell for simple panels. A hosted Tool may contain its own substantial UI, workflow, and business logic that are largely independent of the workbench itself.

The architecture therefore distinguishes between:

- **Workbench responsibilities**, which include discovery, composition, layout, hosting, shared services, and cross-cutting integration.
- **Tool responsibilities**, which include domain-specific UI, workflows, business logic, and internal state.

The workbench hosts tools through a **bounded contract**. Tools contribute explorers, commands, menus, toolbars, status items, and hosted content into the shell, while using shared platform services such as messaging, notifications, persistence, dialogs, and contextual command routing.

This document is intended to act as the basis of a future formal specification.

---

## 2. Problem Statement

The system must support a plugin-style architecture in which utility applications can be authored separately, packaged in assemblies, discovered by reflection, and hosted within a unified workbench shell.

The workbench must provide:

- A consistent host environment
- A tabbed/dockable shell
- Common navigation structures such as explorers
- Integration with a menu bar and contextual menus
- Shared runtime services
- A bounded model for interaction between tool and host
- A platform suitable for tools that are small and simple, or large and business-logic-heavy

The system must avoid two extremes:

1. Treating hosted tools as if they are only lightweight views or panels
2. Allowing tools to directly control the shell in an uncontrolled way

The model therefore needs a clear separation of concerns, a rich contribution model, and a controlled runtime relationship between tool and workbench.

---

## 3. Architectural Positioning

The system should be understood as a **workbench that hosts tools**.

A Tool is both:

- The plugin/discovery unit found in an assembly
- The authored utility application that the workbench hosts

This means the architecture is not primarily centered on “views” or “widgets.” Instead, the main hosted unit is the **Tool**, and the main runtime entity is the **ToolInstance**.

The workbench provides the environment in which tools run. It owns layout, navigation surfaces, contribution composition, lifecycle mediation, persistence, and common services. A tool owns its own domain behavior and UI, and interacts with the workbench through defined contracts.

---

## 4. Design Goals

The model should satisfy the following goals.

### 4.1 Primary Goals

- Support reflection-based discovery of tools from assemblies
- Support user-authored tools with substantial UI and business logic
- Provide a Theia-like workbench shell model
- Allow tools to contribute to multiple shell surfaces
- Preserve clean boundaries between host and tool
- Centralize action behavior through commands
- Support contextual enablement and visibility rules
- Support runtime state, persistence, and restoration
- Allow future growth without forcing redesign of the core model

### 4.2 Secondary Goals

- Allow lazy activation and efficient startup
- Allow shell-native functionality and tool-native functionality to coexist
- Make the model understandable to both platform authors and tool authors
- Support future formalization into a specification and then code contracts

---

## 5. Non-Goals

The following are outside the scope of this conceptual model.

- Concrete code-level interfaces or API signatures
- Detailed dependency injection design
- Security sandboxing or permission enforcement
- Detailed packaging or distribution mechanisms
- Specific persistence formats
- Detailed docking algorithm design
- Specific message types or command grammars
- Specific Blazor implementation patterns

These may be addressed in future design documents.

---

## 6. Core Conceptual Distinction: Workbench vs Tool

The foundational distinction in this architecture is between the **Workbench** and the **Tool**.

### 6.1 Workbench Responsibilities

The workbench owns cross-cutting host concerns, including:

- Tool discovery and loading
- Shell composition and layout
- Explorer hosting
- Menu bar construction
- Toolbar construction
- Tabbed content hosting
- Status bar hosting
- Shared command routing
- Shared context and selection propagation
- Messaging/event infrastructure
- Notifications and dialogs
- Persistence and restoration
- Shared settings and theming

### 6.2 Tool Responsibilities

A tool owns its authored application concerns, including:

- Domain-specific UI
- Business logic
- Workflows and internal navigation
- Local state management
- Interaction with back-end or domain services
- Validation and domain commands
- Tool-specific screen composition
- Internal tool behavior unrelated to the workbench shell

### 6.3 Boundary Principle

The workbench provides **composition, contracts, and shared services**.

The tool owns **its UI, workflow, and domain logic**.

This principle is central to the architecture and should be preserved in all future specifications.

---

## 7. Top-Level Model

The architecture is structured around the following major concepts.

### 7.1 Static Concepts

These represent discovered definitions and contributions.

- Workbench
- Tool
- ToolManager
- ToolRegistry or ContributionCatalog
- ExplorerContribution
- ExplorerSectionContribution
- CommandContribution
- MenuContribution
- ToolbarContribution
- StatusBarContribution
- SettingsContribution

### 7.2 Runtime Concepts

These represent live hosted behavior.

- WorkbenchShell
- ToolInstance
- ToolContext
- Explorer
- ExplorerSection
- ExplorerItem
- Selection state
- Context state
- Open tabs and shell regions

### 7.3 Shared Service Concepts

These provide capabilities used by both workbench and tools.

- ShellService
- ExplorerService
- CommandService
- MenuService
- ContextService
- SelectionService
- NotificationService
- DialogService
- PersistenceService
- SettingsService
- TaskService
- Messenger/EventBus

---

## 8. Tool as the Primary Plugin Unit

A **Tool** is the primary extension and hosting unit in the architecture.

A Tool is reflection-discovered from an assembly and represents an independently authored utility application that integrates into the workbench.

### 8.1 What a Tool Is

A Tool should be understood as:

- A discoverable plugin unit
- A declared set of workbench contributions
- A hosted application entry point
- A source of commands, explorer content, and runtime interaction

### 8.2 What a Tool Contributes

A Tool may contribute any of the following:

- Hosted content to open in the shell
- One or more explorers
- One or more explorer sections
- Explorer item providers
- Commands
- Menu items
- Toolbar items
- Status bar items
- Settings definitions
- Background tasks or task integrations
- Search or quick-open integrations (future)

### 8.3 What a Tool Is Not

A Tool is not merely:

- A view
- A sidebar widget
- A menu provider only
- A passive assembly of utility types

It is a first-class authored application integrated into the workbench.

---

## 9. Tool Instance Model

A Tool definition is static. When hosted in the shell, it becomes a **ToolInstance**.

### 9.1 Purpose of ToolInstance

A ToolInstance represents a running, open, hosted copy of a tool within a tab or panel.

One Tool may produce:

- A single instance only, if it is singleton
- Multiple instances, if it supports multi-instance usage

### 9.2 ToolInstance Responsibilities

A ToolInstance should conceptually hold or manage:

- Identity of the runtime instance
- Reference to the Tool definition
- Current input or activation payload
- Current title, icon, and badge state
- Current dirty/busy state if relevant
- State needed for persistence and restoration
- Any active runtime contributions associated with the instance

### 9.3 Why Tool and ToolInstance Must Be Separate

Separating Tool from ToolInstance allows the platform to distinguish between:

- A discovered tool type
- A particular running copy of that tool in the shell

This distinction is necessary for tab hosting, state restoration, title changes, instance-scoped context, and multi-instance tools.

---

## 10. Workbench Shell Model

The **WorkbenchShell** is the runtime model of the visible host UI.

It is responsible for organizing visible regions, hosting open tool instances, and mediating layout behavior.

### 10.1 Conceptual Regions

The shell may contain the following conceptual regions:

- Menu Bar
- Activity rail or explorer selector
- Left sidebar
- Main tabbed content area
- Right sidebar or inspector area
- Bottom panel
- Status bar

These are conceptual surfaces to which tools and the workbench may contribute.

### 10.2 Shell Responsibilities

The shell owns:

- Open tool instances
- Active tool instance
- Active explorer
- Region visibility and arrangement
- Tab groups and tab activation
- Layout persistence and restoration
- Docking and placement logic
- Closing and focus transitions

### 10.3 Shell Principle

The shell owns **composition and hosting**.

Tools do not directly own the shell.

---

## 11. Contribution Model

The workbench is driven by a **contribution model**. A Tool declares contributions into known extension surfaces, and the workbench composes them.

### 11.1 Why Contributions Exist

Contributions allow the architecture to remain modular without giving tools direct uncontrolled authority over the shell.

A tool declares what it offers. The workbench decides how that offer is composed into the user experience.

### 11.2 Contribution Ownership

A Tool is the owner of its contributions.

A contribution is a specific piece of integration provided by a tool.

### 11.3 Main Contribution Categories

The main contribution categories are:

- Explorer contributions
- Explorer section contributions
- Command contributions
- Menu contributions
- Toolbar contributions
- Status bar contributions
- Settings contributions
- Hosted content/open behavior

These categories are sufficient for a strong first version of the model.

---

## 12. Explorer Model

The **Explorer** is a workbench-owned navigation and action surface, typically hosted in a sidebar.

It is not merely a launcher list. It is a discovery, navigation, selection, and context surface.

### 12.1 Explorer Responsibilities

An explorer may:

- Present navigable structures
- Display trees, lists, or grouped content
- Act as a source of global selection
- Publish context into the workbench
- Expose section-level or item-level actions
- Open or focus tools
- Invoke commands
- Show badges and statuses
- Provide filtering or search
- Provide empty states or onboarding guidance

### 12.2 Explorer as a Context Source

A central role of the explorer is to establish **current context** for other parts of the workbench.

Selecting or activating an item may:

- Change current selection
- Change visible commands
- Change visible menu items
- Change toolbar state
- Influence inspectors or detail panes
- Publish a semantic context value

This makes the explorer a first-class interaction surface, not only a launcher.

---

## 13. ExplorerSection Model

An **ExplorerSection** is a collapsible region within an explorer.

Examples might include:

- Favorites
- Connections
- Jobs
- Recent Items
- Active Sessions
- Pinned Resources

### 13.1 ExplorerSection Responsibilities

A section may define:

- Title
- Order
- Initial expanded/collapsed state
- Visibility conditions
- Item provider or source
- Section header actions
- Empty-state presentation

### 13.2 Static and Dynamic Sections

A section may be:

- Static, with known content
- Dynamic, generated from a provider
- Lazily loaded
- Refreshable
- Filterable

---

## 14. ExplorerItem Model

The **ExplorerItem** is the primary rendered node or entry inside an explorer section.

This replaces vague terminology such as “widget” for sidebar content.

### 14.1 What an ExplorerItem May Represent

An ExplorerItem may represent:

- A resource or entity
- A logical group
- A tree node
- A shortcut into a tool
- An action entry
- A recent item
- A pseudo-node such as “Create New” or “Add Connection”

### 14.2 ExplorerItem Metadata

An ExplorerItem may conceptually expose:

- Identity
- Item type or semantic type
- Label
- Description or subtitle
- Icon
- Badge
- Tooltip
- Children or child availability
- Busy/loading state
- Context value
- Default activation behavior
- Available actions

### 14.3 ExplorerItem Behaviors

An ExplorerItem may support:

- Select
- Activate
- Expand or collapse
- Rename
- Refresh children
- Drag and drop
- Multi-select operations
- Context menu
- Inline actions

### 14.4 Context Value

Each ExplorerItem should expose a semantic **context value** describing what it is, such as:

- `connection`
- `job`
- `historyEntry`
- `file`
- `service`
- `asset`

This context value is important for command visibility, menu visibility, toolbar rules, and contextual interaction.

---

## 15. ExplorerItemProvider Model

Non-trivial explorer content should typically be provided by an **ExplorerItemProvider** rather than hard-coded item lists.

### 15.1 Provider Responsibilities

A provider may be responsible for:

- Supplying root items for a section
- Supplying children for parent items
- Refreshing a section or item branch
- Applying filters
- Resolving dynamic badges or status
- Supporting drag and drop semantics

### 15.2 Why Providers Matter

Providers allow explorer content to reflect live data, lazy expansion, dynamic state, and runtime updates without burdening the shell with domain-specific logic.

---

## 16. ActivationTarget Model

An explorer item should not directly create UI. Instead, it should expose an **ActivationTarget** or equivalent concept describing what happens when the item is activated.

### 16.1 Common Activation Targets

An activation may result in:

- Open Tool X
- Open Tool X with ToolInput Y
- Focus an existing ToolInstance
- Invoke Command C with argument A
- Publish selection or context only

### 16.2 Why ActivationTarget Matters

This allows explorer items to remain flexible and declarative. It also prevents navigation logic from becoming tightly coupled to component instantiation.

---

## 17. Command Model

Commands should be a **first-class primitive** in the architecture.

A command represents a semantic action that can be invoked from multiple surfaces.

### 17.1 Why Commands Are Central

Commands unify behavior across:

- Menu items
- Toolbar buttons
- Explorer inline actions
- Explorer context menus
- Command palette
- Keyboard shortcuts
- Tab headers or other shell chrome

This avoids duplicated action logic and keeps behavior consistent.

### 17.2 Command Responsibilities

A command should conceptually define:

- Identity
- Label
- Optional icon
- Category or grouping
- Execution behavior
- Availability conditions
- Visibility conditions
- Optional argument contract
- Optional keyboard shortcut

### 17.3 Host Commands and Tool Commands

The architecture should distinguish between:

#### Host Commands

Workbench-wide actions such as:

- Open explorer
- Close tab
- Focus next panel
- Show command palette
- Toggle layout regions

#### Tool Commands

Actions exposed by a tool, such as:

- Refresh data
- Connect target
- Publish artifact
- Run task
- Validate configuration

A single command infrastructure may support both, but the conceptual distinction should remain clear.

### 17.4 Command Scope

A useful future refinement is to treat commands as operating in scope, such as:

- Global scope
- Tool scope
- ToolInstance scope
- Selection or context scope

---

## 18. Command Context

Commands should execute against a **CommandContext** or equivalent runtime context.

This may include:

- Active tool instance
- Current explorer item
- Current selection
- Current region
- Active view surface
- Arbitrary context key-values

This lets the same command behave sensibly in different interaction situations without needing duplicate command definitions.

---

## 19. Menu Model

The menu bar should be treated as a contribution surface owned by the workbench.

Tools do not directly own the menu bar. They contribute menu entries that the workbench composes.

### 19.1 MenuContribution

A menu contribution should conceptually specify:

- Target menu or menu path
- Referenced command
- Grouping information
- Order
- Visibility rule

### 19.2 Supported Menu Surfaces

The menu model should support at least:

- Main menu bar
- Explorer item context menus
- Tab context menus
- View or tool-specific menus
- Command palette groupings

### 19.3 Static and Runtime Menus

The architecture should allow both:

- **Static menu contributions**, declared by tools at registration time
- **Runtime menu contributions**, surfaced by the active ToolInstance when appropriate

This allows a complex hosted tool to feel native to the workbench when active.

---

## 20. Toolbar Model

Toolbars are contextual action surfaces attached to specific parts of the shell.

### 20.1 ToolbarContribution

A toolbar contribution may target surfaces such as:

- Explorer header
- Explorer section header
- Active tool chrome
- Tab header
- Global shell toolbar

### 20.2 Toolbar Purpose

Toolbars provide immediate access to actions relevant to the current context. They should usually reference commands rather than define raw behavior directly.

### 20.3 Runtime Tool Participation

The active ToolInstance may expose contextual toolbar actions while it is focused. This allows tools with rich business logic to participate naturally in the shell without taking over it.

---

## 21. Status Bar Model

The status bar is a lightweight surface for persistent low-noise integration.

### 21.1 Status Contributions May Include

- Current environment or profile
- Connection state
- Task count
- Background activity
- Validation state
- Tool-specific indicators

### 21.2 Status Principle

Status bar items should be concise, contextual, and composable. They should complement the shell rather than compete with the main UI.

---

## 22. Context Model

The workbench requires a shared **ContextService** or equivalent system for contextual enablement, visibility, and interaction.

This is one of the most important architectural facilities.

### 22.1 Why Context Matters

Without a context model, every command, menu, and toolbar must directly inspect the current shell state, which leads to tight coupling and complexity.

With a context model, contributions can declare rules such as:

- Show only when the active selection is a connection
- Enable only when at least one item is selected
- Show only when a particular tool is active
- Show only when the active tool instance is dirty

### 22.2 Example Context Keys

Examples may include:

- active explorer
- active tool
- active region
- selection type
- selection count
- can save
- has dirty tools
- tool-specific flags

The exact key format is implementation-specific and outside the scope of this document.

### 22.3 Context Scope

Context may be global or instance-specific. The model should allow the active ToolInstance to publish contextual information that affects command and menu behavior.

---

## 23. Selection Model

Selection is important enough to treat as an explicit concept.

### 23.1 SelectionService Responsibilities

A SelectionService may track:

- The active selection source
- The selected item or items
- The semantic type of the selection
- The currently focused region

### 23.2 Why Selection Matters

Selection frequently drives:

- Contextual menus
- Inspector or details panes
- Toolbar visibility
- Command availability
- Inter-tool communication

The explorer is often a primary source of selection, but not the only one.

---

## 24. ToolContext: The Host–Tool Runtime Contract

Because tools may contain substantial authored UI and business logic, the architecture should provide a bounded runtime contract between the workbench and the hosted tool.

This runtime contract is referred to here as **ToolContext**.

### 24.1 Purpose of ToolContext

ToolContext allows a hosted tool instance to interact with the workbench without directly depending on the shell internals.

### 24.2 ToolContext May Expose Capabilities Such As

- Open or focus another tool
- Request activation of the current instance
- Request close
- Update title, icon, or badge for the current tool instance
- Invoke commands
- Publish or query selection
- Publish context values
- Send or receive messages
- Show notifications
- Open dialogs
- Persist or restore state
- Access settings
- Participate in tasks or progress reporting
- Resolve approved shared services

### 24.3 Principle of Bounded Access

ToolContext should expose only the capabilities the platform intentionally makes available. It should not be a back door into unrestricted shell internals.

---

## 25. Runtime Contributions from ToolInstance

Some integrations should exist only while a particular ToolInstance is active.

### 25.1 Static vs Runtime Contributions

The architecture should distinguish between:

#### Static Contributions

Declared by the Tool during registration:

- Commands
- Explorer definitions
- Menu entries
- Toolbar entries
- Status items
- Capabilities

#### Runtime Contributions

Exposed by a running ToolInstance while active:

- Dynamic title/icon/badge
- Contextual toolbar actions
- Contextual menu entries
- Status text or indicators
- Dirty state
- Busy state
- Current selection and context

### 25.2 Why Runtime Contributions Matter

This distinction allows a hosted tool to feel first-class and context-aware without forcing the shell to model the tool’s internal UI structure.

---

## 26. Messaging and Eventing

The workbench includes a messaging or eventing mechanism, such as a Messenger implementation.

### 26.1 Appropriate Uses

Messaging is well-suited to:

- Broadcast notifications of state changes
- Refresh requests
- Task completion events
- Selection changed notifications
- Cross-tool events where loose coupling is desirable

### 26.2 Inappropriate Uses

Messaging should not become the primary mechanism for:

- Shell composition
- Opening tools
- Direct shell control
- Core command execution
- State persistence

### 26.3 Principle

Use:

- **Services** for capabilities
- **Commands** for user intent
- **Messaging** for broadcast events

This principle keeps the architecture understandable and maintainable.

---

## 27. Shared Workbench Services

The workbench should expose a set of shared services used by tools and by the shell itself.

### 27.1 ShellService

Responsible for shell-level operations such as:

- Opening tools
- Focusing tools
- Closing tools
- Moving or placing hosted instances
- Managing layout or region activation

### 27.2 ExplorerService

Responsible for:

- Explorer registration and composition
- Section and item refresh behavior
- Explorer state tracking
- Expansion and collapse management

### 27.3 CommandService

Responsible for:

- Registering commands
- Resolving visibility and availability
- Executing commands
- Supporting command palette style surfaces

### 27.4 MenuService

Responsible for:

- Building menus from contributions
- Applying grouping and ordering
- Evaluating visibility conditions
- Producing context menus for targets

### 27.5 ContextService

Responsible for contextual state used by commands, menus, and toolbars.

### 27.6 SelectionService

Responsible for current selection tracking and publication.

### 27.7 NotificationService

Responsible for:

- Informational notifications
- Success, warning, and error messages
- Toasts or transient UI messaging
- Progress-related feedback

### 27.8 DialogService

Responsible for:

- Confirmations
- Modal prompts
- Pickers
- Dialog-hosted workbench interactions

### 27.9 PersistenceService

Responsible for:

- Layout persistence
- Open tool restoration
- Tool state persistence
- Explorer state persistence
- User or workspace UI state

### 27.10 SettingsService

Responsible for:

- User settings
- Workspace settings
- Tool-specific settings
- Settings metadata or schema

### 27.11 TaskService

Responsible for long-running operations, including:

- Task tracking
- Progress reporting
- Cancellation support
- Surfacing activity in the shell

---

## 28. Capabilities Model

Not every tool should be assumed to participate in every workbench facility.

A Tool may declare **capabilities** describing which shell behaviors it supports.

### 28.1 Example Capability Areas

A tool might support:

- Single-instance hosting
- Multi-instance hosting
- State restoration
- Dirty tracking
- Context publication
- Selection publication
- Runtime toolbar contributions
- Runtime menu contributions
- Status participation
- Search participation
- Drag-and-drop target handling

### 28.2 Why Capabilities Matter

Capabilities keep the model flexible. The workbench can adapt its behavior according to what a tool supports, rather than assuming every tool behaves like an editor or every tool behaves like a passive panel.

---

## 29. Lifecycle Model

The architecture should support a clear lifecycle for tools.

### 29.1 Discovery

The workbench scans assemblies and discovers available Tool definitions.

### 29.2 Registration

A discovered Tool registers metadata and static contributions with the workbench.

### 29.3 Activation

A Tool may activate:

- At startup
- On first open
- On first command use
- When its explorer becomes relevant
- When a required service or contribution is first used

### 29.4 Opening

A user action or shell action resolves to opening a Tool, optionally with input.

### 29.5 Instantiation

The workbench creates a ToolInstance and hosts its root content in the shell.

### 29.6 Runtime Participation

While active, the ToolInstance may:

- Update tab state
- Publish context
- Publish selection
- Contribute runtime actions
- Participate in task and status reporting

### 29.7 Persistence and Restoration

The workbench may persist layout and tool state for later restoration.

### 29.8 Closing

The ToolInstance may save state, warn on unsaved changes, or otherwise participate in closure behavior.

---

## 30. Static Registration Flow

A typical registration flow is conceptually as follows.

1. The Workbench starts
2. ToolManager discovers Tool definitions from assemblies
3. Each Tool registers its static metadata and contributions
4. Contributions are stored in a ToolRegistry or ContributionCatalog
5. The shell composes explorers, commands, menus, and other surfaces from registered contributions
6. Tools remain dormant or inactive until needed, unless eager activation is required

This flow ensures the shell can be composed declaratively without forcing all tools to instantiate heavy runtime state immediately.

---

## 31. Runtime Interaction Flow

A typical runtime interaction flow is conceptually as follows.

1. The user interacts with an explorer item, menu item, toolbar button, command palette entry, or other shell surface
2. The interaction resolves to a command or an activation target
3. The workbench determines whether a ToolInstance should be created, re-used, or focused
4. The shell hosts the ToolInstance
5. The hosted ToolInstance receives a ToolContext
6. The ToolInstance renders its own UI and business workflow internally
7. While active, the ToolInstance publishes selection, context, and runtime contributions as needed
8. The workbench updates menus, toolbars, status, and shell presentation accordingly

This flow captures the intended relationship between host and tool.

---

## 32. Relationship Between Tool and Internal UI

A Tool may contain a complex internal UI model that is not represented one-for-one in the workbench model.

### 32.1 Important Principle

The workbench should not attempt to model all internal screens, local navigation structures, or internal flows of a tool.

The workbench only needs to know:

- How to host the ToolInstance
- How to interact with it through ToolContext and runtime contributions
- Whether the instance is active, dirty, busy, or restorable
- What shell-visible actions or state the tool publishes

### 32.2 Internal UI Ownership

Inside the hosted surface, the tool author may implement:

- Nested navigation
- Split panes
- Wizards
- Dashboards
- Grids
- Editors
- Inspectors
- Domain-specific panels
- Any other UI appropriate to the tool

This internal structure belongs to the tool, not the workbench.

---

## 33. Why “View” Is Not the Main Architectural Concept

The term “view” is not sufficient as the main architectural concept because it suggests a lightweight rendered surface rather than a substantial hosted application.

In this model:

- The workbench hosts **ToolInstances**
- A ToolInstance renders content using one or more UI components
- A component is an implementation detail of a tool’s UI

This avoids collapsing rich hosted applications into an overly simplistic “view” concept.

---

## 34. Why “Widget” Is Not Preferred Terminology

The term “widget” is too vague for the model described here.

In particular, it blurs distinctions between:

- Explorer items
- Toolbar buttons
- Hosted tools
- Internal tool UI elements
- Shell-native panels

More precise terminology should be used instead:

- Explorer
- ExplorerSection
- ExplorerItem
- Tool
- ToolInstance
- MenuContribution
- ToolbarContribution
- StatusBarContribution

Precision of vocabulary is important because this document is intended to serve as a basis for a formal specification.

---

## 35. Principles for Formal Specification

The future formal specification derived from this document should preserve the following principles.

### 35.1 Tool-Centric Hosting

The system hosts Tools, not arbitrary unmanaged UI fragments.

### 35.2 Declarative Contribution Model

Tools declare contributions. The workbench composes them.

### 35.3 Bounded Runtime Contract

Tools interact with the workbench through ToolContext and approved services rather than direct shell control.

### 35.4 Command-Centric Actions

Commands are the primary action abstraction across shell surfaces.

### 35.5 Context-Driven Visibility and Enablement

Menus, toolbars, and commands should be governed by shared context rather than hard-coded shell inspection.

### 35.6 Separation of Shell and Tool Concerns

The shell owns layout and composition. The tool owns internal UI and domain logic.

### 35.7 Static and Runtime Contributions

The model should support both registration-time and instance-time contributions.

### 35.8 Precision of Terminology

The specification should define terms clearly and avoid ambiguous language.

---

## 36. Recommended Initial Concept Set

The following concepts are recommended as the initial conceptual set for specification work.

### 36.1 Core Host Concepts

- Workbench
- WorkbenchShell
- Tool
- ToolInstance
- ToolManager
- ToolRegistry or ContributionCatalog

### 36.2 Navigation Concepts

- Explorer
- ExplorerSection
- ExplorerItem
- ExplorerItemProvider
- ActivationTarget

### 36.3 Contribution Concepts

- ExplorerContribution
- ExplorerSectionContribution
- CommandContribution
- MenuContribution
- ToolbarContribution
- StatusBarContribution
- SettingsContribution

### 36.4 Runtime Interaction Concepts

- ToolContext
- CommandContext
- ContextService
- SelectionService
- Messenger/EventBus

### 36.5 Shared Service Concepts

- ShellService
- ExplorerService
- CommandService
- MenuService
- NotificationService
- DialogService
- PersistenceService
- SettingsService
- TaskService

This set is sufficient to define a strong first specification without overcommitting to unnecessary detail.

---

## 37. Suggested Next Steps

The next stage of specification work should define, in a more formal and structured way:

- The responsibilities of each named concept
- The lifecycle rules for Tool and ToolInstance
- The composition rules for each contribution type
- The semantics of activation and opening
- The behavior of context evaluation
- The shell rules for singleton vs multi-instance tools
- The persistence and restoration model
- The distinction between static and runtime contributions
- The interaction boundaries exposed through ToolContext

A later stage may then define API contracts and implementation guidance.

---

## 38. Final Summary

This model defines a **tool-hosting workbench architecture** in which:

- **Tools** are the primary plugin and hosted application unit
- **ToolInstances** are the runtime hosted copies of tools inside the shell
- The **Workbench** owns shell composition, layout, and shared services
- **Explorers** are navigation, context, and action surfaces rather than simple launchers
- **Commands** are the central action abstraction across all shell surfaces
- **Menus, toolbars, and status** are contribution surfaces owned by the workbench
- **ToolContext** provides the bounded runtime contract between tool and host
- **Context and selection** drive dynamic behavior across the shell
- The architecture supports both lightweight utilities and substantial user-authored applications with independent business logic

As a basis for formal specification, the most important architectural sentence is this:

> A Tool is a reflection-discovered, independently authored utility application that the Workbench hosts and integrates through explorer, command, menu, toolbar, status, and runtime context contracts.

---

## 39. Glossary

### Workbench
The host platform that discovers tools, composes shell surfaces, and provides shared services.

### WorkbenchShell
The runtime shell model that manages layout, tabs, regions, and visible hosted content.

### Tool
The primary plugin and authored application unit discovered from an assembly.

### ToolInstance
A running hosted instance of a Tool within the shell.

### ToolContext
The bounded runtime contract through which a ToolInstance interacts with the workbench.

### Explorer
A sidebar-hosted navigation and action surface.

### ExplorerSection
A collapsible region within an Explorer.

### ExplorerItem
A rendered navigable or actionable item within an ExplorerSection.

### ExplorerItemProvider
A provider responsible for supplying explorer items dynamically.

### ActivationTarget
A declarative description of what should occur when an explorer item is activated.

### Command
A semantic action that may be invoked from multiple shell surfaces.

### CommandContext
The runtime context in which a command executes.

### MenuContribution
A contribution that places a command into a menu surface.

### ToolbarContribution
A contribution that places a command into a toolbar surface.

### StatusBarContribution
A contribution that places a lightweight status item into the status bar.

### ContextService
A service that exposes contextual state used for enablement, visibility, and interaction.

### SelectionService
A service that tracks current selection and selection source.

### ContributionCatalog / ToolRegistry
A registry of tools and/or their declared contributions.

---

