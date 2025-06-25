using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Markup;
using System.Linq;
using System.Diagnostics;
using Contextualizer.Core;

namespace WpfInteractionApp.Services
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private readonly Dictionary<string, ResourceDictionary> _themeResources;
        private string _currentTheme;
        private const string DefaultTheme = "Light";

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event EventHandler<string> ThemeChanged = delegate { };

        private ThemeManager()
        {
            _themeResources = new Dictionary<string, ResourceDictionary>();
            LoadThemeResources();
            _currentTheme = DefaultTheme;
        }

        private void LoadThemeResources()
        {
            try
            {
                // Load Light theme resources
                var lightColors = new ResourceDictionary
                {
                    Source = new Uri("/WpfInteractionApp;component/Themes/LightCarbonColors.xaml", UriKind.Relative)
                };
                _themeResources["Light"] = lightColors;

                // Load Dark theme resources
                var darkColors = new ResourceDictionary
                {
                    Source = new Uri("/WpfInteractionApp;component/Themes/CarbonColors.xaml", UriKind.Relative)
                };
                _themeResources["Dark"] = darkColors;

                // Load Dim theme resources
                var dimColors = new ResourceDictionary
                {
                    Source = new Uri("/WpfInteractionApp;component/Themes/DimCarbonColors.xaml", UriKind.Relative)
                };
                _themeResources["Dim"] = dimColors;

                Debug.WriteLine("Theme resources loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading theme resources: {ex}");
                MessageBox.Show($"Error loading theme resources: {ex.Message}", "Theme Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ApplyTheme(string themeName)
        {
            try
            {
                Debug.WriteLine($"Applying theme: {themeName}");
                
                if (!_themeResources.ContainsKey(themeName))
                {
                    throw new ArgumentException($"Theme '{themeName}' not found.");
                }

                var app = Application.Current;
                
                // Remove old theme dictionary
                var oldThemeDict = app.Resources.MergedDictionaries
                    .FirstOrDefault(d =>
                    {
                        if (d.Source == null) return false;
                        var sourceString = d.Source.ToString();
                        return sourceString.Contains("CarbonColors.xaml", StringComparison.OrdinalIgnoreCase) ||
                               sourceString.Contains("LightCarbonColors.xaml", StringComparison.OrdinalIgnoreCase) ||
                               sourceString.Contains("DimCarbonColors.xaml", StringComparison.OrdinalIgnoreCase);
                    });

                if (oldThemeDict != null)
                {
                    Debug.WriteLine($"Removing old theme dictionary: {oldThemeDict.Source}");
                    app.Resources.MergedDictionaries.Remove(oldThemeDict);
                }

                // Add new theme dictionary
                var newThemeDict = _themeResources[themeName];
                Debug.WriteLine($"Adding new theme dictionary: {newThemeDict.Source}");
                app.Resources.MergedDictionaries.Add(newThemeDict);

                // Stil değişikliği için yeni yaklaşım
                var stylesDict = app.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("CarbonStyles.xaml") == true);

                if (stylesDict != null)
                {
                    // Stilleri yeniden yükle
                    app.Resources.MergedDictionaries.Remove(stylesDict);
                    var newStylesDict = new ResourceDictionary 
                    { 
                        Source = new Uri("/WpfInteractionApp;component/Themes/CarbonStyles.xaml", UriKind.Relative) 
                    };
                    app.Resources.MergedDictionaries.Add(newStylesDict);
                }
                
                _currentTheme = themeName;
                
                // Save theme preference to AppSettings
                try
                {
                    var settingsService = ServiceLocator.Get<SettingsService>();
                    settingsService.Settings.UISettings.Theme = themeName;
                    settingsService.SaveSettings();
                }
                catch (Exception settingsEx)
                {
                    Debug.WriteLine($"Error saving theme preference: {settingsEx}");
                }

                Debug.WriteLine($"Theme changed to: {themeName}");
                ThemeChanged(this, themeName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying theme: {ex}");
                MessageBox.Show($"Error applying theme: {ex.Message}", "Theme Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CycleTheme()
        {
            var themes = new[] { "Light", "Dark", "Dim" };
            var currentIndex = Array.IndexOf(themes, _currentTheme);
            var nextIndex = (currentIndex + 1) % themes.Length;
            ApplyTheme(themes[nextIndex]);
        }

        public string CurrentTheme => _currentTheme;

        public bool IsDarkTheme => _currentTheme == "Dark" || _currentTheme == "Dim";
    }
}