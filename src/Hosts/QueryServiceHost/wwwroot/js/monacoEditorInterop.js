let monacoLoaderPromise;

const requireJsSources = [
    'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js',
    'https://cdn.jsdelivr.net/npm/requirejs@2.3.6/require.min.js'
];

// Waits briefly for the page-level require.js script to become available before attempting to load a fallback copy.
function waitForRequireLoader(maxAttempts = 20, delayMs = 100) {
    return new Promise((resolve) => {
        let attempts = 0;

        const poll = () => {
            if (window.require && window.require.config) {
                resolve(true);
                return;
            }

            attempts += 1;
            if (attempts >= maxAttempts) {
                resolve(false);
                return;
            }

            window.setTimeout(poll, delayMs);
        };

        poll();
    });
}

// Loads require.js dynamically when the page-level script did not become available in time.
function loadRequireScript(source) {
    return new Promise((resolve, reject) => {
        const existingScript = Array.from(document.scripts).find((script) => script.src === source);
        if (existingScript) {
            if (window.require && window.require.config) {
                resolve();
                return;
            }

            existingScript.addEventListener('load', () => resolve(), { once: true });
            existingScript.addEventListener('error', () => reject(new Error(`Failed to load require.js from '${source}'.`)), { once: true });
            return;
        }

        const script = document.createElement('script');
        script.src = source;
        script.async = true;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load require.js from '${source}'.`));
        document.head.appendChild(script);
    });
}

// Ensures the AMD loader Monaco depends on is available even when the initial page script arrives late or fails to load.
async function ensureRequireLoader() {
    if (window.require && window.require.config) {
        return;
    }

    // Give the page-level script a short chance to finish first because it is still the preferred startup path.
    const foundExistingLoader = await waitForRequireLoader();
    if (foundExistingLoader) {
        return;
    }

    const loadErrors = [];

    for (const source of requireJsSources) {
        try {
            await loadRequireScript(source);

            if (window.require && window.require.config) {
                return;
            }
        }
        catch (error) {
            loadErrors.push(error instanceof Error ? error.message : String(error));
        }
    }

    throw new Error(`Monaco loader (require.js) was not available. ${loadErrors.join(' ')}`.trim());
}

// Loads Monaco once for the entire QueryServiceHost page so repeated editor renders reuse the same browser-side runtime.
function ensureMonaco() {
    if (monacoLoaderPromise) {
        return monacoLoaderPromise;
    }

    monacoLoaderPromise = (async () => {
        if (window.monaco && window.monaco.editor) {
            return window.monaco;
        }

        await ensureRequireLoader();

        return await new Promise((resolve, reject) => {
            window.require.config({
                paths: {
                    vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs'
                }
            });

            window.require(['vs/editor/editor.main'], () => {
                resolve(window.monaco);
            }, reject);
        });
    })().catch((error) => {
        // Clear the cached promise after failure so a later retry can succeed once the loader or network recovers.
        monacoLoaderPromise = null;
        throw error;
    });

    return monacoLoaderPromise;
}

// Creates the Monaco JSON editor inside the empty host element supplied by the Blazor component.
export async function createJsonEditor(hostElement, value, isReadOnly, enableFolding, dotNetReference) {
    const monaco = await ensureMonaco();

    const editor = monaco.editor.create(hostElement, {
        value: value ?? '',
        language: 'json',
        readOnly: isReadOnly === true,
        automaticLayout: true,
        minimap: { enabled: false },
        folding: enableFolding === true,
        lineNumbers: 'on',
        scrollBeyondLastLine: false,
        theme: 'vs-dark'
    });

    // Monaco can mis-measure its container during the first render or after layout changes, so force an initial layout pass.
    requestAnimationFrame(() => {
        try {
            editor.layout();
        }
        catch {
            // Ignore layout failures because the .NET side handles any persistent initialization errors.
        }
    });

    setTimeout(() => {
        try {
            editor.layout();
        }
        catch {
            // Ignore layout failures because the .NET side handles any persistent initialization errors.
        }
    }, 0);

    let resizeObserver = null;
    if (typeof ResizeObserver !== 'undefined') {
        resizeObserver = new ResizeObserver(() => {
            try {
                editor.layout();
            }
            catch {
                // Ignore transient layout failures during resize churn.
            }
        });

        try {
            resizeObserver.observe(hostElement);
        }
        catch {
            // Ignore observer registration failures because Monaco still has automatic layout as a fallback.
        }
    }

    const model = editor.getModel();
    let suppress = false;

    if (model) {
        model.onDidChangeContent(() => {
            if (suppress) {
                return;
            }

            dotNetReference.invokeMethodAsync('OnEditorValueChanged', editor.getValue());
        });
    }

    return {
        editor,
        resizeObserver,
        setSuppress: (valueToSuppress) => {
            suppress = valueToSuppress;
        }
    };
}

// Updates the editor text while suppressing change notifications back into Blazor.
export function setValue(editorHandle, value) {
    if (!editorHandle || !editorHandle.editor) {
        return;
    }

    editorHandle.setSuppress(true);

    try {
        editorHandle.editor.setValue(value ?? '');
        editorHandle.editor.layout();
    }
    finally {
        editorHandle.setSuppress(false);
    }
}

// Forces Monaco to re-measure its current host size after a Blazor render pass.
export function layout(editorHandle) {
    if (!editorHandle || !editorHandle.editor) {
        return;
    }

    try {
        editorHandle.editor.layout();
    }
    catch {
        // Ignore layout failures because they are usually transient during host resize work.
    }
}

// Disposes Monaco resources so browser memory is released when the Blazor component leaves the page.
export function dispose(editorHandle) {
    if (!editorHandle || !editorHandle.editor) {
        return;
    }

    try {
        if (editorHandle.resizeObserver) {
            try {
                editorHandle.resizeObserver.disconnect();
            }
            catch {
                // Ignore observer cleanup failures because the editor is being torn down anyway.
            }
        }

        try {
            const model = editorHandle.editor.getModel();
            if (model) {
                model.dispose();
            }
        }
        catch {
            // Ignore model cleanup failures because the editor disposal below is the final safety net.
        }

        editorHandle.editor.dispose();
    }
    catch {
        // Ignore final cleanup failures because the .NET component is already disposing.
    }
}
