import { injectable } from '@theia/core/shared/inversify';
import { AbstractViewContribution } from '@theia/core/lib/browser/shell/view-contribution';
import {
    SearchStudioRulesViewRank,
    SearchStudioRulesViewContainerId,
    SearchStudioRulesToggleCommandId,
    SearchStudioRulesWidgetId,
    SearchStudioRulesWidgetLabel
} from '../search-studio-constants';
import { SearchStudioRulesWidget } from './search-studio-rules-widget';

@injectable()
export class SearchStudioRulesViewContribution extends AbstractViewContribution<SearchStudioRulesWidget> {

    constructor() {
        super({
            widgetId: SearchStudioRulesWidgetId,
            viewContainerId: SearchStudioRulesViewContainerId,
            widgetName: SearchStudioRulesWidgetLabel,
            defaultWidgetOptions: {
                area: 'left',
                rank: SearchStudioRulesViewRank
            },
            toggleCommandId: SearchStudioRulesToggleCommandId
        });
    }
}
