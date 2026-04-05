import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from './components/ThemeProvider';
import { MainLayout } from './components/layout/MainLayout';
import { Dashboard } from './components/screens/Dashboard';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<MainLayout />}>
              <Route path="/" element={<Dashboard />} />
              <Route path="/handlers" element={<Placeholder title="Handler Management" />} />
              <Route path="/settings" element={<Placeholder title="Settings" />} />
              <Route path="/cron" element={<Placeholder title="Cron Manager" />} />
              <Route path="/marketplace" element={<Placeholder title="Handler Exchange" />} />
              <Route path="/ai-skills" element={<Placeholder title="AI Skills Hub" />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

function Placeholder({ title }: { title: string }) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
      <h2 className="text-2xl font-semibold">{title}</h2>
      <p className="text-muted-foreground">This screen will be connected to the Rust backend.</p>
    </div>
  );
}
