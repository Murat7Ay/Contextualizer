using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class HandlerManagementWindow : Window
    {
        private List<HandlerConfig> _handlers;
        private readonly HandlerManager _handlerManager;

        public HandlerManagementWindow()
        {
            InitializeComponent();
            
            // Get HandlerManager from ServiceLocator
            _handlerManager = ServiceLocator.Get<HandlerManager>();
            
            LoadHandlers();
        }

        private void LoadHandlers()
        {
            try
            {
                // Get all handler configurations
                _handlers = _handlerManager.GetAllHandlerConfigs();
                
                // Sort by name
                _handlers = _handlers.OrderBy(h => h.Name).ToList();
                
                // Bind to UI
                HandlersListBox.ItemsSource = _handlers;
                
                // Update statistics
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading handlers: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            TotalHandlersText.Text = _handlers.Count.ToString();
            EnabledHandlersText.Text = _handlers.Count(h => h.Enabled).ToString();
            DisabledHandlersText.Text = _handlers.Count(h => !h.Enabled).ToString();
        }

        private void ToggleHandler_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string handlerName)
            {
                var handler = _handlers.FirstOrDefault(h => h.Name == handlerName);
                if (handler == null)
                {
                    MessageBox.Show("Handler not found", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Toggle the enabled state
                bool newState = !handler.Enabled;
                
                // Update via HandlerManager (this will save to JSON)
                bool success = _handlerManager.UpdateHandlerEnabledState(handlerName, newState);
                
                if (success)
                {
                    // Update local list
                    handler.Enabled = newState;
                    
                    // Refresh the UI
                    HandlersListBox.ItemsSource = null;
                    HandlersListBox.ItemsSource = _handlers;
                    
                    // Update statistics
                    UpdateStatistics();
                    
                    // Show feedback
                    string status = newState ? "enabled" : "disabled";
                    MessageBox.Show($"Handler '{handlerName}' has been {status}", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to update handler state", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

