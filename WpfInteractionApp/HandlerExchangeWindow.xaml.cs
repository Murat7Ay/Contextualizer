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
            
            HandlersList.ItemsSource = _handlers;
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
            HandlersList.ItemsSource = filteredHandlers;
            
            if (selectedId != null)
            {
                var selectedItem = filteredHandlers.FirstOrDefault(h => h.Id == selectedId);
                if (selectedItem != null)
                {
                    HandlersList.SelectedItem = selectedItem;
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

        private void HandlersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedHandler = HandlersList.SelectedItem as HandlerPackage;
            if (_selectedHandler != null)
            {
                InstallButton.IsEnabled = !_selectedHandler.IsInstalled;
                UpdateButton.IsEnabled = _selectedHandler.IsInstalled && _selectedHandler.HasUpdate;
                RemoveButton.IsEnabled = _selectedHandler.IsInstalled;
            }
            else
            {
                InstallButton.IsEnabled = false;
                UpdateButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
            }
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHandler == null) return;

            try
            {
                await _handlerExchange.InstallHandlerAsync(_selectedHandler.Id);
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
            if (_selectedHandler == null) return;

            try
            {
                await _handlerExchange.UpdateHandlerAsync(_selectedHandler.Id);
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
            if (_selectedHandler == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to remove this handler?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _handlerExchange.RemoveHandlerAsync(_selectedHandler.Id);
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
    }
} 