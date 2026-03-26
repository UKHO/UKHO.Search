import * as React from '@theia/core/shared/react';
import { SearchStudioPrimeReactDemoHostDisplayMode } from './search-studio-primereact-demo-page-props';

/**
 * Describes the shared layout options that determine how a retained PrimeReact page should be hosted inside the Studio shell.
 */
export interface SearchStudioPrimeReactDemoPageLayoutOptions {
    /**
     * Identifies whether the page is running standalone or inside the retained consolidated showcase tabs.
     */
    readonly hostDisplayMode?: SearchStudioPrimeReactDemoHostDisplayMode;

    /**
     * Identifies whether the page contains a data-heavy control that must own inner scrolling when tab-hosted.
     */
    readonly usesDataHeavyLayout?: boolean;
}

/**
 * Identifies how one retained tab panel should handle overflow inside the shared showcase shell.
 */
export type SearchStudioPrimeReactDemoTabContentOverflowMode = 'default' | 'contained';

/**
 * Describes the shared wrapper props used by retained showcase tab content.
 */
export interface SearchStudioPrimeReactDemoTabContentProps {
    /**
     * Stores the stable DOM identifier used by the shared focus-transfer helper.
     */
    readonly focusTargetId: string;

    /**
     * Stores the accessible label announced when keyboard focus moves into the retained tab content.
     */
    readonly ariaLabel: string;

    /**
     * Identifies whether the outer tab panel or the inner PrimeReact surface should own overflow.
     */
    readonly overflowMode?: SearchStudioPrimeReactDemoTabContentOverflowMode;

    /**
     * Supplies the retained page content that should be rendered inside the shared focusable wrapper.
     */
    readonly children: React.ReactNode;
}

/**
 * Resolves the shared page root class list used by retained PrimeReact pages.
 *
 * @param options Supplies the shared layout options for the retained page.
 * @returns The stable class list that applies the shared theme and layout contract.
 */
export function getSearchStudioPrimeReactDemoPageClassName(
    options: SearchStudioPrimeReactDemoPageLayoutOptions
): string {
    const classNames = [
        'search-studio-primereact-demo-page',
        'search-studio-primereact-demo-page--styled'
    ];

    if (options.hostDisplayMode !== 'tabbed') {
        // Keep standalone pages on the base surface contract because only the retained tab shell needs the compact embedded layout rules.
        return classNames.join(' ');
    }

    // Apply the shared embedded-page layout rules whenever the page is hosted inside the retained root tab shell.
    classNames.push('search-studio-primereact-demo-page--tab-hosted');

    if (options.usesDataHeavyLayout) {
        // Opt data-heavy pages into the inner-scroll contract so grids and trees keep overflow ownership inside the page content area.
        classNames.push('search-studio-primereact-demo-page--tab-hosted-data-heavy');
    }

    return classNames.join(' ');
}

/**
 * Resolves the shared tab-panel wrapper class list used by retained showcase tab content.
 *
 * @param overflowMode Identifies whether the shared tab wrapper should contain overflow for an inner scroll owner.
 * @returns The stable tab-panel wrapper class list.
 */
export function getSearchStudioPrimeReactDemoTabContentClassName(
    overflowMode: SearchStudioPrimeReactDemoTabContentOverflowMode = 'default'
): string {
    const classNames = ['search-studio-primereact-demo-page__tab-panel-content'];

    if (overflowMode === 'contained') {
        // Keep overflow on the inner PrimeReact surface when the page exposes its own scrollable grid, tree, or splitter layout.
        classNames.push('search-studio-primereact-demo-page__tab-panel-content--contained');
    }

    return classNames.join(' ');
}

/**
 * Resolves the scroll-height contract used by retained data-heavy PrimeReact pages.
 *
 * @param hostDisplayMode Identifies whether the page is standalone or embedded inside the retained showcase tabs.
 * @returns The PrimeReact scroll-height value that preserves inner scroll ownership for the current host.
 */
export function getSearchStudioPrimeReactDemoDataScrollHeight(
    hostDisplayMode: SearchStudioPrimeReactDemoHostDisplayMode | undefined
): 'flex' | '32rem' {
    if (hostDisplayMode === 'tabbed') {
        // Use PrimeReact flex scrolling inside the retained tab shell so the workbench host stays fixed while the inner data surface scrolls.
        return 'flex';
    }

    // Keep the standalone pages on their existing fixed-height scroll contract so the broader demo surfaces still render predictably outside the tab shell.
    return '32rem';
}

/**
 * Renders the shared focusable tab-panel wrapper used by every retained showcase tab.
 *
 * @param props Supplies the stable focus target, accessibility metadata, overflow contract, and retained page content.
 * @returns The shared tab-panel wrapper element.
 */
export function SearchStudioPrimeReactDemoTabContent(props: SearchStudioPrimeReactDemoTabContentProps): React.ReactNode {
    const className = getSearchStudioPrimeReactDemoTabContentClassName(props.overflowMode);

    // Render one shared focusable wrapper so every retained tab keeps the same focus handoff and overflow ownership contract.
    return (
        <section
            id={props.focusTargetId}
            tabIndex={-1}
            aria-label={props.ariaLabel}
            className={className}>
            {props.children}
        </section>
    );
}
