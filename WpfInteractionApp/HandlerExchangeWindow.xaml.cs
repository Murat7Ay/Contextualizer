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
using System.ComponentModel;

namespace WpfInteractionApp
{
    public partial class HandlerExchangeWindow : Window, INotifyPropertyChanged
    {
        private readonly HandlerExchangeService _exchangeService;
        private readonly ObservableCollection<HandlerPackage> _handlers;
        private string _searchTerm;
        private string _selectedTag;
        private HandlerPackage _selectedHandler;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<HandlerPackage> Handlers => _handlers;

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged(nameof(SearchTerm));
                _ = RefreshHandlersAsync();
            }
        }

        public string SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                OnPropertyChanged(nameof(SelectedTag));
                _ = RefreshHandlersAsync();
            }
        }

        public HandlerPackage SelectedHandler
        {
            get => _selectedHandler;
            set
            {
                _selectedHandler = value;
                OnPropertyChanged(nameof(SelectedHandler));
                OnPropertyChanged(nameof(CanInstall));
                OnPropertyChanged(nameof(CanUpdate));
                OnPropertyChanged(nameof(CanRemove));
            }
        }

        public bool CanInstall => SelectedHandler != null && !SelectedHandler.IsInstalled;
        public bool CanUpdate => SelectedHandler != null && SelectedHandler.IsInstalled && SelectedHandler.HasUpdate;
        public bool CanRemove => SelectedHandler != null && SelectedHandler.IsInstalled;

        public HandlerExchangeWindow()
        {
            InitializeComponent();

            var settingsService = ServiceLocator.Get<SettingsService>();
            var directoryManager = new PluginDirectoryManager(settingsService);
            _exchangeService = new HandlerExchangeService(settingsService, directoryManager);

            _handlers = new ObservableCollection<HandlerPackage>();
            DataContext = this;

            _ = LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            await RefreshHandlersAsync();
            var tags = await _exchangeService.GetAvailableTagsAsync();
            TagFilter.ItemsSource = tags;
        }

        private async Task RefreshHandlersAsync()
        {
            try
            {
                var handlers = await _exchangeService.ListAvailableHandlersAsync(SearchTerm, 
                    SelectedTag != null ? new[] { SelectedTag } : null);
                
                _handlers.Clear();
                foreach (var handler in handlers)
                {
                    _handlers.Add(handler);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing handlers: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (SelectedHandler == null) return;

            try
            {
                var result = await _exchangeService.InstallHandlerAsync(SelectedHandler.Id);
                if (result)
                {
                    await RefreshHandlersAsync();
                    MessageBox.Show("Handler installed successfully.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to install handler.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing handler: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedHandler == null) return;

            try
            {
                var result = await _exchangeService.UpdateHandlerAsync(SelectedHandler.Id);
                if (result)
                {
                    await RefreshHandlersAsync();
                    MessageBox.Show("Handler updated successfully.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to update handler.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating handler: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedHandler == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to remove this handler?",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _exchangeService.RemoveHandlerAsync(SelectedHandler.Id);
                    if (success)
                    {
                        await RefreshHandlersAsync();
                        MessageBox.Show("Handler removed successfully.", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to remove handler.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing handler: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshHandlersAsync();
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 