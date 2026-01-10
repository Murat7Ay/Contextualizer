import { Outlet } from 'react-router-dom';
import { Toolbar } from './Toolbar';
import { TabBar } from './TabBar';
import { ActivityLog } from './ActivityLog';
import { useTabStore } from '../../stores/tabStore';
import { useAppStore } from '../../stores/appStore';
import { ResizablePanelGroup, ResizablePanel, ResizableHandle } from '../ui/resizable';

export function MainLayout() {
  const tabs = useTabStore((state) => state.tabs);
  const activityLogPosition = useAppStore((state) => state.activityLogPosition);

  return (
    <div className="h-screen flex flex-col bg-background">
      {/* Native Title Bar would be here (handled by WPF host) */}
      
      {/* Toolbar */}
      <Toolbar />
      
      {/* Tab Bar (only shown when tabs are open) */}
      {tabs.length > 0 && <TabBar />}
      
      {/* Main Content Area */}
      <div className="flex-1 flex overflow-hidden">
        {activityLogPosition === 'right' ? (
          <ResizablePanelGroup direction="horizontal">
            <ResizablePanel defaultSize={70} minSize={30}>
              <div className="h-full overflow-auto">
                <Outlet />
              </div>
            </ResizablePanel>
            <ResizableHandle />
            <ResizablePanel defaultSize={30} minSize={20} maxSize={50}>
              <ActivityLog />
            </ResizablePanel>
          </ResizablePanelGroup>
        ) : (
          <ResizablePanelGroup direction="vertical" className="flex-1">
            <ResizablePanel defaultSize={70} minSize={30}>
              <div className="h-full overflow-auto">
                <Outlet />
              </div>
            </ResizablePanel>
            <ResizableHandle />
            <ResizablePanel defaultSize={30} minSize={20} maxSize={50}>
              <ActivityLog />
            </ResizablePanel>
          </ResizablePanelGroup>
        )}
      </div>
    </div>
  );
}
