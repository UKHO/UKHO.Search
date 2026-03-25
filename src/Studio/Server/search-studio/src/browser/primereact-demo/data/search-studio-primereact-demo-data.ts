const demoProviders = ['Hydrography', 'Rules', 'Ingestion', 'Operations', 'Catalog'];
const demoRegions = ['North Sea', 'Irish Sea', 'Channel', 'Atlantic', 'Solent', 'Clyde'];
const demoOwners = ['Survey team', 'Rules reviewer', 'Operations analyst', 'Studio maintainer', 'Publishing desk'];
const demoStatuses = ['Ready', 'Review', 'Attention', 'Blocked'] as const;
const demoPriorities = ['Low', 'Medium', 'High'] as const;
const demoTreeDomains = ['Providers', 'Rules', 'Pipelines', 'Catalogues', 'Operations', 'Review'];
const demoWorkspaceKinds = ['Workspace', 'Collection', 'Queue', 'Feed'];

/**
 * Describes a single record rendered by the temporary PrimeReact `DataTable` demo page.
 */
export interface SearchStudioPrimeReactDemoTableRecord {
    /**
     * Stores the stable row identifier used for selection and inline editing.
     */
    readonly id: string;

    /**
     * Stores the visible dataset or work item title shown in the first business column.
     */
    readonly title: string;

    /**
     * Stores the loosely Studio-shaped workspace or grouping name used to show denser business data.
     */
    readonly workspace: string;

    /**
     * Stores the provider or capability area associated with the row.
     */
    readonly provider: string;

    /**
     * Stores the geographic or business area used to make the mock data feel varied.
     */
    readonly region: string;

    /**
     * Stores the owner label shown in the table and filterable summary text.
     */
    readonly owner: string;

    /**
     * Stores the current mock lifecycle state rendered as a PrimeReact severity tag.
     */
    readonly status: SearchStudioPrimeReactDemoStatus;

    /**
     * Stores the relative urgency used to add a second compact status-like column.
     */
    readonly priority: SearchStudioPrimeReactDemoPriority;

    /**
     * Stores the representative item count used to exercise right-aligned numeric columns.
     */
    readonly itemCount: number;

    /**
     * Stores the ISO-style review date string used for table density and sorting.
     */
    readonly lastReviewedOn: string;

    /**
     * Stores the inline editable reviewer note shown in the wider text column.
     */
    readonly reviewNotes: string;

    /**
     * Stores whether the row should permit inline editing during the mock-only review flow.
     */
    readonly isEditable: boolean;
}

/**
 * Describes the domain-specific metadata rendered beside each tree or tree-table node.
 */
export interface SearchStudioPrimeReactDemoTreeNodeData {
    /**
     * Stores the visible display name that the tree-table columns bind to.
     */
    readonly name: string;

    /**
     * Stores the loose type label shown in supporting tags and columns.
     */
    readonly type: string;

    /**
     * Stores the owner or responsible persona shown in the data-heavy hierarchy demos.
     */
    readonly owner: string;

    /**
     * Stores the current mock lifecycle state rendered as a PrimeReact severity tag.
     */
    readonly status: SearchStudioPrimeReactDemoStatus;

    /**
     * Stores the representative item count used to communicate hierarchy density.
     */
    readonly itemCount: number;

    /**
     * Stores the ISO-style timestamp string used for timeline-like columns and summaries.
     */
    readonly lastUpdatedOn: string;
}

/**
 * Describes a single tree-shaped node used by both the `Tree` and `TreeTable` demo pages.
 */
export interface SearchStudioPrimeReactDemoTreeNode {
    /**
     * Stores the stable node key used by PrimeReact expansion and selection state.
     */
    readonly key: string;

    /**
     * Stores the primary visible label shown by the `Tree` node template.
     */
    readonly label: string;

    /**
     * Stores the optional PrimeIcons class used to differentiate top-level node types visually.
     */
    readonly icon?: string;

    /**
     * Stores the supporting metadata consumed by the tree template and tree-table columns.
     */
    readonly data: SearchStudioPrimeReactDemoTreeNodeData;

    /**
     * Stores whether the node should be treated as a terminal item by PrimeReact.
     */
    readonly leaf?: boolean;

    /**
     * Stores whether the node should participate in selection interactions.
     */
    readonly selectable?: boolean;

    /**
     * Stores any child nodes used to create deeper hierarchy and scrolling density.
     */
    readonly children?: ReadonlyArray<SearchStudioPrimeReactDemoTreeNode>;
}

/**
 * Describes a single record rendered by the temporary PrimeReact `DataView` card and list demo page.
 */
export interface SearchStudioPrimeReactDemoDataViewRecord {
    /**
     * Stores the stable card identifier used for pagination and mock selection state.
     */
    readonly id: string;

    /**
     * Stores the visible title rendered at the top of the card or list item.
     */
    readonly title: string;

    /**
     * Stores the supporting summary text rendered inside the card or list body.
     */
    readonly summary: string;

    /**
     * Stores the loosely Studio-shaped owner label used to vary the mock content.
     */
    readonly owner: string;

    /**
     * Stores the provider or capability label used for quick card scanning.
     */
    readonly provider: string;

    /**
     * Stores the region or operating area shown in the metadata row.
     */
    readonly region: string;

    /**
     * Stores the mock lifecycle state rendered through a PrimeReact tag.
     */
    readonly status: SearchStudioPrimeReactDemoStatus;

    /**
     * Stores the representative item count used in the compact metric section.
     */
    readonly itemCount: number;

    /**
     * Stores the ISO-style review date string shown in the supporting metadata.
     */
    readonly lastUpdatedOn: string;

    /**
     * Stores the short emphasis label used to highlight a subset of cards during review.
     */
    readonly emphasis: 'Spotlight' | 'Compare' | 'Baseline';
}

/**
 * Identifies the supported mock lifecycle states reused across the data-heavy demo pages.
 */
export type SearchStudioPrimeReactDemoStatus = typeof demoStatuses[number];

/**
 * Identifies the supported mock priority labels reused in the data-table page.
 */
export type SearchStudioPrimeReactDemoPriority = typeof demoPriorities[number];

/**
 * Returns the supported status values used by the temporary data-heavy demo pages.
 *
 * @returns The immutable list of supported mock lifecycle states.
 */
export function getSearchStudioPrimeReactDemoStatuses(): ReadonlyArray<SearchStudioPrimeReactDemoStatus> {
    // Expose the shared status values through a helper so page components and tests reuse one stable list.
    return demoStatuses;
}

/**
 * Creates a large in-memory record set for the temporary `DataTable` demo page.
 *
 * @param recordCount Identifies how many rows should be generated for scrolling, pagination, and density review.
 * @returns The generated mock records in a stable deterministic order.
 */
export function createDataTableDemoRecords(recordCount = 180): ReadonlyArray<SearchStudioPrimeReactDemoTableRecord> {
    const records: SearchStudioPrimeReactDemoTableRecord[] = [];

    // Generate deterministic mixed mock rows so reviewers can evaluate sorting, filtering, pagination, and editing repeatedly.
    for (let index = 0; index < recordCount; index += 1) {
        records.push(createDataTableDemoRecord(index));
    }

    return records;
}

/**
 * Creates a medium-sized in-memory record set for the temporary `DataView` demo page.
 *
 * @param recordCount Identifies how many cards or list items should be generated for density and pagination review.
 * @returns The generated mock card-list records in a stable deterministic order.
 */
export function createDataViewDemoRecords(recordCount = 18): ReadonlyArray<SearchStudioPrimeReactDemoDataViewRecord> {
    const records: SearchStudioPrimeReactDemoDataViewRecord[] = [];

    // Generate deterministic mixed records so reviewers can compare the same content in both grid and list layouts repeatedly.
    for (let index = 0; index < recordCount; index += 1) {
        const provider = demoProviders[index % demoProviders.length];
        const region = demoRegions[(index + 2) % demoRegions.length];
        const owner = demoOwners[(index + 1) % demoOwners.length];

        records.push({
            id: `card-${index + 1}`,
            title: `${provider} review pack ${index + 1}`,
            summary: `This disposable record compares card and list presentation for ${region.toLowerCase()} handoff work inside the Studio shell.`,
            owner,
            provider,
            region,
            status: demoStatuses[(index + 1) % demoStatuses.length],
            itemCount: ((index * 5) % 80) + 12,
            lastUpdatedOn: createDemoDateString(index + 4),
            emphasis: index % 6 === 0 ? 'Spotlight' : index % 3 === 0 ? 'Compare' : 'Baseline'
        });
    }

    return records;
}

/**
 * Creates a large in-memory tree for the temporary `Tree` demo page.
 *
 * @param domainCount Identifies how many top-level domains should be generated.
 * @param workspaceCount Identifies how many mid-level nodes each domain should contain.
 * @param itemCount Identifies how many leaf nodes each workspace should contain.
 * @returns The generated mock tree nodes in a stable deterministic hierarchy.
 */
export function createTreeDemoNodes(
    domainCount = 6,
    workspaceCount = 6,
    itemCount = 5
): ReadonlyArray<SearchStudioPrimeReactDemoTreeNode> {
    const rootNodes: SearchStudioPrimeReactDemoTreeNode[] = [];

    // Build a three-level hierarchy so the Tree page can demonstrate expand/collapse, selection, and useful toolbar actions.
    for (let domainIndex = 0; domainIndex < domainCount; domainIndex += 1) {
        const domainKey = `tree-domain-${domainIndex + 1}`;
        const domainName = `${demoTreeDomains[domainIndex % demoTreeDomains.length]} domain ${domainIndex + 1}`;
        const children: SearchStudioPrimeReactDemoTreeNode[] = [];

        for (let workspaceIndex = 0; workspaceIndex < workspaceCount; workspaceIndex += 1) {
            const workspaceKey = `${domainKey}-workspace-${workspaceIndex + 1}`;
            const workspaceName = `${demoWorkspaceKinds[workspaceIndex % demoWorkspaceKinds.length]} ${workspaceIndex + 1}`;
            const leafNodes: SearchStudioPrimeReactDemoTreeNode[] = [];

            for (let itemIndex = 0; itemIndex < itemCount; itemIndex += 1) {
                const leafSequence = (domainIndex * workspaceCount * itemCount) + (workspaceIndex * itemCount) + itemIndex;
                const leafKey = `${workspaceKey}-item-${itemIndex + 1}`;
                const leafLabel = `Package ${leafSequence + 1}`;

                leafNodes.push({
                    key: leafKey,
                    label: leafLabel,
                    icon: 'pi pi-file',
                    leaf: true,
                    selectable: true,
                    data: {
                        name: leafLabel,
                        type: 'Package',
                        owner: demoOwners[leafSequence % demoOwners.length],
                        status: demoStatuses[leafSequence % demoStatuses.length],
                        itemCount: (leafSequence % 7) + 1,
                        lastUpdatedOn: createDemoDateString(leafSequence)
                    }
                });
            }

            children.push({
                key: workspaceKey,
                label: workspaceName,
                icon: 'pi pi-folder-open',
                selectable: true,
                data: {
                    name: workspaceName,
                    type: demoWorkspaceKinds[workspaceIndex % demoWorkspaceKinds.length],
                    owner: demoOwners[(domainIndex + workspaceIndex) % demoOwners.length],
                    status: demoStatuses[(domainIndex + workspaceIndex) % demoStatuses.length],
                    itemCount: leafNodes.length,
                    lastUpdatedOn: createDemoDateString(domainIndex + workspaceIndex)
                },
                children: leafNodes
            });
        }

        rootNodes.push({
            key: domainKey,
            label: domainName,
            icon: 'pi pi-database',
            selectable: true,
            data: {
                name: domainName,
                type: 'Domain',
                owner: demoOwners[domainIndex % demoOwners.length],
                status: demoStatuses[domainIndex % demoStatuses.length],
                itemCount: children.length,
                lastUpdatedOn: createDemoDateString(domainIndex)
            },
            children
        });
    }

    return rootNodes;
}

/**
 * Creates a large in-memory hierarchy for the temporary `TreeTable` demo page.
 *
 * @param domainCount Identifies how many top-level groups should be generated.
 * @param workspaceCount Identifies how many child groups each domain should contain.
 * @param itemCount Identifies how many terminal rows each child group should contain.
 * @returns The generated mock tree-table nodes in a stable deterministic hierarchy.
 */
export function createTreeTableDemoNodes(
    domainCount = 5,
    workspaceCount = 5,
    itemCount = 6
): ReadonlyArray<SearchStudioPrimeReactDemoTreeNode> {
    const rootNodes: SearchStudioPrimeReactDemoTreeNode[] = [];

    // Build a dense but readable hierarchy so the TreeTable page can demonstrate scrollable columns and nested business rows.
    for (let domainIndex = 0; domainIndex < domainCount; domainIndex += 1) {
        const domainKey = `tree-table-domain-${domainIndex + 1}`;
        const domainName = `${demoTreeDomains[domainIndex % demoTreeDomains.length]} portfolio ${domainIndex + 1}`;
        const workspaceNodes: SearchStudioPrimeReactDemoTreeNode[] = [];

        for (let workspaceIndex = 0; workspaceIndex < workspaceCount; workspaceIndex += 1) {
            const workspaceKey = `${domainKey}-workspace-${workspaceIndex + 1}`;
            const workspaceName = `${demoWorkspaceKinds[workspaceIndex % demoWorkspaceKinds.length]} lane ${workspaceIndex + 1}`;
            const itemNodes: SearchStudioPrimeReactDemoTreeNode[] = [];

            for (let itemIndex = 0; itemIndex < itemCount; itemIndex += 1) {
                const itemSequence = (domainIndex * workspaceCount * itemCount) + (workspaceIndex * itemCount) + itemIndex;
                const itemKey = `${workspaceKey}-row-${itemIndex + 1}`;
                const itemName = `Review row ${itemSequence + 1}`;

                itemNodes.push({
                    key: itemKey,
                    label: itemName,
                    icon: 'pi pi-file-edit',
                    leaf: true,
                    selectable: true,
                    data: {
                        name: itemName,
                        type: 'Row',
                        owner: demoOwners[itemSequence % demoOwners.length],
                        status: demoStatuses[(itemSequence + 1) % demoStatuses.length],
                        itemCount: ((itemSequence + 2) % 11) + 2,
                        lastUpdatedOn: createDemoDateString(itemSequence + 3)
                    }
                });
            }

            workspaceNodes.push({
                key: workspaceKey,
                label: workspaceName,
                icon: 'pi pi-sitemap',
                selectable: true,
                data: {
                    name: workspaceName,
                    type: 'Lane',
                    owner: demoOwners[(domainIndex + workspaceIndex + 1) % demoOwners.length],
                    status: demoStatuses[(domainIndex + workspaceIndex + 2) % demoStatuses.length],
                    itemCount: itemNodes.reduce((total, node) => total + node.data.itemCount, 0),
                    lastUpdatedOn: createDemoDateString(domainIndex + workspaceIndex + 5)
                },
                children: itemNodes
            });
        }

        rootNodes.push({
            key: domainKey,
            label: domainName,
            icon: 'pi pi-folder',
            selectable: true,
            data: {
                name: domainName,
                type: 'Portfolio',
                owner: demoOwners[(domainIndex + 2) % demoOwners.length],
                status: demoStatuses[(domainIndex + 3) % demoStatuses.length],
                itemCount: workspaceNodes.reduce((total, node) => total + node.data.itemCount, 0),
                lastUpdatedOn: createDemoDateString(domainIndex + 7)
            },
            children: workspaceNodes
        });
    }

    return rootNodes;
}

/**
 * Creates a single deterministic mock record for the temporary `DataTable` page.
 *
 * @param index Identifies which deterministic row permutation should be created.
 * @returns The generated mock business-style record.
 */
function createDataTableDemoRecord(index: number): SearchStudioPrimeReactDemoTableRecord {
    const provider = demoProviders[index % demoProviders.length];
    const region = demoRegions[index % demoRegions.length];
    const owner = demoOwners[index % demoOwners.length];
    const status = demoStatuses[index % demoStatuses.length];
    const priority = demoPriorities[index % demoPriorities.length];
    const workspaceNumber = Math.floor(index / 6) + 1;
    const title = `${region} dataset ${index + 1}`;

    // Blend generic and Studio-shaped wording so the table feels realistic without depending on any backend contract.
    return {
        id: `record-${index + 1}`,
        title,
        workspace: `${provider} workspace ${workspaceNumber}`,
        provider,
        region,
        owner,
        status,
        priority,
        itemCount: ((index * 7) % 125) + 18,
        lastReviewedOn: createDemoDateString(index),
        reviewNotes: index % 3 === 0 ? 'Ready for review panel' : 'Awaiting mock follow-up',
        isEditable: index % 5 !== 0
    };
}

/**
 * Creates a deterministic date-like string for dense table and hierarchy surfaces.
 *
 * @param offset Identifies the deterministic day offset that should influence the mock value.
 * @returns The generated ISO-style date string.
 */
function createDemoDateString(offset: number): string {
    const month = ((offset % 9) + 1).toString().padStart(2, '0');
    const day = ((offset % 27) + 1).toString().padStart(2, '0');

    // Keep the value ISO-like so PrimeReact sorting and reviewer scanning both remain straightforward.
    return `2025-${month}-${day}`;
}
