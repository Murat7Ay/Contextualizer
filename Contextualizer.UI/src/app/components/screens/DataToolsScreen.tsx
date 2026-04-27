import { useSearchParams } from 'react-router-dom';
import { Database, FileCode2 } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { DataToolsManager } from './DataToolsManager';
import { RawSqlToolsManager } from './RawSqlToolsManager';

type DataToolsTab = 'registry' | 'raw-sql';

function normalizeTab(value: string | null): DataToolsTab {
  return value === 'raw-sql' ? 'raw-sql' : 'registry';
}

export function DataToolsScreen() {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = normalizeTab(searchParams.get('tab'));

  const handleTabChange = (value: string) => {
    const nextTab = normalizeTab(value);
    const nextParams = new URLSearchParams(searchParams);
    nextParams.set('tab', nextTab);
    setSearchParams(nextParams, { replace: true });
  };

  return (
    <Tabs value={activeTab} onValueChange={handleTabChange} className="h-full gap-0">
      <div className="border-b bg-card/70 px-6 py-4 backdrop-blur">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h1 className="text-[28px] font-semibold">Data Tools</h1>
            <p className="text-sm text-muted-foreground">
              Registry-backed tools and fixed-connection raw SQL MCP tools live here.
            </p>
          </div>

          <TabsList>
            <TabsTrigger value="registry">
              <Database className="h-4 w-4" />
              Registry Tools
            </TabsTrigger>
            <TabsTrigger value="raw-sql">
              <FileCode2 className="h-4 w-4" />
              Raw SQL Tools
            </TabsTrigger>
          </TabsList>
        </div>
      </div>

      <TabsContent value="registry" className="h-full overflow-hidden">
        <DataToolsManager />
      </TabsContent>

      <TabsContent value="raw-sql" className="h-full overflow-hidden">
        <RawSqlToolsManager />
      </TabsContent>
    </Tabs>
  );
}