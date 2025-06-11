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
    public partial class HandlerMarketplaceWindow : Window
    {
        private readonly IHandlerMarketplace _marketplace;
        private readonly ISettingsService _settingsService;
        private ObservableCollection<HandlerPackage> _handlers;
        private ObservableCollection<string> _tags;
        private HandlerPackage _selectedHandler;

        public HandlerMarketplaceWindow()
        {
            InitializeComponent();
            
            _marketplace = new FileHandlerMarketplace();
            _handlers = new ObservableCollection<HandlerPackage>();
            _tags = new ObservableCollection<string>();
            
            HandlersList.ItemsSource = _handlers;
            TagFilter.ItemsSource = _tags;
            
            Loaded += HandlerMarketplaceWindow_Loaded;
        }

        private async void HandlerMarketplaceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshHandlers();
        }

        private async Task RefreshHandlers()
        {
            try
            {
                var handlers = await _marketplace.ListAvailableHandlersAsync();
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
                await _marketplace.InstallHandlerAsync(_selectedHandler.Id);
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
                await _marketplace.UpdateHandlerAsync(_selectedHandler.Id);
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
                    await _marketplace.RemoveHandlerAsync(_selectedHandler.Id);
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
    }
} 