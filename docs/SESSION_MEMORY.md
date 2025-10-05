# Contextualizer Documentation Project - Session Memory

## 🎯 CURRENT MISSION
Create a comprehensive, single-page HTML documentation site for Contextualizer application with all features, examples, and guides in one massive `index.html` file.

## 📊 CURRENT STATUS
- ✅ **COMPLETED**: Modern CSS framework created (`docs/assets/css/main.css`) - GitHub-style design is EXCELLENT
- ✅ **COMPLETED**: JavaScript functionality created (`docs/assets/js/main.js`) - Interactive features working
- ❌ **WRONG APPROACH**: Created multiple HTML files (index.html, handlers.html, examples.html) - USER WANTS SINGLE FILE
- ✅ **COMPLETED**: Cleaned up old documentation files (.md files removed, README.md updated)

## 🔄 NEXT STEPS (For New Session)
1. **DECISION NEEDED**: Single massive `index.html` vs multiple HTML files
   - User preference: Let me decide the best approach
   - Current: Multiple files created (wrong approach according to user)
   - Recommendation: Single `index.html` with JavaScript section switching for better performance

2. **CONSOLIDATE DOCUMENTATION**: Merge all content into single comprehensive page:
   - Overview & Getting Started
   - Installation & Configuration  
   - All Handler Types (Regex, API, Database, File, Cron, Custom)
   - Complete Actions Reference
   - UI Controls & Tab Management
   - Real-world Examples & Use Cases
   - Advanced Topics (Plugin Development, Logging, Performance)
   - Troubleshooting & FAQ

## 🎨 DESIGN SYSTEM (KEEP THIS)
- **CSS Framework**: `docs/assets/css/main.css` - Modern GitHub-style design
- **JavaScript**: `docs/assets/js/main.js` - Navigation, search, interactive features
- **Color Scheme**: Professional blue (#0366d6) with light/dark theme support
- **Layout**: Sidebar navigation + main content area
- **Components**: Cards, code blocks, tables, grids, responsive design

## 📁 CURRENT FILE STRUCTURE
```
docs/
├── index.html (main page - needs consolidation)
├── handlers.html (merge into index.html)
├── examples.html (merge into index.html)
├── assets/
│   ├── css/main.css (KEEP - excellent design)
│   └── js/main.js (KEEP - good functionality)
└── SESSION_MEMORY.md (this file)
```

## 🚨 KEY POINTS FOR NEXT SESSION
1. **User feedback**: "Design is great but wrong approach"
2. **Task**: Create ONE comprehensive documentation page
3. **Keep**: CSS and JavaScript frameworks (they're excellent)
4. **Decision**: Single HTML vs multiple files - let AI decide best approach
5. **Goal**: Complete Contextualizer documentation with all features

## ⚠️ CRITICAL WORKING RULE
**DO NOT ADD ANYTHING TO HTML ON MY OWN!**
- ❌ Never add content to HTML based on my assumptions
- ✅ User will show me code paths and examples
- ✅ User will guide me on what areas to work on
- ✅ I will only control, fix, and add what user specifically requests
- ✅ I will follow user's direction exactly, not my imagination

## 📋 CONTENT TO INCLUDE (All in one place)
- **Getting Started**: Installation, quick setup, first handler
- **Core Concepts**: Clipboard monitoring, handler lifecycle, actions
- **Handler Types**: Complete guide for all 6+ handler types with examples
- **Actions Reference**: All available actions with parameters
- **UI Features**: Tab management, window controls, Chrome-like behavior
- **Configuration**: JSON structure, properties, best practices  
- **Examples**: Basic, advanced, real-world use cases
- **Advanced**: Plugin development, custom handlers, performance
- **API Reference**: Complete interface documentation
- **Troubleshooting**: Common issues and solutions

## 💡 RECOMMENDATION FOR NEXT SESSION
**Go with SINGLE `index.html` approach because:**
- Better for offline use
- Easier to search across all content
- Single file to maintain
- JavaScript can handle section navigation
- Better performance (no page loads)
- User can Ctrl+F across entire documentation

**Structure**: Use JavaScript section switching with sidebar navigation (like current design but all in one file)

## 📝 FINAL NOTES FOR NEW SESSION
**IMPORTANT REMINDERS:**

1. **WORKING METHOD**: 
   - User will guide me step by step
   - User will show code paths and examples
   - I will ONLY work on what user specifically requests
   - NO creative additions from my side!

2. **CURRENT ASSETS TO PRESERVE**:
   - `docs/assets/css/main.css` - Perfect GitHub-style design ✅
   - `docs/assets/js/main.js` - Great interactive features ✅
   - Design system is excellent, keep it exactly as is

3. **TASK AHEAD**:
   - Consolidate all HTML content into single `index.html`
   - User will show me which parts of the codebase to document
   - User will provide examples for each section
   - Follow user's guidance exactly

4. **WHAT USER LIKED**:
   - Modern design and styling
   - Professional look and feel
   - Interactive features

5. **WHAT USER DIDN'T LIKE**:
   - Multiple HTML files approach
   - Me adding content without guidance

**READY FOR USER DIRECTION IN NEW SESSION! 🚀**


---

## 🗓 Session Update (2025-10-05)

### ✅ Decisions & Completed Changes
- Language set to English for documentation.
- Installation updated:
  - GitHub clone/build path using `https://github.com/Murat7Ay/Contextualizer`.
  - Portable EXE from `\\ortak\\cashmanagement\\murat ay\\contextualizer` to `C:\\PortableApps\\Contextualizer`.
- Quick Start revised to “Exchange” flow with seeded Hello handler (`CreateSampleExchangeHandler`).
- Added Portable Directory Structure (from `CreatePortableDirectories()`): `Config`, `Data/Exchange`, `Data/Installed`, `Data/Logs`, `Plugins`, `Temp`.
- Configuration expanded:
  - Full Handler JSON schema (covers all `HandlerConfig` properties and nested types).
  - `handlers.json` example corrected to top-level `{ "handlers": [...] }` and removed screen/title for `show_notification`.
  - `appsettings.json` example aligned with `AppSettings.cs`.
  - Placeholder convention standardized to `$(key)` (e.g., `$(token)`).
- Hello sample handler now: regex, `markdown2`, `auto_focus_tab: true`, `bring_window_to_front: true`, `_formatted_output` usage.

### 🔀 Branch
- Created docs branch for these changes: `docs/single-index-docs`.

### 📌 Next Suggestions (pending user direction)
- Consolidate remaining HTML (handlers/examples) into single `index.html` sections.
- Add screenshots and minimal UI wiring docs for `SettingsWindow` and `LoggingSettingsWindow` references.

### 🖼 Image Placeholders & Replacement Notes
When images are needed, we will use temporary placeholders and you can replace them later:

- Installation structure screenshot
  - Placeholder: `https://placehold.co/800x400?text=Installation+Structure`
  - Target file to replace: `docs/assets/img/installation-structure.png`
  - Replace with: Screenshot of `C:\\PortableApps\\Contextualizer` folder structure (showing `Config`, `Data/Exchange`, `Data/Installed`, `Data/Logs`, `Plugins`, `Temp`).

- Exchange flow (Quick Start) diagram/screenshot
  - Placeholder: `https://placehold.co/1000x450?text=Exchange+Flow`
  - Target file to replace: `docs/assets/img/exchange-flow.png`
  - Replace with: Visual of seeding `sample-regex-handler.json` and running the Hello handler.

- Settings window
  - Placeholder: `https://placehold.co/900x600?text=SettingsWindow`
  - Target file to replace: `docs/assets/img/settings-window.png`
  - Replace with: Screenshot of `SettingsWindow` (paths, shortcut, timing, config system, network updates).

- Logging settings window
  - Placeholder: `https://placehold.co/900x600?text=LoggingSettingsWindow`
  - Target file to replace: `docs/assets/img/logging-settings-window.png`
  - Replace with: Screenshot of `LoggingSettingsWindow` (local logging, analytics, log stats).

Note: We will only insert these placeholders into `index.html` when/if we add a visual for the relevant section. Until then, this list tracks what to provide.

---

## 🔄 Refresh Start (2025-10-05)

- Docs consolidated into single `index.html`; visual sections added:
  - Execution Pipeline with detailed Dispatch flow (confirmation, inputs, seeders, multi-pass resolution, action conditions/inner_actions, output_format)
  - Dynamic Value Resolution (`$(key)`, `$config:`, `$func:`, `$file:`) and resolution order
  - Function tree, Condition operator grid, User Input flow + Toast demos
- Installation and Quick Start updated to Exchange flow and portable structure
- Configuration corrected (`handlers.json` top-level `{ handlers: [...] }`, `appsettings.json` aligned with `AppSettings.cs`)
- Placeholder convention enforced: `$(key)` (e.g., `$(token)`)
- Branch: `docs/single-index-docs`

Next: Build Handlers section with concrete handler types and code references.
- Action: enumerate `IHandler` implementations and document each handler type with example JSON and behavior
- Warning hook: if new handlers or plugins are added, revisit Function/Condition docs and Execution Pipeline notes
 
### 🎛 UI Controls Focus (2025-10-05)
- Add UI Controls deep-dive under `#ui-controls` in `docs/index.html` covering:
  - IDynamicScreen pattern and dynamic screen discovery via reflection
  - Screens implementing `IDynamicScreen`: `MarkdownViewer2` (markdown → HTML), `UrlViewer` (WebView2), `PlSqlEditor` (WebView2 + ACE editor), `JsonFormatterView`, `XmlFormatterView`
  - WebView2 usage: `EnsureCoreWebView2Async`, `WebMessageReceived`, virtual host mapping for local assets (e.g., `SetVirtualHostNameToFolderMapping("local", Assets/ace, Allow)`)
  - Markdown rendering: Markdig pipeline with advanced extensions; theming-aware HTML CSS injected
  - Theme system: Carbon design tokens + `ThemeManager`; `IThemeAware.OnThemeChanged` propagation from `MainWindow`
  - Exchange UI (`HandlerExchangeWindow`): list/filter/sort/install/update/remove handlers; window position persistence
  - Cron Manager (`CronManagerWindow`): list/filter/enable/disable/trigger; Carbon status styling

### ⚠ Notes & Warnings
- "shared_view_folder" term not found in codebase. If we plan a shared HTML view folder for WebView2 screens, we can adopt the same technique used by `PlSqlEditor` (virtual host mapping) to serve local static assets and templates. If you confirm, we will add a small section in docs and wire a constant like `shared_webview_profile`/`shared_view_folder` into context and settings.