const stylesheetHref = "/_content/UKHO.Workbench/workbench-grid-splitter.css";
const stylesheetId = "ukho-workbench-grid-splitter-stylesheet";
const columnDraggingClass = "ukho-workbench-grid-splitter-dragging-column";
const rowDraggingClass = "ukho-workbench-grid-splitter-dragging-row";
const instances = new WeakMap();

export function initializeSplitters(element, dotNetReference)
{
    // The stylesheet is Workbench-owned and auto-loaded so consumers do not need manual host-page includes.
    ensureStylesheet();

    if (!element || instances.has(element))
    {
        return;
    }

    const instance =
    {
        cleanupActions: [],
        dragState: null,
        pendingFrame: 0
    };

    registerSplitters(element, dotNetReference, instance, "column", startColumnDrag);
    registerSplitters(element, dotNetReference, instance, "row", startRowDrag);
    instances.set(element, instance);
}

export function disposeSplitters(element)
{
    if (!element)
    {
        return;
    }

    const instance = instances.get(element);
    if (!instance)
    {
        return;
    }

    stopActiveDrag(instance);
    instance.cleanupActions.forEach((cleanupAction) => cleanupAction());
    instance.cleanupActions = [];
    instances.delete(element);
}

function ensureStylesheet()
{
    if (document.getElementById(stylesheetId))
    {
        return;
    }

    const link = document.createElement("link");
    link.id = stylesheetId;
    link.rel = "stylesheet";
    link.href = stylesheetHref;
    document.head.appendChild(link);
}

function registerSplitters(element, dotNetReference, instance, direction, startDragHandler)
{
    // Each grid instance only owns its direct gutter children so nested splitter-enabled grids do not double-register inner gutters.
    const gutters = Array.from(element.children)
        .filter((child) => child.dataset.ukhoWorkbenchSplitter === direction);

    // Each authored splitter gutter gets its own drag start handler, but cleanup stays centralized per grid instance.
    gutters.forEach((gutter) =>
    {
        const pointerDownHandler = (event) => startDragHandler(event, element, gutter, dotNetReference, instance);
        gutter.addEventListener("mousedown", pointerDownHandler);
        instance.cleanupActions.push(() => gutter.removeEventListener("mousedown", pointerDownHandler));
    });
}

function startColumnDrag(event, element, gutter, dotNetReference, instance)
{
    // Column dragging works by mutating the two adjacent CSS Grid column tracks in pixel space during the active drag.
    event.preventDefault();

    const sizes = parseTemplateSizes(getComputedStyle(element).gridTemplateColumns);
    const splitterTrackIndex = Number(gutter.dataset.splitterTrackIndex);
    const previousTrackIndex = Number(gutter.dataset.previousTrackIndex);
    const nextTrackIndex = Number(gutter.dataset.nextTrackIndex);
    const previousSize = readPixelSize(sizes[previousTrackIndex - 1]);
    const nextSize = readPixelSize(sizes[nextTrackIndex - 1]);

    if (!Number.isFinite(previousSize) || !Number.isFinite(nextSize))
    {
        return;
    }

    stopActiveDrag(instance);
    document.body.classList.add(columnDraggingClass);

    instance.dragState =
    {
        direction: "column",
        dotNetReference,
        element,
        latestPointerPosition: event.clientX,
        nextSize,
        nextTrackIndex,
        previousSize,
        previousTrackIndex,
        sizes,
        splitterTrackIndex,
        startPointerPosition: event.clientX
    };

    const pointerMoveHandler = (moveEvent) => queueResize(moveEvent, instance);
    const pointerUpHandler = () => stopActiveDrag(instance);

    window.addEventListener("mousemove", pointerMoveHandler);
    window.addEventListener("mouseup", pointerUpHandler);

    instance.dragState.pointerMoveHandler = pointerMoveHandler;
    instance.dragState.pointerUpHandler = pointerUpHandler;
}

function startRowDrag(event, element, gutter, dotNetReference, instance)
{
    // Row dragging mirrors the column path but targets grid-template-rows and the pointer Y axis instead.
    event.preventDefault();

    const sizes = parseTemplateSizes(getComputedStyle(element).gridTemplateRows);
    const splitterTrackIndex = Number(gutter.dataset.splitterTrackIndex);
    const previousTrackIndex = Number(gutter.dataset.previousTrackIndex);
    const nextTrackIndex = Number(gutter.dataset.nextTrackIndex);
    const previousSize = readPixelSize(sizes[previousTrackIndex - 1]);
    const nextSize = readPixelSize(sizes[nextTrackIndex - 1]);

    if (!Number.isFinite(previousSize) || !Number.isFinite(nextSize))
    {
        return;
    }

    stopActiveDrag(instance);
    document.body.classList.add(rowDraggingClass);

    instance.dragState =
    {
        direction: "row",
        dotNetReference,
        element,
        latestPointerPosition: event.clientY,
        nextSize,
        nextTrackIndex,
        previousSize,
        previousTrackIndex,
        sizes,
        splitterTrackIndex,
        startPointerPosition: event.clientY
    };

    const pointerMoveHandler = (moveEvent) => queueResize(moveEvent, instance);
    const pointerUpHandler = () => stopActiveDrag(instance);

    window.addEventListener("mousemove", pointerMoveHandler);
    window.addEventListener("mouseup", pointerUpHandler);

    instance.dragState.pointerMoveHandler = pointerMoveHandler;
    instance.dragState.pointerUpHandler = pointerUpHandler;
}

function queueResize(event, instance)
{
    const dragState = instance.dragState;
    if (!dragState)
    {
        return;
    }

    // High-frequency mousemove events are coalesced behind requestAnimationFrame so resize notifications stay responsive without flooding the UI.
    dragState.latestPointerPosition = dragState.direction === "column" ? event.clientX : event.clientY;

    if (instance.pendingFrame !== 0)
    {
        return;
    }

    instance.pendingFrame = window.requestAnimationFrame(() =>
    {
        instance.pendingFrame = 0;
        applyResize(instance);
    });
}

function applyResize(instance)
{
    const dragState = instance.dragState;
    if (!dragState)
    {
        return;
    }

    // The splitter itself keeps its fixed authored thickness; only the immediately adjacent content tracks are resized.
    // Clamp the drag delta so the two adjacent tracks always keep the same combined size and the grid cannot grow beyond its original bounds.
    const rawDelta = dragState.latestPointerPosition - dragState.startPointerPosition;
    const minimumTrackSize = 1;
    const minimumDelta = minimumTrackSize - dragState.previousSize;
    const maximumDelta = dragState.nextSize - minimumTrackSize;
    const clampedDelta = Math.min(Math.max(rawDelta, minimumDelta), maximumDelta);
    const previousSize = dragState.previousSize + clampedDelta;
    const nextSize = dragState.nextSize - clampedDelta;
    dragState.sizes[dragState.previousTrackIndex - 1] = `${previousSize}px`;
    dragState.sizes[dragState.nextTrackIndex - 1] = `${nextSize}px`;

    if (dragState.direction === "column")
    {
        dragState.element.style.gridTemplateColumns = dragState.sizes.join(" ");
        void dragState.dotNetReference.invokeMethodAsync(
            "NotifyColumnResize",
            dragState.splitterTrackIndex,
            dragState.previousTrackIndex,
            dragState.nextTrackIndex,
            previousSize,
            nextSize,
            dragState.element.style.gridTemplateColumns);
        return;
    }

    dragState.element.style.gridTemplateRows = dragState.sizes.join(" ");
    void dragState.dotNetReference.invokeMethodAsync(
        "NotifyRowResize",
        dragState.splitterTrackIndex,
        dragState.previousTrackIndex,
        dragState.nextTrackIndex,
        previousSize,
        nextSize,
        dragState.element.style.gridTemplateRows);
}

function stopActiveDrag(instance)
{
    // Drag cleanup must always clear the body cursor state, even when the grid or circuit disappears mid-interaction.
    if (instance.pendingFrame !== 0)
    {
        window.cancelAnimationFrame(instance.pendingFrame);
        instance.pendingFrame = 0;
    }

    const dragState = instance.dragState;
    if (!dragState)
    {
        clearDraggingClasses();
        return;
    }

    window.removeEventListener("mousemove", dragState.pointerMoveHandler);
    window.removeEventListener("mouseup", dragState.pointerUpHandler);
    instance.dragState = null;
    clearDraggingClasses();
}

function clearDraggingClasses()
{
    document.body.classList.remove(columnDraggingClass);
    document.body.classList.remove(rowDraggingClass);
}

function parseTemplateSizes(template)
{
    return template
        .split(/\s+/)
        .filter((token) => token && token.trim().length > 0);
}

function readPixelSize(value)
{
    return Number.parseFloat(value.replace("px", ""));
}
