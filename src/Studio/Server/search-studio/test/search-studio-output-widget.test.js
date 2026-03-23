const test = require('node:test');
const assert = require('node:assert/strict');
const Module = require('node:module');

global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({ style: {}, classList: { add() {}, remove() {} }, setAttribute() {}, removeAttribute() {}, nodeType: 1, ownerDocument: null }),
    documentElement: { style: {} },
    body: { classList: { add() {}, remove() {} } },
    fonts: {
        ready: Promise.resolve()
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    devicePixelRatio: 1.25,
    getComputedStyle() {
        return {
            getPropertyValue(name) {
                if (name === '--theia-editor-font-family') {
                    return 'Cascadia Mono, monospace';
                }

                if (name === '--theia-editor-font-size') {
                    return '14px';
                }

                return '';
            },
            fontFamily: 'Segoe UI',
            fontSize: '14px',
            lineHeight: '14px',
            letterSpacing: '0px',
            backgroundColor: 'rgb(37, 37, 38)',
            color: 'rgb(255, 255, 255)'
        };
    },
    localStorage: {
        getItem() {
            return undefined;
        },
        setItem() {},
        removeItem() {}
    },
    requestAnimationFrame(callback) {
        callback();
        return 0;
    },
    cancelAnimationFrame() {}
};
global.HTMLElement = class HTMLElement {};
global.Element = global.HTMLElement;
global.DragEvent = class DragEvent {};
global.ResizeObserver = class ResizeObserver {
    constructor(callback) {
        this.callback = callback;
    }

    observe(target) {
        this.target = target;
    }

    disconnect() {}
};

const terminalInstances = [];
const fitAddonInstances = [];
const webglAddonInstances = [];
const originalLoad = Module._load;

Module._load = function (request, parent, isMain) {
    if (request === 'react-dom/client') {
        return {
            createRoot() {
                return {
                    render() {},
                    unmount() {}
                };
            }
        };
    }

    if (request === '@xterm/xterm') {
        return {
            Terminal: class FakeTerminal {
                constructor(options) {
                    this.options = options;
                    this.rows = 1;
                    this.writes = [];
                    this.loadedAddons = [];
                    this.resetCount = 0;
                    this.disposeCount = 0;
                    this.scrollToBottomCount = 0;
                    terminalInstances.push(this);
                }

                loadAddon(addon) {
                    this.loadedAddons.push(addon);
                }

                open(host) {
                    this.host = host;
                }

                write(text) {
                    this.writes.push(text);
                }

                reset() {
                    this.resetCount += 1;
                }

                refresh(start, end) {
                    this.lastRefresh = { start, end };
                }

                scrollToBottom() {
                    this.scrollToBottomCount += 1;
                }

                dispose() {
                    this.disposeCount += 1;
                }
            }
        };
    }

    if (request === '@xterm/addon-fit') {
        return {
            FitAddon: class FakeFitAddon {
                constructor() {
                    this.fitCount = 0;
                    fitAddonInstances.push(this);
                }

                fit() {
                    this.fitCount += 1;
                }
            }
        };
    }

    if (request === '@xterm/addon-webgl') {
        return {
            WebglAddon: class FakeWebglAddon {
                constructor() {
                    webglAddonInstances.push(this);
                }
            }
        };
    }

    return originalLoad.call(this, request, parent, isMain);
};

const { SearchStudioOutputWidget } = require('../lib/browser/panel/search-studio-output-widget.js');

Module._load = originalLoad;

test.beforeEach(() => {
    terminalInstances.length = 0;
    fitAddonInstances.length = 0;
    webglAddonInstances.length = 0;
});

test('SearchStudioOutputWidget initializes a read-only terminal host and renders merged output text', () => {
    const widget = new SearchStudioOutputWidget();
    const terminalHost = {
        clientWidth: 640,
        clientHeight: 180,
        setAttribute() {},
        classList: { add() {} },
        querySelector() {
            return null;
        }
    };

    widget._outputService = {
        entries: [{ id: 'entry-1', timestamp: '2026-03-23T10:03:37.280Z', level: 'info', source: 'providers', message: 'Loaded providers.' }]
    };
    widget._captureTerminalHost(terminalHost);
    widget._hasPendingRevealLatest = true;

    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    assert.equal(terminalInstances.length, 1);
    assert.equal(fitAddonInstances.length, 1);
    assert.equal(webglAddonInstances.length, 1);
    assert.equal(terminalInstances[0].options.disableStdin, true);
    assert.equal(terminalInstances[0].options.fontFamily, 'Cascadia Mono, monospace');
    assert.equal(terminalInstances[0].options.fontSize, 14);
    assert.equal(terminalInstances[0].options.theme.background, 'rgb(37, 37, 38)');
    assert.equal(terminalInstances[0].options.theme.foreground, 'rgb(255, 255, 255)');
    assert.equal(terminalInstances[0].writes[0], '10:03:37 \u001b[38;2;169;199;255mINFO\u001b[39m [providers] Loaded providers.\r\n');
    assert.deepEqual(terminalInstances[0].lastRefresh, { start: 0, end: 0 });
    assert.equal(terminalInstances[0].scrollToBottomCount, 1);
    assert.equal(widget._hasPendingRevealLatest, false);
});

test('SearchStudioOutputWidget revealLatestIfNeeded does not scroll after clear resets the terminal buffer', () => {
    const widget = new SearchStudioOutputWidget();
    const terminalHost = {
        clientWidth: 640,
        clientHeight: 180,
        setAttribute() {},
        classList: { add() {} },
        querySelector() {
            return null;
        }
    };

    widget._outputService = {
        entries: [{ id: 'entry-1', timestamp: '2026-03-23T10:03:37.280Z', level: 'info', source: 'providers', message: 'Loaded providers.' }]
    };
    widget._captureTerminalHost(terminalHost);
    widget._hasPendingRevealLatest = true;

    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    widget._outputService = { entries: [] };
    widget._hasPendingRevealLatest = true;
    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    assert.equal(terminalInstances[0].resetCount, 2);
    assert.equal(terminalInstances[0].scrollToBottomCount, 1);
    assert.equal(widget._hasPendingRevealLatest, false);
});

test('SearchStudioOutputWidget clear followed by append writes only the new output line and reveals it', () => {
    const widget = new SearchStudioOutputWidget();
    const terminalHost = {
        clientWidth: 640,
        clientHeight: 180,
        setAttribute() {},
        classList: { add() {} },
        querySelector() {
            return null;
        }
    };

    widget._outputService = {
        entries: [{ id: 'entry-1', timestamp: '2026-03-23T10:03:37.280Z', level: 'info', source: 'providers', message: 'Loaded providers.' }]
    };
    widget._captureTerminalHost(terminalHost);
    widget._hasPendingRevealLatest = true;

    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    widget._outputService = { entries: [] };
    widget._hasPendingRevealLatest = true;
    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    widget._outputService = {
        entries: [{ id: 'entry-2', timestamp: '2026-03-23T10:03:38.280Z', level: 'error', source: 'rules', message: 'Rule validation failed.' }]
    };
    widget._hasPendingRevealLatest = true;
    widget.syncTerminalContent();
    widget.revealLatestIfNeeded();

    assert.deepEqual(terminalInstances[0].writes, [
        '10:03:37 \u001b[38;2;169;199;255mINFO\u001b[39m [providers] Loaded providers.\r\n',
        '10:03:38 \u001b[38;2;255;179;186mERROR\u001b[39m [rules] Rule validation failed.\r\n'
    ]);
    assert.equal(terminalInstances[0].resetCount, 3);
    assert.equal(terminalInstances[0].scrollToBottomCount, 2);
});

test('SearchStudioOutputWidget disposes the terminal host when the widget is disposed', () => {
    const widget = new SearchStudioOutputWidget();
    const terminalHost = {
        clientWidth: 640,
        clientHeight: 180,
        setAttribute() {},
        classList: { add() {} },
        querySelector() {
            return null;
        }
    };

    widget._outputService = { entries: [] };
    widget._captureTerminalHost(terminalHost);

    widget.dispose();

    assert.equal(terminalInstances[0].disposeCount, 1);
});
