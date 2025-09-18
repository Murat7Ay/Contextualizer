# 📦 Contextualizer Portable Application

## 🎯 **Ready to Use - No Configuration Needed**

Contextualizer works immediately after launch with an organized portable directory structure. Everything is automatic!

### **📁 Directory Structure**

The application automatically creates and manages this structure:

```
C:\PortableApps\Contextualizer\
├── 📁 Config\
│   ├── appsettings.json          # Application settings
│   └── handlers.json             # Handler configurations
├── 📁 Data\
│   ├── 📁 Exchange\              # Handler marketplace/templates
│   │   └── sample-regex-handler.json
│   ├── 📁 Installed\             # Installed handler metadata
│   └── 📁 Logs\                  # Application logs
│       ├── error_2025-09-18.log
│       ├── info_2025-09-18.log
│       └── warning_2025-09-18.log
├── 📁 Plugins\                   # Custom plugins directory
└── 📁 Temp\                      # Temporary files
```

## 🚀 **First Launch Experience**

### **✅ Everything Happens Automatically**
1. **Launch Application**: Simply run `Contextualizer.exe`
2. **Files Created**: All directories and files appear automatically
3. **Welcome Handler**: A sample handler is ready to use
4. **Sample Template**: An email extraction handler template is available in the marketplace

### **✅ What Gets Created Automatically**

#### **📄 Default handlers.json**
```json
{
  "handlers": [
    {
      "name": "Welcome Handler",
      "type": "manual",
      "screen_id": "welcome_screen",
      "title": "Welcome to Contextualizer!",
      "description": "This is a sample handler to get you started.",
      "actions": [
        {
          "name": "show_notification",
          "message": "Welcome to Contextualizer! 🎉\n\nThis portable installation is ready to use.",
          "title": "Welcome",
          "duration": 10
        }
      ]
    }
  ]
}
```

#### **📄 Default appsettings.json**
```json
{
  "handlers_file_path": "C:\\PortableApps\\Contextualizer\\Config\\handlers.json",
  "plugins_directory": "C:\\PortableApps\\Contextualizer\\Plugins",
  "exchange_directory": "C:\\PortableApps\\Contextualizer\\Data\\Exchange",
  "keyboard_shortcut": {
    "modifier_keys": ["Ctrl"],
    "key": "W"
  },
  "clipboard_wait_timeout": 5,
  "window_activation_delay": 100,
  "clipboard_clear_delay": 800,
  "logging_settings": {
    "enable_local_logging": true,
    "enable_usage_tracking": true,
    "minimum_log_level": "Info",
    "local_log_path": "C:\\PortableApps\\Contextualizer\\Data\\Logs"
  }
}
```

#### **📄 Sample Exchange Handler**
A ready-to-install email extraction handler template is automatically created in the Exchange directory.

## 🔧 **Benefits of Portable Application**

### **✅ No Configuration Needed**
- No manual file creation required
- No path configuration needed
- Works immediately after first launch

### **✅ Self-Contained**
- All data in one directory tree
- Easy to backup entire application
- Easy to move between machines
- No registry dependencies

### **✅ Development Friendly**
- Clear separation of concerns
- Organized file structure
- Easy debugging with dedicated logs directory
- Plugin development support

### **✅ User Friendly**
- Predictable file locations
- Easy to find configuration files
- Clear directory purposes
- Sample content to get started

## 📋 **Directory Purposes**

| Directory | Purpose | Auto-Created | Contains |
|-----------|---------|--------------|----------|
| `Config/` | Application configuration | ✅ | Settings, handlers |
| `Data/Exchange/` | Handler marketplace | ✅ | Installable handlers |
| `Data/Installed/` | Installation metadata | ✅ | Installed handler info |
| `Data/Logs/` | Application logs | ✅ | Error, info, debug logs |
| `Plugins/` | Custom plugins | ✅ | Plugin assemblies |
| `Temp/` | Temporary files | ✅ | Cache, temp data |

## 🛠️ **Customization**

While the portable application works out-of-the-box, you can customize paths by editing `appsettings.json`:

```json
{
  "handlers_file_path": "C:\\YourCustomPath\\handlers.json",
  "plugins_directory": "C:\\YourCustomPath\\Plugins",
  "exchange_directory": "C:\\YourCustomPath\\Exchange"
}
```

## 🚨 **Error Handling**

If directory creation fails:
- Application continues to run
- Falls back to current directory for logs
- Error details logged to console
- User can manually create directories

## 📊 **Logging**

The portable application includes comprehensive logging:
- **Error Logs**: `Data/Logs/error_YYYY-MM-DD.log`
- **Info Logs**: `Data/Logs/info_YYYY-MM-DD.log`
- **Debug Logs**: `Data/Logs/debug_YYYY-MM-DD.log`
- **Automatic Rotation**: Old logs cleaned up automatically
- **Performance Tracking**: Handler execution metrics

## 🎉 **Ready to Use!**

After first launch, you can:
1. **Test the Welcome Handler**: Use manual handlers menu
2. **Install Sample Template**: Visit the marketplace
3. **Create Custom Handlers**: Edit `handlers.json`
4. **View Activity**: Check the activity log panel
5. **Monitor System**: Check logs in `Data/Logs/`

The portable application ensures Contextualizer works perfectly from the first launch! 🚀
