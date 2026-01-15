import { useEffect, useMemo, useState } from 'react';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { cn } from '../ui/utils';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { Search, X, Play, Pencil } from 'lucide-react';
import { useCronStore, type CronJobDto } from '../../stores/cronStore';
import { requestCronList, setCronJobEnabled, triggerCronJob, updateCronJob } from '../../host/webview2Bridge';
import { ScrollArea } from '../ui/scroll-area';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Label } from '../ui/label';

type StatusFilter = 'all' | 'active' | 'disabled';

function getLastResult(job: CronJobDto): { label: string; className: string } {
  if (!job.lastExecution) {
    return { label: 'Never run', className: 'text-muted-foreground' };
  }
  if (job.lastError && job.lastError.trim().length > 0) {
    return { label: 'Failed', className: 'text-red-600 dark:text-red-400' };
  }
  return { label: 'Success', className: 'text-green-600 dark:text-green-400' };
}

export function CronManager() {
  const addLog = useActivityLogStore((state) => state.addLog);

  const schedulerRunning = useCronStore((s) => s.isRunning);
  const jobs = useCronStore((s) => s.jobs);
  const loaded = useCronStore((s) => s.loaded);

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [editOpen, setEditOpen] = useState(false);
  const [editJobId, setEditJobId] = useState<string>('');
  const [editCronExpression, setEditCronExpression] = useState<string>('');
  const [editTimezone, setEditTimezone] = useState<string>('');

  useEffect(() => {
    requestCronList();
  }, []);

  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return jobs.filter((j) => {
      const matchesSearch =
        q.length === 0 ||
        j.jobId.toLowerCase().includes(q) ||
        j.cronExpression.toLowerCase().includes(q) ||
        (j.handlerName ?? '').toLowerCase().includes(q);
      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && j.enabled) ||
        (statusFilter === 'disabled' && !j.enabled);
      return matchesSearch && matchesStatus;
    });
  }, [jobs, searchQuery, statusFilter]);

  const toggleJob = (jobId: string) => {
    const job = jobs.find((j) => j.jobId === jobId);
    if (!job) return;
    setCronJobEnabled(jobId, !job.enabled);
    addLog('info', `Cron job '${job.jobId}' ${job.enabled ? 'disabled' : 'enabled'}`);
  };

  const triggerJob = (jobId: string) => {
    const job = jobs.find((j) => j.jobId === jobId);
    if (!job) return;
    triggerCronJob(jobId);
    addLog('info', `Manually triggered cron job: ${job.jobId}`);
  };

  const editJob = (jobId: string) => {
    const job = jobs.find((j) => j.jobId === jobId);
    if (!job) return;
    setEditJobId(job.jobId);
    setEditCronExpression(job.cronExpression ?? '');
    setEditTimezone(job.timezone ?? '');
    setEditOpen(true);
  };

  const clearFilters = () => {
    setSearchQuery('');
    setStatusFilter('all');
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Cron Job Manager</h1>
          <p className="text-sm text-muted-foreground">
            Monitor schedules, enable/disable jobs, and trigger runs manually.
          </p>
        </div>
        <Badge
          variant="secondary"
          className={cn(
            'text-xs px-3 py-1 rounded-full',
            schedulerRunning
              ? 'bg-green-500/10 text-green-700 dark:text-green-400'
              : 'bg-red-500/10 text-red-700 dark:text-red-400',
          )}
        >
          Scheduler: {schedulerRunning ? 'Running' : 'Stopped'}
        </Badge>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Search & Filters</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col md:flex-row gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search jobs..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-8 h-9"
            />
          </div>

          <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as StatusFilter)}>
            <SelectTrigger className="h-9 w-full md:w-[160px]">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Jobs</SelectItem>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="disabled">Disabled</SelectItem>
            </SelectContent>
          </Select>

          <Button variant="outline" className="h-9" onClick={clearFilters}>
            <X className="h-4 w-4 mr-2" />
            Clear
          </Button>
        </CardContent>
      </Card>

      <div className="text-sm text-muted-foreground">
        {jobs.length} job{jobs.length !== 1 ? 's' : ''} scheduled • {filtered.length} displayed
      </div>

      {!loaded && jobs.length === 0 ? (
        <div className="text-center text-sm text-muted-foreground py-12">Waiting for host…</div>
      ) : filtered.length === 0 ? (
        <div className="text-center text-sm text-muted-foreground py-12">No jobs match the current filters.</div>
      ) : (
        <div className="space-y-4">
          {filtered.map((job) => {
            const last = getLastResult(job);
            return (
              <Card key={job.jobId} className="hover:bg-accent/30 transition-colors">
                <CardHeader className="pb-3">
                  <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-3">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <CardTitle className="text-base">{job.jobId}</CardTitle>
                        <Badge
                          variant="secondary"
                          className={cn(
                            'text-xs',
                            job.enabled
                              ? 'bg-green-500/10 text-green-700 dark:text-green-400'
                              : 'bg-yellow-500/10 text-yellow-700 dark:text-yellow-400',
                          )}
                        >
                          {job.enabled ? 'Active' : 'Disabled'}
                        </Badge>
                        {job.handlerName && (
                          <Badge variant="outline" className="text-xs">
                            {job.handlerName}
                          </Badge>
                        )}
                      </div>
                      <div className="text-sm text-muted-foreground font-mono mt-1">{job.cronExpression}</div>
                    </div>
                    <div className="flex gap-2 md:justify-end">
                      <Button variant="outline" size="sm" onClick={() => editJob(job.jobId)}>
                        <Pencil className="h-4 w-4 mr-2" />
                        Edit
                      </Button>
                      <Button variant="outline" size="sm" onClick={() => toggleJob(job.jobId)}>
                        {job.enabled ? 'Disable' : 'Enable'}
                      </Button>
                      <Button size="sm" onClick={() => triggerJob(job.jobId)}>
                        <Play className="h-4 w-4 mr-2" />
                        Trigger
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">Next Execution</div>
                    <div className="font-mono text-sm">{job.nextExecution ?? '—'}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">Executions</div>
                    <div className="font-medium">{job.executionCount}</div>
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground mb-1">Last Result</div>
                    <div className={cn('font-semibold', last.className)}>{last.label}</div>
                    {job.lastError && (
                      <div className="text-xs text-muted-foreground mt-1 truncate" title={job.lastError}>
                        {job.lastError}
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Edit cron job</DialogTitle>
            <DialogDescription>
              Update the cron expression (and optional timezone) for this job.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm font-semibold">Job ID</Label>
              <Input value={editJobId} disabled />
            </div>

            <div className="space-y-2">
              <Label className="text-sm font-semibold">Cron expression</Label>
              <Input
                value={editCronExpression}
                onChange={(e) => setEditCronExpression(e.target.value)}
                placeholder="0 8 * * MON-FRI"
                className="font-mono"
              />
              <div className="text-xs text-muted-foreground">
                Example: <span className="font-mono">0 8 * * MON-FRI</span>
              </div>
            </div>

            <div className="space-y-2">
              <Label className="text-sm font-semibold">Timezone (optional)</Label>
              <Input
                value={editTimezone}
                onChange={(e) => setEditTimezone(e.target.value)}
                placeholder="Europe/Istanbul"
                className="font-mono"
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                const ok = updateCronJob(editJobId, editCronExpression.trim(), editTimezone.trim() || undefined);
                if (!ok) {
                  addLog('error', 'Failed to send cron update request (host not available?)');
                  return;
                }
                setEditOpen(false);
              }}
              disabled={!editJobId || !editCronExpression.trim()}
            >
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      </div>
    </ScrollArea>
  );
}
