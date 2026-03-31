const outputPanelStateKey = Symbol("workbenchOutputPanelState");

// Initializes the shell-owned output panel helpers for scroll tracking.
export function initializeOutputPanel(outputElement, dotNetReference) {
    // The helper stores its listener on the element so repeated renders can safely replace the callback without duplicating handlers.
    disposeOutputPanel(outputElement);

    if (!outputElement || !dotNetReference) {
        return;
    }

    const notifyViewportState = () => {
        // A small tolerance avoids false negatives caused by fractional browser layout values.
        const isAtEnd = outputElement.scrollTop + outputElement.clientHeight >= outputElement.scrollHeight - 4;
        dotNetReference.invokeMethodAsync("NotifyOutputViewportStateAsync", isAtEnd);
    };

    outputElement.addEventListener("scroll", notifyViewportState, { passive: true });
    outputElement[outputPanelStateKey] = {
        dotNetReference,
        notifyViewportState
    };

    notifyViewportState();
}

// Removes the shell-owned scroll helper from the output viewport.
export function disposeOutputPanel(outputElement) {
    // Disposal guards against repeated open/close cycles within the same Blazor session.
    if (!outputElement || !outputElement[outputPanelStateKey]) {
        return;
    }

    const state = outputElement[outputPanelStateKey];
    outputElement.removeEventListener("scroll", state.notifyViewportState);
    delete outputElement[outputPanelStateKey];
}

// Scrolls the output viewport to the most recent retained entry.
export function scrollToEnd(outputElement) {
    // The panel remains browser-owned for scrolling so the shell can request the motion after render without manual DOM traversal in .NET.
    if (!outputElement) {
        return;
    }

    outputElement.scrollTop = outputElement.scrollHeight;
}
