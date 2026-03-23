import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioSearchToggleCommandId,
    SearchStudioSearchViewRank,
    SearchStudioSearchViewContainerId,
    SearchStudioSearchWidgetId,
    SearchStudioSearchWidgetLabel
} from '../search-studio-constants';
import { SearchStudioSearchWidget } from './search-studio-search-widget';

@injectable()
export class SearchStudioSearchViewContribution extends AbstractViewContribution<SearchStudioSearchWidget> {

    constructor() {
        super({
            widgetId: SearchStudioSearchWidgetId,
            viewContainerId: SearchStudioSearchViewContainerId,
            widgetName: SearchStudioSearchWidgetLabel,
            defaultWidgetOptions: {
                area: 'left',
                rank: SearchStudioSearchViewRank
            },
            toggleCommandId: SearchStudioSearchToggleCommandId
        });
    }
}
