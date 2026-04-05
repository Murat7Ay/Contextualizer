import { useEffect } from 'react';
import { useAppStore } from '../stores/appStore';

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const theme = useAppStore((state) => state.theme);

  useEffect(() => {
    const root = document.documentElement;
    root.classList.remove('light', 'dark');
    
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.add('light');
    }

    // NOTE: Do NOT send set_theme to WPF host here!
    // This effect runs on mount with the default theme ('light'), which would
    // override the user's saved theme in WPF. Theme sync to host is handled
    // explicitly in Settings.tsx when the user changes the theme.
  }, [theme]);

  return <>{children}</>;
}
