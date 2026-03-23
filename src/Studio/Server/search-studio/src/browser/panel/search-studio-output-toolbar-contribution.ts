import { Widget } from '@theia/core/lib/browser';
import { TabBarToolbarContribution, TabBarToolbarRegistry } from '@theia/core/lib/browser/shell/tab-bar-toolbar/tab-bar-toolbar-registry';
import { injectable } from '@theia/core/shared/inversify';
import {
    SearchStudioClearOutputCommand,
    SearchStudioCopyAllOutputCommand,
    SearchStudioOutputWidgetId
} from '../search-studio-constants';

@injectable()
export class SearchStudioOutputToolbarContribution implements TabBarToolbarContribution {

    registerToolbarItems(registry: TabBarToolbarRegistry): void {
        registry.registerItem({
            id: 'search-studio.output.copy-all.toolbar',
            command: SearchStudioCopyAllOutputCommand.id,
            icon: 'codicon codicon-copy',
            tooltip: 'Copy all',
            group: 'navigation',
            priority: 0,
            isVisible: widget => this.isOutputWidget(widget)
        });

        registry.registerItem({
            id: 'search-studio.output.clear.toolbar',
            command: SearchStudioClearOutputCommand.id,
            icon: 'codicon codicon-clear-all',
            tooltip: 'Clear output',
            group: 'navigation',
            priority: 1,
            isVisible: widget => this.isOutputWidget(widget)
        });
    }

    protected isOutputWidget(widget?: Widget): boolean {
        return widget?.id === SearchStudioOutputWidgetId;
    }
}
