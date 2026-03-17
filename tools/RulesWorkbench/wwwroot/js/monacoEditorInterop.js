let monacoLoaderPromise;

function ensureMonaco() {
	if (monacoLoaderPromise) {
		return monacoLoaderPromise;
	}

	monacoLoaderPromise = new Promise((resolve, reject) => {
		if (window.monaco && window.monaco.editor) {
			resolve(window.monaco);
			return;
		}

		const requireConfigured = window.require && window.require.config;
		if (!requireConfigured) {
			reject(new Error('Monaco loader (require.js) not found.')); 
			return;
		}

		window.require.config({ paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs' } });
		window.require(['vs/editor/editor.main'], () => {
			resolve(window.monaco);
		}, reject);
	});

	return monacoLoaderPromise;
}

export async function createJsonEditor(hostElement, value, isReadOnly, enableFolding, dotNetRef) {
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
	});

	// Monaco can occasionally mis-measure its container in Blazor during initial render or
	// when the element becomes visible after layout changes. Force a layout once the
	// browser has painted, and also on container resizes.
	requestAnimationFrame(() => {
		try { editor.layout(); } catch { }
	});
	setTimeout(() => {
		try { editor.layout(); } catch { }
	}, 0);

	let resizeObserver = null;
	if (typeof ResizeObserver !== 'undefined') {
		resizeObserver = new ResizeObserver(() => {
			try { editor.layout(); } catch { }
		});
		try { resizeObserver.observe(hostElement); } catch { }
	}

	const model = editor.getModel();
	let suppress = false;

	if (model) {
		model.onDidChangeContent(() => {
			if (suppress) {
				return;
			}

			const current = editor.getValue();
			dotNetRef.invokeMethodAsync('OnEditorValueChanged', current);
		});
	}

    return {
		editor,
      resizeObserver,
		setSuppress: (v) => { suppress = v; },
	};
}

export function setValue(editorHandle, value) {
	if (!editorHandle || !editorHandle.editor) {
		return;
	}

	editorHandle.setSuppress(true);
	try {
		editorHandle.editor.setValue(value ?? '');
	}
	finally {
		editorHandle.setSuppress(false);
	}

	try { editorHandle.editor.layout(); } catch { }
}

export function layout(editorHandle) {
	if (!editorHandle || !editorHandle.editor) {
		return;
	}

	try {
		editorHandle.editor.layout();
	}
	catch {
		// ignore
	}
}

export function dispose(editorHandle) {
	if (!editorHandle || !editorHandle.editor) {
		return;
	}

	try {
      if (editorHandle.resizeObserver) {
			try { editorHandle.resizeObserver.disconnect(); } catch { }
		}

		try {
			const model = editorHandle.editor.getModel();
			if (model) {
				model.dispose();
			}
		}
		catch {
		}

		try {
			const dom = editorHandle.editor.getDomNode();
			if (dom) {
				dom.innerHTML = '';
			}
		}
		catch {
		}

		editorHandle.editor.dispose();
	}
	catch {
		// ignore
	}
}
