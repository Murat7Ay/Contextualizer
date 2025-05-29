using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using WpfInteractionApp.Properties;

namespace WpfInteractionApp.Services
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public class ThemeService
    {
        private readonly Application _application;
        private readonly Dictionary<ThemeType, Uri> _themeResources;

        public event EventHandler<ThemeType>? ThemeChanged;

        public ThemeType CurrentTheme { get; private set; }

        public ThemeService(Application application)
        {
            _application = application;
            _themeResources = new Dictionary<ThemeType, Uri>
            {
                { ThemeType.Dark, new Uri("/WpfInteractionApp;component/Themes/CarbonColors.xaml", UriKind.Relative) },
                { ThemeType.Light, new Uri("/WpfInteractionApp;component/Themes/LightCarbonColors.xaml", UriKind.Relative) }
            };

            // Load saved theme from settings
            try
            {
                CurrentTheme = Enum.Parse<ThemeType>(WpfInteractionApp.Properties.Settings.Default.Theme, true);
            }
            catch
            {
                CurrentTheme = ThemeType.Dark;
                WpfInteractionApp.Properties.Settings.Default.Theme = CurrentTheme.ToString();
                WpfInteractionApp.Properties.Settings.Default.Save();
            }
            Debug.WriteLine($"Initial theme loaded from settings: {CurrentTheme}");
        }

        public void SwitchTheme()
        {
            var newTheme = CurrentTheme == ThemeType.Dark ? ThemeType.Light : ThemeType.Dark;
            Debug.WriteLine($"Switching theme from {CurrentTheme} to {newTheme}");
            ApplyTheme(newTheme);
        }

        private void ApplyTheme(ThemeType theme)
        {
            Debug.WriteLine($"ApplyTheme called with theme: {theme}");

            if (!_themeResources.TryGetValue(theme, out var themeUri))
            {
                Debug.WriteLine($"Theme URI not found for theme: {theme}");
                return;
            }

            Debug.WriteLine($"Theme URI: {themeUri}");

            try
            {
                // Find and remove any existing theme dictionaries
                var themeDicts = _application.Resources.MergedDictionaries
                    .Where(d => d.Source?.OriginalString.Contains("CarbonColors.xaml") == true ||
                               d.Source?.OriginalString.Contains("LightCarbonColors.xaml") == true)
                    .ToList();

                Debug.WriteLine($"Found {themeDicts.Count} existing theme dictionaries");

                foreach (var dict in themeDicts)
                {
                    Debug.WriteLine($"Removing dictionary: {dict.Source?.OriginalString}");
                    _application.Resources.MergedDictionaries.Remove(dict);
                }

                // Add new theme at the beginning (before styles)
                var newDict = new ResourceDictionary { Source = themeUri };
                
                // Find the styles dictionary to insert before it
                var stylesDict = _application.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("CarbonStyles.xaml") == true);

                if (stylesDict != null)
                {
                    var index = _application.Resources.MergedDictionaries.IndexOf(stylesDict);
                    _application.Resources.MergedDictionaries.Insert(index, newDict);

                    // Force styles to reload by removing and re-adding them
                    _application.Resources.MergedDictionaries.Remove(stylesDict);
                    var newStylesDict = new ResourceDictionary { Source = stylesDict.Source };
                    _application.Resources.MergedDictionaries.Insert(index + 1, newStylesDict);
                }
                else
                {
                    // If styles not found, just add at the beginning
                    _application.Resources.MergedDictionaries.Insert(0, newDict);
                }

                Debug.WriteLine("New theme dictionary added successfully");

                CurrentTheme = theme;

                // Save theme setting
                WpfInteractionApp.Properties.Settings.Default.Theme = theme.ToString();
                WpfInteractionApp.Properties.Settings.Default.Save();
                Debug.WriteLine($"Theme setting saved: {theme}");

                // Notify subscribers
                ThemeChanged?.Invoke(this, theme);
                Debug.WriteLine("Theme change completed successfully");

                // Force a refresh of all windows
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsLoaded)
                    {
                        var content = window.Content;
                        window.Content = null;
                        window.Content = content;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying theme: {ex}");
                throw;
            }
        }
    }
} 