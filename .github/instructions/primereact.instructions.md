# PrimeReact Guide for Copilot / AI Coding Agents

This document is intended to be given to an AI coding agent working in a React codebase that uses **PrimeReact**.

It is optimized for agents that need:
- a clear mental model of PrimeReact,
- a reliable order for reading documentation,
- strong defaults for component selection,
- warnings about common mistakes,
- curated official links.

## 1) What PrimeReact is

PrimeReact is an open-source React UI component suite published by PrimeTek and licensed under **MIT**. It provides a broad set of production-oriented components including form controls, overlays, panels, navigation, data display, and a feature-rich `DataTable`.

Official starting points:
- Homepage: https://primereact.org/
- GitHub repository: https://github.com/primefaces/primereact
- License: https://github.com/primefaces/primereact/blob/master/LICENSE.md
- Installation: https://primereact.org/installation/
- Components index: https://primereact.org/#/ (homepage/showcase) and https://v11.primereact.org/components

## 2) Core mental model

PrimeReact has two broad styling approaches:

### Styled mode
Use PrimeReact’s built-in themes and component styles.

Read first:
- Installation: https://primereact.org/installation/
- Theming: https://primereact.org/theming/
- CSS Layer guide: https://primereact.org/guides/csslayer/

### Unstyled mode
Use PrimeReact for behavior, accessibility, state wiring, and internal structure, but provide your own styling.

Read first:
- Unstyled mode: https://primereact.org/unstyled/
- Pass Through API: https://primereact.org/passthrough/
- Configuration: https://primereact.org/configuration/

## 3) Preferred defaults for agents

Unless the host application already uses a stock PrimeReact theme, prefer the following working assumptions:

1. **Treat PrimeReact as a component engine, not necessarily a full design system.**
2. **Prefer controlled components** (`value`, `onChange`) for all forms.
3. **Prefer unstyled mode** when integrating into an existing host shell, design system, IDE shell, or strongly branded application.
4. **Use styled mode** only when the project explicitly wants PrimeReact’s theme stack.
5. **Check overlay behavior early** for dropdowns, dialogs, autocomplete, calendars, menus, and popups.
6. **Keep DataTable feature combinations conservative at first**. Add advanced features incrementally.
7. **Prefer accessibility-preserving customization** through documented props, templates, and Pass Through rather than DOM hacks.

## 4) Agent operating instructions

When generating PrimeReact code, follow these rules.

### 4.1 Imports
Use direct component imports following the docs pattern, for example:
- `import { Button } from 'primereact/button';`
- `import { InputText } from 'primereact/inputtext';`
- `import { DataTable } from 'primereact/datatable';`
- `import { Column } from 'primereact/column';`

Do not invent import paths. Confirm component-specific paths from the official component page.

### 4.2 Form components
Assume PrimeReact form fields are generally controlled.

Typical pattern:
- bind `value`
- update via `onChange`
- use semantic labels and IDs
- use `invalid` or documented invalid styling for validation state
- keep business validation outside the PrimeReact component unless the component explicitly supports the required behavior

Useful official docs:
- InputText: https://primereact.org/inputtext/
- Dropdown: https://primereact.org/dropdown/
- AutoComplete: https://primereact.org/autocomplete/
- Calendar / Date Picker: https://primereact.org/calendar/

### 4.3 Layout and form-like composition
PrimeReact includes container/panel/workflow components but is not primarily a layout engine. For constrained form-like layouts, use a combination of:
- PrimeReact containers such as `Panel`, `Fieldset`, `Tabs`, `Stepper`, `Splitter`, `Divider`
- normal CSS layout (`display: grid`, `display: flex`)
- optional utility frameworks already used by the host project

Useful references:
- Panel: https://primereact.org/panel/
- Components index (for Splitter, Tabs, Stepper, etc.): https://v11.primereact.org/components
- Theming / architecture: https://primereact.org/theming/

### 4.4 DataTable
Use `DataTable` when you need a rich grid-like table. Start simple and add features one by one.

Recommended rollout order:
1. basic rows and columns,
2. sorting,
3. filtering,
4. pagination,
5. row selection,
6. templates and custom cells,
7. lazy loading,
8. virtualization,
9. frozen columns / row grouping / editing only if needed.

Primary reference:
- DataTable: https://primereact.org/datatable/

### 4.5 Overlays
PrimeReact overlays often use portals and, by default, popups are appended to `document.body`. In embedded applications, docked shells, modals, shadow roots, split panes, or custom scrolling containers, this can affect clipping, z-index, and focus behavior.

Read before generating overlay-heavy UI:
- Configuration (`appendTo`, `styleContainer`, global config): https://primereact.org/configuration/

When issues occur, evaluate:
- whether the popup should use `appendTo: 'self'`
- whether a custom DOM container is needed
- whether z-index configuration needs adjustment
- whether scroll containers or transformed parents are interfering

## 5) Best-practice implementation patterns

### Pattern A: Existing design system or host shell
Use **unstyled mode**.

Why:
- avoids CSS collisions,
- makes it easier to match existing design tokens,
- works better in embedded shells,
- reduces the risk of fighting the host application’s global styles.

Read:
- https://primereact.org/unstyled/
- https://primereact.org/passthrough/

### Pattern B: Rapid CRUD/admin UI
Use **styled mode + DataTable + standard form controls**.

Why:
- fastest path to production,
- strong out-of-the-box coverage,
- less design work up front.

Read:
- https://primereact.org/installation/
- https://primereact.org/theming/
- https://primereact.org/datatable/

### Pattern C: Complex host container (IDE, portal, nested shell)
Use **unstyled mode**, prefer conservative overlays, and validate popup mounting behavior immediately.

Read:
- https://primereact.org/configuration/
- https://primereact.org/unstyled/

## 6) Common pitfalls to avoid

### Pitfall 1: Treating PrimeReact like plain HTML
PrimeReact components often expose a documented API for value management, templates, accessibility, and customization. Do not bypass this with DOM mutation or selector-driven hacks.

### Pitfall 2: Forgetting required CSS setup in styled mode
In styled mode, the install docs show required theme/core setup. Do not generate styled-mode examples without the necessary imports.

Read:
- https://primereact.org/installation/

### Pitfall 3: Ignoring Pass Through in unstyled mode
If the project uses unstyled mode, do not assume CSS class names alone are the best integration point. PrimeReact’s Pass Through API exists specifically to target component internals in a supported way.

Read:
- https://primereact.org/passthrough/

### Pitfall 4: Assuming overlay defaults are always safe
Portaled overlays can be wrong for embedded/docked UI. Check `appendTo`, z-index, and style container settings.

Read:
- https://primereact.org/configuration/

### Pitfall 5: Overloading DataTable on day one
Do not combine every advanced DataTable feature at once. Start with a minimal, correct table and expand only when needed.

Read:
- https://primereact.org/datatable/

### Pitfall 6: Mixing unrelated CSS systems carelessly
If styled mode is used alongside Tailwind, Bootstrap, host shell CSS, or a design system, read the CSS Layer guidance first.

Read:
- https://primereact.org/guides/csslayer/
- Bootstrap integration example: https://primereact.org/bootstrap/

### Pitfall 7: Weak accessibility assumptions
PrimeReact has accessibility guidance and component-specific accessibility notes. Use labels, IDs, aria props, and keyboard-safe patterns.

Read:
- Accessibility guide: https://primereact.org/guides/accessibility/

## 7) Curated reading order for an agent

### Minimum viable reading list
1. Installation
   - https://primereact.org/installation/
2. Components index / official docs homepage
   - https://primereact.org/
   - https://v11.primereact.org/components
3. DataTable
   - https://primereact.org/datatable/
4. Configuration
   - https://primereact.org/configuration/
5. Theming OR Unstyled depending on project mode
   - Styled: https://primereact.org/theming/
   - Unstyled: https://primereact.org/unstyled/
6. Pass Through API
   - https://primereact.org/passthrough/
7. Accessibility
   - https://primereact.org/guides/accessibility/

### For form-heavy work
Read these next:
- InputText: https://primereact.org/inputtext/
- Dropdown: https://primereact.org/dropdown/
- AutoComplete: https://primereact.org/autocomplete/
- Calendar: https://primereact.org/calendar/

### For shell/container/workbench UIs
Read these next:
- Configuration: https://primereact.org/configuration/
- Unstyled mode: https://primereact.org/unstyled/
- Pass Through: https://primereact.org/passthrough/
- CSS Layer: https://primereact.org/guides/csslayer/

## 8) Component-selection guidance

Use this section as a quick lookup for generation tasks.

### Text input
- Prefer `InputText` for standard text entry.
- Use semantic labels and helper text.
- Keep field state controlled.

Docs:
- https://primereact.org/inputtext/

### Selection from options
- Prefer `Dropdown` for single selection from a moderate list.
- Use `AutoComplete` when the list is large or remote-filtered.

Docs:
- https://primereact.org/dropdown/
- https://primereact.org/autocomplete/

### Date selection
- Use `Calendar` / date picker.

Docs:
- https://primereact.org/calendar/

### Constrained sections and cards
- Use `Panel`, `Fieldset`, `Divider`, `Tabs`, `Stepper`, `Splitter` depending on interaction needs.

Docs:
- Panel: https://primereact.org/panel/
- Components index: https://v11.primereact.org/components

### Data grid
- Use `DataTable` for most tabular business UI.
- Use lazy/virtual features only after correctness is established.

Docs:
- https://primereact.org/datatable/

### List/grid content view
- Consider `DataView` when you want list-vs-grid rendering rather than a true tabular layout.

Docs:
- https://primereact.org/dataview/

## 9) Instructions specifically for Copilot-style agents

When asked to generate PrimeReact code:

1. Determine whether the project is using **styled** or **unstyled** mode.
2. If unknown and the project has an existing design system or shell, default to **unstyled mode**.
3. Use the official documented import path for each component.
4. Keep all form fields controlled.
5. Prefer composition over custom wrappers unless the project already has wrapper components.
6. For overlays, explicitly consider popup mounting (`appendTo`).
7. For `DataTable`, generate a minimal correct version first.
8. Avoid private DOM assumptions.
9. Preserve accessibility with labels, `htmlFor`, IDs, and documented aria props.
10. Do not invent theme tokens, CSS variables, or class names that the project has not defined.

## 10) Suggested prompt block for an agent

You can give the following block to a coding agent:

```md
You are working in a React project that uses PrimeReact.

Rules:
- Prefer official PrimeReact APIs and documented import paths.
- Use controlled form components.
- If the host app has its own design system or shell, prefer PrimeReact unstyled mode patterns.
- Use Pass Through for supported internal customization instead of DOM hacks.
- For overlays, consider PrimeReact configuration such as appendTo and z-index behavior.
- For DataTable, start with a minimal implementation and add advanced features incrementally.
- Preserve accessibility with labels, ids, aria props, and keyboard-safe patterns.
- Before implementing a component, consult the relevant official PrimeReact page from the curated links list.

Curated links:
- Installation: https://primereact.org/installation/
- Theming: https://primereact.org/theming/
- Unstyled: https://primereact.org/unstyled/
- Pass Through: https://primereact.org/passthrough/
- Configuration: https://primereact.org/configuration/
- Accessibility: https://primereact.org/guides/accessibility/
- Components index: https://v11.primereact.org/components
- InputText: https://primereact.org/inputtext/
- Dropdown: https://primereact.org/dropdown/
- AutoComplete: https://primereact.org/autocomplete/
- Calendar: https://primereact.org/calendar/
- Panel: https://primereact.org/panel/
- DataTable: https://primereact.org/datatable/
- DataView: https://primereact.org/dataview/
- CSS Layer: https://primereact.org/guides/csslayer/
```

## 11) Final recommendation

For most embedded, branded, or shell-based applications, the safest PrimeReact strategy is:
- **PrimeReact in unstyled mode**,
- **Pass Through for supported internals customization**,
- **host-app CSS/tokens for styling**,
- **careful overlay configuration**,
- **incremental adoption of DataTable features**.

For fast business UI and internal tools, the fastest strategy is:
- **styled mode**,
- **standard PrimeReact theme**,
- **DataTable + core form controls first**.

