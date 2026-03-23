import {
    SearchStudioHomeJumpPoint
} from './search-studio-home-types';

export const SearchStudioHomeJumpPoints: readonly SearchStudioHomeJumpPoint[] = [
    {
        id: 'start-ingestion',
        label: 'Start ingestion',
        description: 'Open the closest existing ingestion landing document for the current or first available provider.',
        emphasis: 'primary'
    },
    {
        id: 'manage-rules',
        label: 'Manage rules',
        description: 'Open the current rules landing document so provider-scoped rule discovery and placeholder authoring surfaces are immediately available.'
    },
    {
        id: 'browse-providers',
        label: 'Browse providers',
        description: 'Open the provider overview so queue, dead-letter, and related operational placeholders are easy to review.'
    }
];
