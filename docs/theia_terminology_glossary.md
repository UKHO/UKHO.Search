# Eclipse Theia UI Terminology Glossary

This glossary uses the UI shown in the screenshot ./docs/theia-screenshot.jpg as a practical reference point. Where possible, it uses terminology that aligns with Theia and the VS Code-style workbench model that Theia follows.

## Core layout terms

### Workbench
The overall application shell that hosts editors, panels, side bars, menus, toolbars, and supporting UI regions.

### Main Menu / Menu Bar
The horizontal menu at the top of the window containing items such as **File**, **Edit**, **Selection**, **View**, **Go**, **Debug**, **Terminal**, and **Help**.

### Activity Bar
The vertical strip of icons on the far left side of the window.

This is the term for the area containing icons such as:
- Explorer
- Search
- Source Control
- Debug / Run
- Extensions or other contributed items

### Activity Bar item
A single icon inside the Activity Bar. Selecting one usually reveals or focuses a related area in the Side Bar.

In the screenshot, the circled magnifying-glass icon is the **Search** Activity Bar item.

### Side Bar
The vertical panel immediately to the right of the Activity Bar. It displays a selected view container, such as Explorer or Search.

### Secondary Side Bar
An optional second side panel on the opposite side of the window in layouts that enable it. Not always present.

## Navigation and content terms

### View container
A container shown in the Side Bar or Panel that groups one or more related views.

Examples include:
- Explorer
- Search
- Source Control

In practical specifications, this is often the best term for a major functional section opened from the Activity Bar.

### View
A specific UI component inside a view container.

Examples:
- **Outline** is a view
- **Problems** is a view
- **Output** is a view
- A file tree is often presented as a view inside Explorer

A view container may contain one view or several stacked views.

### Explorer
The view container used for file and project navigation in the Side Bar.

### Search
The view container used for workspace-wide searching.

### Outline
A view, often shown on the right, that presents the structural outline of the current file or symbol tree.

## Editor area terms

### Editor area
The main central region where files, editors, and other primary content appear.

### Editor
A content widget opened in the editor area, such as a TypeScript file, Markdown file, settings UI, or custom editor.

### Editor tab
A tab at the top of the editor area representing one open editor.

### Group / Editor group
A subdivision of the editor area that can host one or more editor tabs. Multiple groups allow split layouts.

## Bottom area terms

### Panel
The lower region of the workbench used for auxiliary views.

In the screenshot, the bottom area containing **Problems**, **Output**, and **Debug Console** is the Panel.

### Panel view
A view shown within the Panel.

Examples:
- Problems
- Output
- Debug Console
- Terminal

## Status and support terms

### Status Bar
The horizontal bar along the bottom of the window showing contextual status information, branch name, diagnostics, language mode, line/column position, and similar indicators.

### Command Palette
A searchable command launcher typically opened by keyboard shortcut. It is used to invoke commands exposed by Theia and installed extensions.

### Context menu
A menu opened by right-clicking within a specific area, such as the file tree, editor, or panel.

## Recommended wording for a specification

For the element you originally asked about, the clearest wording is:

> The application exposes major work areas through the **Activity Bar**. Each **Activity Bar item** opens or focuses a corresponding **view container** in the **Side Bar**.

For broader UI descriptions, this wording is usually clear and accurate:

> The workbench consists of a **Menu Bar**, **Activity Bar**, **Side Bar**, **editor area**, **Panel**, and **Status Bar**.

## Practical naming guidance

When writing your specification, these terms are usually the safest choices:

- Use **Activity Bar** for the far-left icon strip
- Use **Activity Bar item** for one icon in that strip
- Use **Side Bar** for the left-side panel that opens next to it
- Use **view container** for a major section such as Explorer or Search
- Use **view** for a smaller component inside a container
- Use **Panel** for the lower tabbed area
- Use **editor area** for the main central workspace

## Example sentence set

- “Navigation between major tool areas is provided through the **Activity Bar**.”
- “Selecting an **Activity Bar item** opens its related **view container** in the **Side Bar**.”
- “Primary documents open in the **editor area**.”
- “Diagnostics and console-style tools appear in the **Panel**.”
- “Contextual state and environment indicators are shown in the **Status Bar**.”
