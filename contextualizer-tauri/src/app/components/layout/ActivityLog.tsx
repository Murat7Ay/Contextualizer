import { Trash2, Search, Filter, X } from 'lucide-react';
import { useActivityLogStore, type LogLevel } from '../../stores/activityLogStore';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import { ScrollArea } from '../ui/scroll-area';
import { cn } from '../ui/utils';
import { formatDistanceToNow } from 'date-fns';
import { useAppStore } from '../../stores/appStore';

const levelColors: Record<LogLevel, string> = {
  success: 'bg-green-500/10 text-green-700 dark:text-green-400',
  error: 'bg-red-500/10 text-red-700 dark:text-red-400',
  warning: 'bg-yellow-500/10 text-yellow-700 dark:text-yellow-400',
  info: 'bg-blue-500/10 text-blue-700 dark:text-blue-400',
  debug: 'bg-gray-500/10 text-gray-700 dark:text-gray-400',
  critical: 'bg-red-600/20 text-red-800 dark:text-red-300'
};

export function ActivityLog() {
  const { logs, filter, searchQuery, clearLogs, setFilter, setSearchQuery } = useActivityLogStore();
  const toggleActivityLog = useAppStore((s) => s.toggleActivityLog);

  const filteredLogs = logs.filter((log) => {
    const matchesFilter = filter === 'all' || log.level === filter;
    const matchesSearch = searchQuery === '' || 
      log.message.toLowerCase().includes(searchQuery.toLowerCase()) ||
      log.details?.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesFilter && matchesSearch;
  });

  return (
    <div className="h-full flex flex-col bg-background border-l overflow-hidden">
      {/* Header */}
      <div className="h-12 border-b px-4 flex items-center gap-2">
        <h2 className="text-sm font-semibold flex-1">Activity Log</h2>
        <Button variant="ghost" size="sm" onClick={toggleActivityLog} title="Close">
          <X className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" onClick={clearLogs} title="Clear logs">
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>

      {/* Filters */}
      <div className="p-3 border-b space-y-2">
        <div className="relative">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search logs..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-8 h-9"
          />
        </div>
        <Select value={filter} onValueChange={(value) => setFilter(value as LogLevel | 'all')}>
          <SelectTrigger className="h-9">
            <Filter className="h-4 w-4 mr-2" />
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Levels</SelectItem>
            <SelectItem value="success">Success</SelectItem>
            <SelectItem value="error">Error</SelectItem>
            <SelectItem value="warning">Warning</SelectItem>
            <SelectItem value="info">Info</SelectItem>
            <SelectItem value="debug">Debug</SelectItem>
            <SelectItem value="critical">Critical</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Log List */}
      <ScrollArea className="flex-1 min-h-0">
        <div className="p-3 space-y-2">
          {filteredLogs.length === 0 ? (
            <div className="text-center text-sm text-muted-foreground py-8">
              No activity logs yet
            </div>
          ) : (
            filteredLogs.map((log) => (
              <div
                key={log.id}
                className="p-3 rounded-md border bg-card hover:bg-accent/50 transition-colors"
              >
                <div className="flex items-start gap-2 mb-1">
                  <Badge
                    className={cn("text-xs capitalize", levelColors[log.level])}
                    variant="secondary"
                  >
                    {log.level}
                  </Badge>
                  <span className="text-xs text-muted-foreground">
                    {formatDistanceToNow(log.timestamp, { addSuffix: true })}
                  </span>
                </div>
                <p className="text-sm">{log.message}</p>
                {log.details && (
                  <details className="mt-2">
                    <summary className="text-xs text-muted-foreground cursor-pointer hover:text-foreground">
                      Show details
                    </summary>
                    <pre className="text-xs mt-2 p-2 bg-muted rounded overflow-x-auto">
                      {log.details}
                    </pre>
                  </details>
                )}
              </div>
            ))
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
