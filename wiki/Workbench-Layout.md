# Workbench layout

This page is the developer guide for `UKHO.Workbench` layout components.

Use it as the source of truth for:

- the `Grid` authoring model
- row and column definitions
- content placement with `GridElement`
- splitter authoring with `SplitterColumnDefinition` and `SplitterRowDefinition`
- defaults, constraints, and notification behavior
- runnable examples in `LayoutSample`

## Overview

`UKHO.Workbench.Layout` provides a WPF-style grid authoring surface for Blazor.

The layout API is centered on:

- `Grid`
- `ColumnDefinition`
- `RowDefinition`
- `GridElement`
- `SplitterColumnDefinition`
- `SplitterRowDefinition`

This gives developers a familiar row/column layout model, plus optional draggable splitters for desktop-style pane resizing.

## Where it lives in this repository

- Layout components: `src/workbench/server/UKHO.Workbench/Layout/`
- Splitter assets: `src/workbench/server/UKHO.Workbench/wwwroot/`
- Canonical runnable showcase: `src/Workbench/samples/LayoutSample/Components/Pages/LayoutSplitterShowcase.razor`

`LayoutSample` is the canonical standalone runnable example for Workbench layout behaviour in this repository.

The sample is intentionally minimal:

- it runs directly as a standalone Blazor Server app
- it renders the extracted layout showcase as a single-page sample at `/`
- it contains no extra navigation menu, sidebar, header shell, or template chrome

Run the showcase locally with:

```powershell
dotnet run --project src/Workbench/samples/LayoutSample/LayoutSample.csproj
```

Then navigate to:

- `https://localhost:<port>/`

## Core concepts

### Grid

`Grid` is the container. It owns the ordered row and column track definitions, renders the CSS Grid template, and wires splitter behavior when splitter tracks are present.

Supported `Grid` parameters:

| Parameter | Description | Type | Default |
|---|---|---|---|
| `Width` | Fixed grid width in pixels | `double?` | `100%` |
| `Height` | Fixed grid height in pixels | `double?` | `100%` |
| `ColumnGap` | Gap between columns, in pixels | `double` | `0` |
| `RowGap` | Gap between rows, in pixels | `double` | `0` |
| `OnResize` | Unified splitter notification callback | `EventCallback<GridResizeNotification>` | unset |

Example:

```razor
<Grid Width="960" Height="320" ColumnGap="12" RowGap="8">
    ...
</Grid>
```

### Column and row definitions

Use `ColumnDefinition` and `RowDefinition` to declare content tracks.

Supported sizing tokens:

- `*` for a single star-sized track
- `2*`, `3*`, and so on for proportional star sizing
- `Auto` for content-sized tracks
- a fixed number such as `120` for pixels

Examples:

```razor
<ColumnDefinitions>
    <ColumnDefinition Width="240" />
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="2*" />
</ColumnDefinitions>

<RowDefinitions>
    <RowDefinition Height="96" />
    <RowDefinition Height="*" />
</RowDefinitions>
```

Definition parameters:

| Component | Parameters |
|---|---|
| `ColumnDefinition` | `Width`, `MinWidth`, `MaxWidth` |
| `RowDefinition` | `Height`, `MinHeight`, `MaxHeight` |

### GridElement

Use `GridElement` inside the `Content` fragment to place content into the authored grid tracks.

Supported parameters:

| Parameter | Description | Default |
|---|---|---|
| `Row` | 1-based start row | `1` |
| `Column` | 1-based start column | `1` |
| `RowSpan` | Ending row line | next row |
| `ColumnSpan` | Ending column line | next column |
| `HorizontalAlignment` | Horizontal placement | `Stretch` |
| `VerticalAlignment` | Vertical placement | `Stretch` |

Important:

- row and column numbering is **1-based**
- splitter tracks count as ordinary tracks for indexing
- `GridElement` content is clipped to its own cell so adjacent panes do not paint over each other during aggressive resizing

## Basic grid example

```razor
<Grid Width="600" Height="240">
    <ColumnDefinitions>
        <ColumnDefinition Width="180" />
        <ColumnDefinition Width="*" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="80" />
        <RowDefinition Height="*" />
    </RowDefinitions>

    <Content>
        <GridElement Row="1" Column="1">
            <div>Top left</div>
        </GridElement>
        <GridElement Row="1" Column="2">
            <div>Top right</div>
        </GridElement>
        <GridElement Row="2" Column="1" ColumnSpan="3">
            <div>Bottom row content</div>
        </GridElement>
    </Content>
</Grid>
```

## Splitter authoring

### SplitterColumnDefinition

Use `SplitterColumnDefinition` to insert a dedicated vertical splitter gutter between two resizable columns.

Parameters:

| Parameter | Description | Default |
|---|---|---|
| `Width` | Splitter gutter width | `4px` |

Example:

```razor
<Grid Width="960" Height="320" OnResize="HandleGridResizeAsync">
    <ColumnDefinitions>
        <ColumnDefinition Width="280" />
        <SplitterColumnDefinition />
        <ColumnDefinition Width="*" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="*" />
    </RowDefinitions>
    <Content>
        <GridElement Column="1">
            <div>Navigator</div>
        </GridElement>
        <GridElement Column="3">
            <div>Workspace</div>
        </GridElement>
    </Content>
</Grid>
```

### SplitterRowDefinition

Use `SplitterRowDefinition` to insert a dedicated horizontal splitter gutter between two resizable rows.

Parameters:

| Parameter | Description | Default |
|---|---|---|
| `Height` | Splitter gutter height | `4px` |

Example:

```razor
<Grid Width="960" Height="320" OnResize="HandleGridResizeAsync">
    <ColumnDefinitions>
        <ColumnDefinition Width="*" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="140" />
        <SplitterRowDefinition />
        <RowDefinition Height="*" />
    </RowDefinitions>
    <Content>
        <GridElement Row="1">
            <div>Inspector</div>
        </GridElement>
        <GridElement Row="3">
            <div>Console</div>
        </GridElement>
    </Content>
</Grid>
```

### Mixed row and column splitters

You can combine both splitter types in a single `Grid`.

```razor
<Grid Width="960" Height="360" OnResize="HandleGridResizeAsync">
    <ColumnDefinitions>
        <ColumnDefinition Width="280" />
        <SplitterColumnDefinition />
        <ColumnDefinition Width="*" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="120" />
        <SplitterRowDefinition />
        <RowDefinition Height="*" />
    </RowDefinitions>
    <Content>
        <GridElement Row="1" Column="1">
            <div>Explorer</div>
        </GridElement>
        <GridElement Row="1" Column="3">
            <div>Preview</div>
        </GridElement>
        <GridElement Row="3" Column="1">
            <div>Details</div>
        </GridElement>
        <GridElement Row="3" Column="3">
            <div>Canvas</div>
        </GridElement>
    </Content>
</Grid>
```

## Advanced patterns

The current layout implementation supports:

- multiple column splitters in one grid
- multiple row splitters in one grid
- mixed row and column splitters in one grid
- nested splitter-enabled grids
- layouts that combine splitters with `RowGap` and `ColumnGap`
- supported adjacent resize pairs such as `fixed + star`

### Nested splitter-enabled grids

Nested grids are supported. Each grid instance registers only its own direct gutter elements, so outer and inner splitters behave independently.

Example pattern:

```razor
<Grid Width="960" Height="420" ColumnGap="12" RowGap="10" OnResize="HandleGridResizeAsync">
    <ColumnDefinitions>
        <ColumnDefinition Width="220" />
        <SplitterColumnDefinition />
        <ColumnDefinition Width="*" />
        <SplitterColumnDefinition />
        <ColumnDefinition Width="200" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="120" />
        <SplitterRowDefinition />
        <RowDefinition Height="*" />
    </RowDefinitions>
    <Content>
        <GridElement Row="3" Column="3">
            <Grid ColumnGap="8" RowGap="8" OnResize="HandleGridResizeAsync">
                <ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <SplitterColumnDefinition />
                    <ColumnDefinition Width="*" />
                </ColumnDefinitions>
                <RowDefinitions>
                    <RowDefinition Height="96" />
                    <SplitterRowDefinition />
                    <RowDefinition Height="*" />
                </RowDefinitions>
                <Content>
                    <GridElement Row="1" Column="1">
                        <div>Nested pane A</div>
                    </GridElement>
                    <GridElement Row="1" Column="3">
                        <div>Nested pane B</div>
                    </GridElement>
                    <GridElement Row="3" Column="1">
                        <div>Nested pane C</div>
                    </GridElement>
                    <GridElement Row="3" Column="3">
                        <div>Nested pane D</div>
                    </GridElement>
                </Content>
            </Grid>
        </GridElement>
    </Content>
</Grid>
```

## Defaults and built-in behavior

### Splitter defaults

- omitted splitter `Width` or `Height` defaults to `4px`
- splitters are transparent at rest
- splitters highlight blue on hover
- column splitters use `col-resize`
- row splitters use `row-resize`

The hover color can be overridden by setting:

- `--ukho-workbench-splitter-hover-color`

### Automatic asset loading

Splitter-enabled grids automatically load the Workbench-owned splitter script and stylesheet. No manual host page includes are required.

### Gaps and splitters

`RowGap` and `ColumnGap` remain additional spacing. They do not replace splitter gutter thickness.

### Resize bounds

Splitter dragging is clamped so the two tracks adjacent to the active splitter keep a constant combined size. This prevents the overall grid from expanding beyond its original container when one side reaches its minimum effective size.

## Notifications

Use `Grid.OnResize` to receive continuous resize updates while a splitter is dragged.

```razor
<Grid OnResize="HandleGridResizeAsync">
    ...
</Grid>
```

The callback receives `GridResizeNotification`.

Reported values:

- `Direction`
- `SplitterTrackIndex`
- `PreviousTrackIndex`
- `NextTrackIndex`
- `PreviousTrackSizeInPixels`
- `NextTrackSizeInPixels`
- `GridTemplate`

Notification conventions:

- `Direction` identifies the affected axis
- all indices are **1-based**
- `PreviousTrackSizeInPixels` and `NextTrackSizeInPixels` describe the two content tracks immediately adjacent to the active splitter
- `GridTemplate` is the resolved template string for the affected axis only
  - column drags: `grid-template-columns`
  - row drags: `grid-template-rows`

Example handler:

```razor
<Grid OnResize="HandleGridResizeAsync">
    ...
</Grid>

@code {
    private Task HandleGridResizeAsync(GridResizeNotification notification)
    {
        // Inspect notification.Direction and notification.GridTemplate here.
        return Task.CompletedTask;
    }
}
```

## Constraints and fail-fast rules

The layout model intentionally rejects invalid splitter configurations.

Rules:

- a splitter must sit between two resizable content tracks
- a splitter cannot appear at the outer edge of a grid
- a splitter cannot border another splitter directly
- `Auto` rows or columns cannot be one of the two resizable tracks adjacent to a splitter
- splitter tracks are reserved gutters and cannot host normal `GridElement` content

If these rules are violated, the grid fails fast during component initialization or rendering.

## Alignment

`GridElement` supports the existing alignment values:

| Value | Horizontal behavior | Vertical behavior |
|---|---|---|
| `@Alignment.Start` | Left | Top |
| `@Alignment.Center` | Center | Center |
| `@Alignment.End` | Right | Bottom |
| `@Alignment.Stretch` | Full width | Full height |

If another CSS rule interferes with expected alignment behavior, reset the conflicting styles in local component CSS.

Example:

```razor
<style>
    label {
        margin: 0;
        padding: 0;
    }
</style>
```

## Troubleshooting

### A splitter is not draggable

Check that:

- the splitter sits between two resizable tracks
- neither adjacent track is `Auto`
- the grid has rendered with its final row and column definitions

### Content overlaps a neighboring pane during resize

The layout component clips `GridElement` content to its own cell. If a child component still appears visually wrong when shrunk, check that the child’s own CSS is not forcing overflow behavior you do not want.

### A pane becomes very small

The layout system prevents container growth during drag. Whether the shrunken pane wraps, clips, or scrolls is up to the pane content and its own CSS.

## Complete example

```razor
@using UKHO.Workbench.Layout

<Grid Width="960" Height="360" ColumnGap="12" RowGap="8" OnResize="HandleGridResizeAsync">
    <ColumnDefinitions>
        <ColumnDefinition Width="240" />
        <SplitterColumnDefinition />
        <ColumnDefinition Width="*" />
    </ColumnDefinitions>
    <RowDefinitions>
        <RowDefinition Height="120" />
        <SplitterRowDefinition />
        <RowDefinition Height="*" />
    </RowDefinitions>

    <Content>
        <GridElement Row="1" Column="1">
            <section>Top left</section>
        </GridElement>
        <GridElement Row="1" Column="3">
            <section>Top right</section>
        </GridElement>
        <GridElement Row="3" Column="1">
            <section>Bottom left</section>
        </GridElement>
        <GridElement Row="3" Column="3">
            <section>Bottom right</section>
        </GridElement>
    </Content>
</Grid>

@code {
    private Task HandleGridResizeAsync(GridResizeNotification notification)
    {
        return Task.CompletedTask;
    }
}
```
