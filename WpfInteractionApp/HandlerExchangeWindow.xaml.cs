using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Contextualizer.PluginContracts.Interfaces;
using Contextualizer.PluginContracts.Models;
using Contextualizer.Core.Services;
using WpfInteractionApp.Services;
using Contextualizer.Core;

namespace WpfInteractionApp
{
    public partial class HandlerExchangeWindow : Window
    {
        private readonly IHandlerExchange _handlerExchange;
        private ObservableCollection<HandlerPackage> _handlers;
        private ObservableCollection<string> _tags;
        private HandlerPackage _selectedHandler;

        public HandlerExchangeWindow()
        {
            InitializeComponent();
            
            _handlerExchange = new FileHandlerExchange();
            _handlers = new ObservableCollection<HandlerPackage>();
            _tags = new ObservableCollection<string>();

            // ✨ Use ItemsControl instead of ListView
            HandlersItemsControl.ItemsSource = _handlers;
            TagFilter.ItemsSource = _tags;
            
            Loaded += HandlerExchangeWindow_Loaded;
            
            // Restore window position and setup closing event
            RestoreWindowPosition();
            Closing += HandlerExchangeWindow_Closing;
        }

        private async void HandlerExchangeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshHandlers();
        }

        private async Task RefreshHandlers()
        {
            try
            {
                var handlers = await _handlerExchange.ListAvailableHandlersAsync();
                _handlers.Clear();
                foreach (var handler in handlers)
                {
                    _handlers.Add(handler);
                }

                // Etiketleri güncelle
                var allTags = handlers.SelectMany(h => h.Tags).Distinct().OrderBy(t => t);
                _tags.Clear();
                foreach (var tag in allTags)
                {
                    _tags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading handlers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_handlers == null) return;

            var searchText = SearchBox.Text.ToLower();
            var selectedTag = TagFilter.SelectedItem as string;
            var sortBy = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredHandlers = _handlers.Where(h =>
                (string.IsNullOrEmpty(searchText) || h.Name.ToLower().Contains(searchText) || h.Description.ToLower().Contains(searchText)) &&
                (selectedTag == null || h.Tags.Contains(selectedTag))
            );

            switch (sortBy)
            {
                case "Name":
                    filteredHandlers = filteredHandlers.OrderBy(h => h.Name);
                    break;
                case "Version":
                    filteredHandlers = filteredHandlers.OrderByDescending(h => h.Version);
                    break;
                case "Author":
                    filteredHandlers = filteredHandlers.OrderBy(h => h.Author);
                    break;
            }

            var selectedId = _selectedHandler?.Id;
            HandlersItemsControl.ItemsSource = filteredHandlers;
            
            if (selectedId != null)
            {
                var selectedItem = filteredHandlers.FirstOrDefault(h => h.Id == selectedId);
                if (selectedItem != null)
                {
                    // ItemsControl doesn't have SelectedItem, selection is handled differently
                }
            }
        }

        private void TagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchBox_TextChanged(sender, null);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchBox_TextChanged(sender, null);
        }

        // Selection is now handled by individual card buttons, no need for list selection
        private void HandlerCard_SelectionChanged(HandlerPackage handler)
        {
            _selectedHandler = handler;
            // Note: Individual card buttons handle their own state, no need to update bottom buttons
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            HandlerPackage handler = null;
            
            // Check if this is from a card button (has DataContext) or bottom button (uses _selectedHandler)
            if (sender is Button button && button.DataContext is HandlerPackage cardHandler)
            {
                handler = cardHandler;
            }
            else if (_selectedHandler != null)
            {
                handler = _selectedHandler;
            }
            
            if (handler == null) return;

            try
            {
                await _handlerExchange.InstallHandlerAsync(handler.Id);
                await RefreshHandlers();
                MessageBox.Show("Handler installed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing handler: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            HandlerPackage handler = null;
            
            // Check if this is from a card button (has DataContext) or bottom button (uses _selectedHandler)
            if (sender is Button button && button.DataContext is HandlerPackage cardHandler)
            {
                handler = cardHandler;
            }
            else if (_selectedHandler != null)
            {
                handler = _selectedHandler;
            }
            
            if (handler == null) return;

            try
            {
                await _handlerExchange.UpdateHandlerAsync(handler.Id);
                await RefreshHandlers();
                MessageBox.Show("Handler updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating handler: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            HandlerPackage handler = null;
            
            // Check if this is from a card button (has DataContext) or bottom button (uses _selectedHandler)
            if (sender is Button button && button.DataContext is HandlerPackage cardHandler)
            {
                handler = cardHandler;
            }
            else if (_selectedHandler != null)
            {
                handler = _selectedHandler;
            }
            
            if (handler == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to remove '{handler.Name}' handler?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _handlerExchange.RemoveHandlerAsync(handler.Id);
                    await RefreshHandlers();
                    MessageBox.Show("Handler removed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing handler: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshHandlers();
        }

        private void AddHandler_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement new handler creation
            MessageBox.Show("This feature is not implemented yet.", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RestoreWindowPosition()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowPos = settingsService.Settings.WindowSettings.ExchangeWindow;
                
                if (!double.IsNaN(windowPos.Left) && !double.IsNaN(windowPos.Top))
                {
                    Left = windowPos.Left;
                    Top = windowPos.Top;
                }
                
                if (!double.IsNaN(windowPos.Width) && windowPos.Width > 0)
                {
                    Width = windowPos.Width;
                }
                
                if (!double.IsNaN(windowPos.Height) && windowPos.Height > 0)
                {
                    Height = windowPos.Height;
                }
            }
            catch (InvalidOperationException)
            {
                // ServiceLocator not initialized or SettingsService not registered
            }
        }

        private void HandlerExchangeWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowPos = settingsService.Settings.WindowSettings.ExchangeWindow;
                
                // Only update in memory, don't save to disk yet
                if (IsValidPosition(Left))
                    windowPos.Left = Left;
                if (IsValidPosition(Top))
                    windowPos.Top = Top;
                if (IsValidSize(Width))
                    windowPos.Width = Width;
                if (IsValidSize(Height))
                    windowPos.Height = Height;
                
                // Settings will be saved when application exits
            }
            catch (InvalidOperationException)
            {
                // ServiceLocator not initialized or SettingsService not registered
            }
        }

        private static bool IsValidPosition(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidSize(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        }

        // ✨ New Event Handler for Details Button
        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is HandlerPackage handler)
            {
                // Show handler details in a popup or dialog
                var detailsMessage = $"Handler: {handler.Name}\n" +
                                   $"Version: {handler.Version}\n" +
                                   $"Author: {handler.Author}\n" +
                                   $"Description: {handler.Description}\n" +
                                   $"Tags: {string.Join(", ", handler.Tags ?? new string[0])}\n" +
                                   $"Dependencies: {string.Join(", ", handler.Dependencies ?? new string[0])}";
                
                MessageBox.Show(detailsMessage, "Handler Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ✨ New Event Handler for Card Click (Selection)
        private void HandlerCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HandlerPackage handler)
            {
                HandlerCard_SelectionChanged(handler);
            }
        }
    }
} 