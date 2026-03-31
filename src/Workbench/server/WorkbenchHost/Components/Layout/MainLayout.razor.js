const outputPanelStateKey = Symbol("workbenchOutputPanelState");

// Returns the scrollable viewport used by the hosted terminal, falling back to the host element during early render phases.
function getOutputViewport(outputElement) {
    // XtermBlazor renders its own nested viewport, so the shell targets that element when it becomes available.
    if (!outputElement) {
        return null;
    }

    return outputElement.querySelector(".xterm-viewport") ?? outputElement;
}

// Returns the current selected text when the active browser selection belongs to the hosted output terminal.
function getSelectedOutputText(outputElement) {
    // Terminal selection is still browser-native text selection, so the helper verifies that the active selection lives inside the output host before exposing it.
    if (!outputElement) {
        return "";
    }

    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
        return "";
    }

    const selectedText = selection.toString();
    if (!selectedText) {
        return "";
    }

    const range = selection.getRangeAt(0);
    const commonAncestor = range.commonAncestorContainer;
    const commonAncestorElement = commonAncestor.nodeType === Node.ELEMENT_NODE
        ? commonAncestor
        : commonAncestor.parentElement;

    return commonAncestorElement && outputElement.contains(commonAncestorElement)
        ? selectedText
        : "";
}

// Returns whether the hosted output terminal currently owns an active browser text selection.
function hasActiveOutputSelection(outputElement) {
    // Selection state drives the copy toolbar button, so the boolean helper simply wraps the terminal-owned selection text lookup.
    return getSelectedOutputText(outputElement).length > 0;
}

// Returns the CSS variable value for the supplied token when the current theme defines one.
function getCssVariableValue(styleSource, variableName) {
    // The helper centralizes CSS-variable reads so theme extraction can fall back cleanly when a token is not defined.
    if (!styleSource || !variableName) {
        return "";
    }

    return styleSource.getPropertyValue(variableName).trim();
}

// Converts the supplied CSS color into an rgba string with the requested alpha channel.
function withAlpha(color, alpha) {
    // The terminal theme needs a few translucent values for selection and scrollbar states, so the helper normalizes the browser-derived base colors first.
    if (!color) {
        return "";
    }

    if (color.startsWith("rgba(")) {
        return color.replace(/rgba\(([^)]+),[^)]+\)/u, `rgba($1, ${alpha})`);
    }

    if (color.startsWith("rgb(")) {
        return color.replace("rgb(", "rgba(").replace(")", `, ${alpha})`);
    }

    const hexColor = color.replace("#", "");
    if (hexColor.length !== 6) {
        return color;
    }

    const red = Number.parseInt(hexColor.slice(0, 2), 16);
    const green = Number.parseInt(hexColor.slice(2, 4), 16);
    const blue = Number.parseInt(hexColor.slice(4, 6), 16);
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

// Determines whether the supplied background color should be treated as a dark terminal background.
function isDarkColor(color) {
    // A simple luminance check keeps the fallback terminal palette aligned with whichever Radzen light or dark theme is currently active.
    const match = color.match(/\d+(?:\.\d+)?/gu);
    if (!match || match.length < 3) {
        return true;
    }

    const red = Number.parseFloat(match[0]);
    const green = Number.parseFloat(match[1]);
    const blue = Number.parseFloat(match[2]);
    const luminance = ((0.2126 * red) + (0.7152 * green) + (0.0722 * blue)) / 255;
    return luminance < 0.55;
}

// Builds the browser-derived theme payload that the Blazor layout maps into xterm.js options.
export function readOutputTerminalTheme(outputElement) {
    // Theme extraction remains browser-owned because the active Radzen appearance toggle ultimately materializes as CSS values in the DOM.
    if (!outputElement) {
        return {};
    }

    const outputStyles = getComputedStyle(outputElement);
    const documentStyles = getComputedStyle(document.documentElement);
    const background = outputStyles.backgroundColor || getCssVariableValue(documentStyles, "--rz-base-background-color") || "rgb(17, 24, 39)";
    const foreground = outputStyles.color || getCssVariableValue(documentStyles, "--rz-text-color") || "rgb(248, 250, 252)";
    const isDark = isDarkColor(background);
    const info = getCssVariableValue(documentStyles, "--rz-info") || (isDark ? "#84caff" : "#1570ef");
    const success = getCssVariableValue(documentStyles, "--rz-success") || (isDark ? "#6ce9a6" : "#039855");
    const warning = getCssVariableValue(documentStyles, "--rz-warning") || (isDark ? "#fde272" : "#ca8504");
    const danger = getCssVariableValue(documentStyles, "--rz-danger") || (isDark ? "#fda29b" : "#d92d20");

    return {
        background,
        foreground,
        cursor: foreground,
        cursorAccent: background,
        selectionBackground: withAlpha(info, isDark ? 0.3 : 0.18),
        selectionInactiveBackground: withAlpha(info, isDark ? 0.18 : 0.1),
        scrollbarSliderBackground: withAlpha(foreground, isDark ? 0.18 : 0.12),
        scrollbarSliderHoverBackground: withAlpha(foreground, isDark ? 0.28 : 0.2),
        scrollbarSliderActiveBackground: withAlpha(foreground, isDark ? 0.38 : 0.28),
        black: isDark ? "#1f2937" : "#111827",
        red: danger,
        green: success,
        yellow: warning,
        blue: info,
        magenta: isDark ? "#c084fc" : "#7c3aed",
        cyan: isDark ? "#22d3ee" : "#0891b2",
        white: isDark ? "#d0d5dd" : "#475467",
        brightBlack: isDark ? "#98a2b3" : "#667085",
        brightRed: isDark ? "#fda29b" : "#f04438",
        brightGreen: isDark ? "#6ce9a6" : "#12b76a",
        brightYellow: isDark ? "#fde272" : "#eaaa08",
        brightBlue: isDark ? "#b2ddff" : "#2e90fa",
        brightMagenta: isDark ? "#e9d5ff" : "#a855f7",
        brightCyan: isDark ? "#a5f3fc" : "#06b6d4",
        brightWhite: isDark ? "#f8fafc" : "#101828"
    };
}

// Copies supplied text to the clipboard by preferring the async Clipboard API and falling back to a temporary textarea when needed.
export async function copyTextToClipboard(text) {
    // The shell uses one clipboard helper for toolbar clicks and keyboard shortcuts so clipboard fallbacks stay centralized.
    if (!text) {
        return;
    }

    if (navigator.clipboard && typeof navigator.clipboard.writeText === "function") {
        await navigator.clipboard.writeText(text);
        return;
    }

    const temporaryTextArea = document.createElement("textarea");
    temporaryTextArea.value = text;
    temporaryTextArea.setAttribute("readonly", "readonly");
    temporaryTextArea.style.position = "fixed";
    temporaryTextArea.style.opacity = "0";
    document.body.appendChild(temporaryTextArea);
    temporaryTextArea.select();
    document.execCommand("copy");
    document.body.removeChild(temporaryTextArea);
}

// Initializes the shell-owned output panel helpers for scroll tracking.
export function initializeOutputPanel(outputElement, dotNetReference) {
    // The helper stores its listener on the element so repeated renders can safely replace the callback without duplicating handlers.
    disposeOutputPanel(outputElement);

    if (!outputElement || !dotNetReference) {
        return;
    }

    const viewportElement = getOutputViewport(outputElement);
    if (!viewportElement) {
        return;
    }

    const notifyViewportState = () => {
        // A small tolerance avoids false negatives caused by fractional browser layout values.
        const isAtEnd = viewportElement.scrollTop + viewportElement.clientHeight >= viewportElement.scrollHeight - 4;
        dotNetReference.invokeMethodAsync("NotifyOutputViewportStateAsync", isAtEnd);
    };
    const notifyThemeState = () => {
        // Theme refresh notifications stay lightweight because the .NET side will read the latest browser-derived values only when the layout re-renders.
        dotNetReference.invokeMethodAsync("NotifyOutputThemeStateChangedAsync");
    };
    const notifyHostResize = () => {
        // Host resize notifications prompt the .NET layout to re-render so the fit addon can recalculate rows and columns against the latest panel size.
        dotNetReference.invokeMethodAsync("NotifyOutputHostResizedAsync");
        notifyViewportState();
    };
    const notifySelectionState = () => {
        // Selection gestures can happen inside xterm's canvas-backed surface, so the shell asks .NET to query xterm directly instead of trusting DOM selection text alone.
        dotNetReference.invokeMethodAsync("ProbeOutputSelectionStateAsync");
    };
    const handleKeyDown = async event => {
        // Terminal keyboard shortcuts stay minimal: Ctrl+F opens the panel-local find strip and Ctrl+C copies selected text from the read-only surface.
        if (!(event.ctrlKey || event.metaKey)) {
            return;
        }

        const key = (event.key ?? "").toLowerCase();
        if (key === "f") {
            event.preventDefault();
            event.stopPropagation();
            await dotNetReference.invokeMethodAsync("NotifyOutputFindShortcutAsync");
            return;
        }

        if (key === "c") {
            event.preventDefault();
            event.stopPropagation();
            await dotNetReference.invokeMethodAsync("NotifyOutputCopyShortcutAsync");
            notifySelectionState();
        }
    };
    const themeObserver = new MutationObserver(() => notifyThemeState());
    const resizeObserver = new ResizeObserver(() => notifyHostResize());

    viewportElement.addEventListener("scroll", notifyViewportState, { passive: true });
    outputElement.addEventListener("keydown", handleKeyDown, true);
    outputElement.addEventListener("mouseup", notifySelectionState);
    outputElement.addEventListener("keyup", notifySelectionState);
    document.addEventListener("selectionchange", notifySelectionState);
    resizeObserver.observe(outputElement);
    themeObserver.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ["class", "style", "data-theme"]
    });

    if (document.body) {
        themeObserver.observe(document.body, {
            attributes: true,
            attributeFilter: ["class", "style", "data-theme"]
        });
    }

    outputElement[outputPanelStateKey] = {
        dotNetReference,
        handleKeyDown,
        notifySelectionState,
        viewportElement,
        notifyViewportState,
        resizeObserver,
        themeObserver
    };

    notifyViewportState();
    notifySelectionState();
}

// Removes the shell-owned scroll helper from the output viewport.
export function disposeOutputPanel(outputElement) {
    // Disposal guards against repeated open/close cycles within the same Blazor session.
    if (!outputElement || !outputElement[outputPanelStateKey]) {
        return;
    }

    const state = outputElement[outputPanelStateKey];
    state.viewportElement.removeEventListener("scroll", state.notifyViewportState);
    outputElement.removeEventListener("keydown", state.handleKeyDown, true);
    outputElement.removeEventListener("mouseup", state.notifySelectionState);
    outputElement.removeEventListener("keyup", state.notifySelectionState);
    document.removeEventListener("selectionchange", state.notifySelectionState);
    state.resizeObserver.disconnect();
    state.themeObserver.disconnect();
    delete outputElement[outputPanelStateKey];
}

// Scrolls the output viewport to the most recent retained entry.
export function scrollToEnd(outputElement) {
    // The panel remains browser-owned for scrolling so the shell can request the motion after render without manual DOM traversal in .NET.
    const viewportElement = getOutputViewport(outputElement);
    if (!viewportElement) {
        return;
    }

    viewportElement.scrollTop = viewportElement.scrollHeight;
}
