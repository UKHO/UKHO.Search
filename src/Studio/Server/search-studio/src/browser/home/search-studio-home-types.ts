export type SearchStudioHomeJumpPointId = 'start-ingestion' | 'manage-rules' | 'browse-providers';

export interface SearchStudioHomeJumpPoint {
    readonly id: SearchStudioHomeJumpPointId;
    readonly label: string;
    readonly description: string;
    readonly emphasis?: 'primary' | 'secondary';
}
