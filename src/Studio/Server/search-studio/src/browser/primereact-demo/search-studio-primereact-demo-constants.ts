import { Command } from '@theia/core/lib/common';

/**
 * Identifies the supported temporary PrimeReact demo pages that can be opened from the Theia `View` menu.
 */
export type SearchStudioPrimeReactDemoPageId = 'bootstrap' | 'datatable' | 'forms' | 'dataview' | 'layout' | 'showcase' | 'tree' | 'treetable';

/**
 * Describes the metadata used to expose one temporary PrimeReact demo page through commands, menus, and widget titles.
 */
export interface SearchStudioPrimeReactDemoPageDefinition {
    /**
     * Stores the stable logical page identifier used by the temporary demo widget.
     */
    readonly pageId: SearchStudioPrimeReactDemoPageId;

    /**
     * Stores the visible command and menu label shown to reviewers.
     */
    readonly label: string;

    /**
     * Stores the visible document tab label shown when the widget renders the page.
     */
    readonly widgetLabel: string;

    /**
     * Stores the explanatory document-tab caption shown for the page.
     */
    readonly widgetCaption: string;
}

/**
 * Describes one temporary PrimeReact page entry that should be exposed through Theia commands and menus.
 */
export interface SearchStudioPrimeReactDemoCommandDefinition {
    /**
     * Stores the stable logical page identifier that should open when the command runs.
     */
    readonly pageId: SearchStudioPrimeReactDemoPageId;

    /**
     * Stores the Theia command object registered for the temporary demo page.
     */
    readonly command: Command;

    /**
     * Stores the visible View-menu label shown to reviewers.
     */
    readonly menuLabel: string;
}

/**
 * Identifies the temporary PrimeReact demo widget instance in the workbench.
 */
export const SearchStudioPrimeReactDemoWidgetId = 'search-studio.primereact-demo';

/**
 * Identifies the widget factory that recreates the temporary PrimeReact demo document.
 */
export const SearchStudioPrimeReactDemoWidgetFactoryId = 'search-studio.primereact-demo';

/**
 * Supplies the visible tab label used for the temporary PrimeReact research document.
 */
export const SearchStudioPrimeReactDemoWidgetLabel = 'PrimeReact Demo';

/**
 * Supplies the workbench icon class used for the temporary PrimeReact research document tab.
 */
export const SearchStudioPrimeReactDemoWidgetIconClass = 'codicon codicon-symbol-color';

/**
 * Opens the temporary PrimeReact research page from commands and menus.
 */
export const SearchStudioShowPrimeReactDemoCommand: Command = {
    id: 'search-studio.primereact-demo.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Demo'
};

/**
 * Opens the temporary PrimeReact `DataTable` research page from commands and menus.
 */
export const SearchStudioShowPrimeReactDataTableDemoCommand: Command = {
    id: 'search-studio.primereact-demo.datatable.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Data Table Demo'
};

/**
 * Opens the temporary PrimeReact forms research page from commands and menus.
 */
export const SearchStudioShowPrimeReactFormsDemoCommand: Command = {
    id: 'search-studio.primereact-demo.forms.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Forms Demo'
};

/**
 * Opens the temporary PrimeReact `DataView` research page from commands and menus.
 */
export const SearchStudioShowPrimeReactDataViewDemoCommand: Command = {
    id: 'search-studio.primereact-demo.dataview.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Data View Demo'
};

/**
 * Opens the temporary PrimeReact layout and container research page from commands and menus.
 */
export const SearchStudioShowPrimeReactLayoutDemoCommand: Command = {
    id: 'search-studio.primereact-demo.layout.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Layout Demo'
};

/**
 * Opens the temporary PrimeReact combined showcase research page from commands and menus.
 */
export const SearchStudioShowPrimeReactShowcaseDemoCommand: Command = {
    id: 'search-studio.primereact-demo.showcase.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Showcase Demo'
};

/**
 * Opens the temporary PrimeReact `Tree` research page from commands and menus.
 */
export const SearchStudioShowPrimeReactTreeDemoCommand: Command = {
    id: 'search-studio.primereact-demo.tree.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Tree Demo'
};

/**
 * Opens the temporary PrimeReact `TreeTable` research page from commands and menus.
 */
export const SearchStudioShowPrimeReactTreeTableDemoCommand: Command = {
    id: 'search-studio.primereact-demo.treetable.show',
    category: 'UKHO Search Studio',
    label: 'PrimeReact Tree Table Demo'
};

const searchStudioPrimeReactDemoPageDefinitions: Record<SearchStudioPrimeReactDemoPageId, SearchStudioPrimeReactDemoPageDefinition> = {
    bootstrap: {
        pageId: 'bootstrap',
        label: 'PrimeReact Demo',
        widgetLabel: 'PrimeReact Demo',
        widgetCaption: 'Temporary PrimeReact bootstrap evaluation demo'
    },
    datatable: {
        pageId: 'datatable',
        label: 'PrimeReact Data Table Demo',
        widgetLabel: 'PrimeReact Data Table Demo',
        widgetCaption: 'Temporary PrimeReact DataTable evaluation demo'
    },
    forms: {
        pageId: 'forms',
        label: 'PrimeReact Forms Demo',
        widgetLabel: 'PrimeReact Forms Demo',
        widgetCaption: 'Temporary PrimeReact forms evaluation demo'
    },
    dataview: {
        pageId: 'dataview',
        label: 'PrimeReact Data View Demo',
        widgetLabel: 'PrimeReact Data View Demo',
        widgetCaption: 'Temporary PrimeReact DataView evaluation demo'
    },
    layout: {
        pageId: 'layout',
        label: 'PrimeReact Layout Demo',
        widgetLabel: 'PrimeReact Layout Demo',
        widgetCaption: 'Temporary PrimeReact layout and container evaluation demo'
    },
    showcase: {
        pageId: 'showcase',
        label: 'PrimeReact Showcase Demo',
        widgetLabel: 'PrimeReact Showcase Demo',
        widgetCaption: 'Temporary PrimeReact combined showcase evaluation demo'
    },
    tree: {
        pageId: 'tree',
        label: 'PrimeReact Tree Demo',
        widgetLabel: 'PrimeReact Tree Demo',
        widgetCaption: 'Temporary PrimeReact Tree evaluation demo'
    },
    treetable: {
        pageId: 'treetable',
        label: 'PrimeReact Tree Table Demo',
        widgetLabel: 'PrimeReact Tree Table Demo',
        widgetCaption: 'Temporary PrimeReact TreeTable evaluation demo'
    }
};

const searchStudioPrimeReactDemoCommandDefinitions: ReadonlyArray<SearchStudioPrimeReactDemoCommandDefinition> = [
    {
        pageId: 'bootstrap',
        command: SearchStudioShowPrimeReactDemoCommand,
        menuLabel: 'PrimeReact Demo'
    },
    {
        pageId: 'datatable',
        command: SearchStudioShowPrimeReactDataTableDemoCommand,
        menuLabel: 'PrimeReact Data Table Demo'
    },
    {
        pageId: 'forms',
        command: SearchStudioShowPrimeReactFormsDemoCommand,
        menuLabel: 'PrimeReact Forms Demo'
    },
    {
        pageId: 'dataview',
        command: SearchStudioShowPrimeReactDataViewDemoCommand,
        menuLabel: 'PrimeReact Data View Demo'
    },
    {
        pageId: 'layout',
        command: SearchStudioShowPrimeReactLayoutDemoCommand,
        menuLabel: 'PrimeReact Layout Demo'
    },
    {
        pageId: 'showcase',
        command: SearchStudioShowPrimeReactShowcaseDemoCommand,
        menuLabel: 'PrimeReact Showcase Demo'
    },
    {
        pageId: 'tree',
        command: SearchStudioShowPrimeReactTreeDemoCommand,
        menuLabel: 'PrimeReact Tree Demo'
    },
    {
        pageId: 'treetable',
        command: SearchStudioShowPrimeReactTreeTableDemoCommand,
        menuLabel: 'PrimeReact Tree Table Demo'
    }
];

/**
 * Gets the metadata definition for a supported temporary PrimeReact demo page.
 *
 * @param pageId Identifies the logical page that should be shown in the shared temporary demo widget.
 * @returns The metadata definition for the requested page.
 */
export function getSearchStudioPrimeReactDemoPageDefinition(
    pageId: SearchStudioPrimeReactDemoPageId
): SearchStudioPrimeReactDemoPageDefinition {
    // Resolve the shared page metadata from one central dictionary so commands, menus, and the widget stay aligned.
    return searchStudioPrimeReactDemoPageDefinitions[pageId];
}

/**
 * Gets the ordered temporary PrimeReact command definitions used by the shared command and menu contributions.
 *
 * @returns The immutable ordered list of temporary PrimeReact command definitions.
 */
export function getSearchStudioPrimeReactDemoCommandDefinitions(): ReadonlyArray<SearchStudioPrimeReactDemoCommandDefinition> {
    // Keep the temporary demo registrations in one ordered list so the entire research surface remains easy to review and remove later.
    return searchStudioPrimeReactDemoCommandDefinitions;
}
