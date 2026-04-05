import { Outlet } from 'react-router-dom';
import { Toolbar } from './Toolbar';
import { TabBar } from './TabBar';
import { ActivityLog } from './ActivityLog';
import { useTabStore } from '../../stores/tabStore';
import { useAppStore } from '../../stores/appStore';
import { ResizablePanelGroup, ResizablePanel, ResizableHandle } from '../ui/resizable';
import { ScrollArea } from '../ui/scroll-area';
import { cn } from '../ui/utils';

export function MainLayout() {
  const tabs = useTabStore((state) => state.tabs);
  const activityLogPosition = useAppStore((state) => state.activityLogPosition);
  const activityLogOpen = useAppStore((state) => state.activityLogOpen);
  const setActivityLogOpen = useAppStore((state) => state.setActivityLogOpen);

  return (
    <div className="h-screen flex flex-col bg-background">
      {/* Native Title Bar would be here (handled by WPF host) */}
      
      {/* Toolbar */}
      <Toolbar />
      
      {/* Tab Bar (only shown when tabs are open) */}
      {tabs.length > 0 && <TabBar />}
      
      {/* Main Content Area */}
      <div className="flex-1 flex overflow-hidden relative">
        {activityLogPosition === 'bottom' ? (
          <ResizablePanelGroup direction="vertical" className="flex-1">
            <ResizablePanel defaultSize={70} minSize={30}>
              <ScrollArea className="h-full">
                <Outlet />
              </ScrollArea>
            </ResizablePanel>
            <ResizableHandle />
            <ResizablePanel defaultSize={30} minSize={20} maxSize={50}>
              <ActivityLog />
            </ResizablePanel>
          </ResizablePanelGroup>
        ) : (
          <>
            <ScrollArea className="h-full flex-1">
              <Outlet />
            </ScrollArea>

            <div
              className={cn(
                'fixed inset-0 bg-black/40 transition-opacity z-40',
                activityLogOpen ? 'opacity-100' : 'opacity-0 pointer-events-none',
              )}
              onClick={() => setActivityLogOpen(false)}
            />
            <div
              className={cn(
                'fixed top-0 right-0 h-full w-[420px] max-w-[90vw] bg-background border-l shadow-lg z-50 transition-transform',
                activityLogOpen ? 'translate-x-0' : 'translate-x-full',
              )}
            >
              <ActivityLog />
            </div>
          </>
        )}
      </div>
    </div>
  );
}
