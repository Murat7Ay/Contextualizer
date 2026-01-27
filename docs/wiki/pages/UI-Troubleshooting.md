# UI Troubleshooting

## Symptoms
- Blank UI or white screen
- Tabs not opening
- Actions not appearing in tabs
- Prompt dialogs not showing

## Checks
1. Ensure UI dist exists in Assets\Ui\dist (build step).
2. Confirm `screen_id` matches a supported screen.
3. Verify that `show_window` is executed with a valid context.
4. Check WebView2 initialization errors in host logs.

## Source References
- WPF host shell: [WpfInteractionApp/ReactShellWindow.xaml.cs](WpfInteractionApp/ReactShellWindow.xaml.cs)
- WebView interaction: [WpfInteractionApp/WebViewUserInteractionService.cs](WpfInteractionApp/WebViewUserInteractionService.cs)
- Tab routing: [Contextualizer.UI/src/app/stores/tabStore.ts](Contextualizer.UI/src/app/stores/tabStore.ts)