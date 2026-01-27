# Lifecycle & Startup

This page describes application startup and shutdown sequences.

## Startup Order
1. Settings service
2. Configuration service (config.ini / secrets.ini)
3. Logging service
4. Theme initialization
5. WPF host + WebView2
6. User interaction services (WebView/Native)
7. Cron scheduler
8. Handler exchange service
9. Handler manager + clipboard listener
10. MCP server (if enabled)

## Shutdown Order
- Save settings
- Stop MCP server
- Dispose handler manager

## Source References
- App lifecycle: [WpfInteractionApp/App.xaml.cs](WpfInteractionApp/App.xaml.cs)