import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from './components/ThemeProvider';
import { MainLayout } from './components/layout/MainLayout';
import { Dashboard } from './components/screens/Dashboard';
import { SettingsScreen } from './components/screens/Settings';
import { HandlerManagement } from './components/screens/HandlerManagement';
import { HandlerExchange } from './components/screens/HandlerExchange';
import { CronManager } from './components/screens/CronManager';
import { DynamicTabScreen } from './components/screens/DynamicTabScreen';
import { Toaster } from 'sonner';
import { initHostBridge } from './host/initHostBridge';
import { HostBridgeListener } from './host/HostBridgeListener';
import { HostPromptLayer } from './host/HostPromptLayer';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

export default function App() {
  useEffect(() => initHostBridge(), []);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <BrowserRouter>
          <HostBridgeListener />
          <HostPromptLayer />
          <Routes>
            <Route path="/" element={<MainLayout />}>
              <Route index element={<Dashboard />} />
              {/* WebView2 loads https://.../index.html; redirect so the app isn't blank on startup */}
              <Route path="index.html" element={<Navigate to="/" replace />} />
              <Route path="settings/*" element={<SettingsScreen />} />
              <Route path="handlers" element={<HandlerManagement />} />
              <Route path="marketplace" element={<HandlerExchange />} />
              <Route path="cron" element={<CronManager />} />
              <Route path="tab/:screenId/:title" element={<DynamicTabScreen />} />
              {/* Fallback: never show a blank screen */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </BrowserRouter>
        <Toaster position="top-right" richColors />
      </ThemeProvider>
    </QueryClientProvider>
  );
}
