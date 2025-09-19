using Contextualizer.PluginContracts;
using Contextualizer.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class CronManagerWindow : Window
    {
        private readonly ICronService _cronService;
        private readonly ObservableCollection<CronJobViewModel> _jobs;
        private readonly ObservableCollection<CronJobViewModel> _filteredJobs;

        public CronManagerWindow()
        {
            InitializeComponent();
            
            _cronService = ServiceLocator.Get<ICronService>();
            _jobs = new ObservableCollection<CronJobViewModel>();
            _filteredJobs = new ObservableCollection<CronJobViewModel>();
            
            JobsItemsControl.ItemsSource = _filteredJobs;
            DataContext = this;
            
            LoadJobs();
            UpdateSchedulerStatus();
        }

        public ICommand ToggleJobCommand => new RelayCommand<string>(ToggleJob);
        public ICommand TriggerJobCommand => new RelayCommand<string>(TriggerJob);
        public ICommand EditJobCommand => new RelayCommand<string>(EditJob);
        public ICommand DeleteJobCommand => new RelayCommand<string>(DeleteJob);

        private void LoadJobs()
        {
            _jobs.Clear();
            
            var scheduledJobs = _cronService.GetScheduledJobs();
            foreach (var job in scheduledJobs.Values)
            {
                _jobs.Add(new CronJobViewModel(job));
            }
            
            ApplyFilters();
            UpdateJobCount();
        }

        private void UpdateSchedulerStatus()
        {
            var isRunning = _cronService.IsRunning;
            SchedulerStatusText.Text = $"Scheduler: {(isRunning ? "Running" : "Stopped")}";
            
            if (isRunning)
            {
                SchedulerStatusBorder.Background = (Brush)FindResource("Carbon.Brush.Success.Background");
                SchedulerStatusBorder.BorderBrush = (Brush)FindResource("Carbon.Brush.Success.Primary");
                SchedulerStatusText.Foreground = (Brush)FindResource("Carbon.Brush.Success.Primary");
            }
            else
            {
                SchedulerStatusBorder.Background = (Brush)FindResource("Carbon.Brush.Danger.Background");
                SchedulerStatusBorder.BorderBrush = (Brush)FindResource("Carbon.Brush.Danger.Primary");
                SchedulerStatusText.Foreground = (Brush)FindResource("Carbon.Brush.Danger.Primary");
            }
        }

        private void UpdateJobCount()
        {
            if (_jobs == null || _filteredJobs == null) return;
            
            JobCountText.Text = $"{_jobs.Count} job{(_jobs.Count != 1 ? "s" : "")} scheduled";
            FilteredJobCountText.Text = $"{_filteredJobs.Count} job{(_filteredJobs.Count != 1 ? "s" : "")} displayed";
            TotalJobCountText.Text = $"{_jobs.Count} total job{(_jobs.Count != 1 ? "s" : "")}";
        }

        private void ApplyFilters()
        {
            // Null-safe check for collections
            if (_filteredJobs == null || _jobs == null) return;
            
            _filteredJobs.Clear();
            
            var searchText = SearchBox?.Text?.ToLower() ?? "";
            var selectedStatus = (StatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Jobs";
            
            var filtered = _jobs.Where(job =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  job.JobId.ToLower().Contains(searchText) ||
                                  job.CronExpression.ToLower().Contains(searchText);
                
                // Status filter
                var matchesStatus = selectedStatus == "All Jobs" ||
                                  (selectedStatus == "Active" && job.Enabled) ||
                                  (selectedStatus == "Disabled" && !job.Enabled);
                
                return matchesSearch && matchesStatus;
            });
            
            foreach (var job in filtered)
            {
                _filteredJobs.Add(job);
            }
        }

        private void ToggleJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            var jobViewModel = _jobs.FirstOrDefault(j => j.JobId == jobId);
            if (jobViewModel == null) return;

            var success = _cronService.SetJobEnabled(jobId, !jobViewModel.Enabled);
            if (success)
            {
                jobViewModel.Enabled = !jobViewModel.Enabled;
                
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Info, 
                    $"Cron job '{jobId}' {(jobViewModel.Enabled ? "enabled" : "disabled")}"
                );
            }
        }

        private async void TriggerJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            var confirmation = new ConfirmationDialog(
                "Trigger Cron Job", 
                $"Are you sure you want to manually trigger the job '{jobId}'?"
            );
            
            if (await confirmation.ShowDialogAsync())
            {
                var success = _cronService.TriggerJob(jobId);
                if (success)
                {
                    ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                        LogType.Info, 
                        $"Manually triggered cron job: {jobId}"
                    );
                }
                else
                {
                    ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                        LogType.Error, 
                        $"Failed to trigger cron job: {jobId}"
                    );
                }
            }
        }

        private void EditJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;
            
            // TODO: Implement job editing dialog
            MessageBox.Show($"Edit job functionality will be implemented soon.\nJob ID: {jobId}", 
                          "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return;

            var confirmation = new ConfirmationDialog(
                "Delete Cron Job", 
                $"Are you sure you want to permanently delete the job '{jobId}'?\n\nThis action cannot be undone."
            );
            
            if (await confirmation.ShowDialogAsync())
            {
                // TODO: Implement job deletion
                MessageBox.Show($"Delete job functionality will be implemented soon.\nJob ID: {jobId}", 
                              "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
            UpdateJobCount();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateJobCount();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusFilter.SelectedIndex = 0;
        }

        private void AddJob_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement add job dialog
            MessageBox.Show("Add new job functionality will be implemented soon.", 
                          "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            var activeJobs = _jobs.Count(j => j.Enabled);
            var disabledJobs = _jobs.Count(j => !j.Enabled);
            var totalExecutions = _jobs.Sum(j => j.ExecutionCount);
            var failedJobs = _jobs.Count(j => !string.IsNullOrEmpty(j.LastError));

            var stats = $"📊 Cron Job Statistics\n\n" +
                       $"Total Jobs: {_jobs.Count}\n" +
                       $"Active Jobs: {activeJobs}\n" +
                       $"Disabled Jobs: {disabledJobs}\n" +
                       $"Total Executions: {totalExecutions}\n" +
                       $"Jobs with Errors: {failedJobs}";

            MessageBox.Show(stats, "Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshJobs_Click(object sender, RoutedEventArgs e)
        {
            LoadJobs();
            UpdateSchedulerStatus();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class CronJobViewModel
    {
        public CronJobViewModel(CronJobInfo jobInfo)
        {
            JobId = jobInfo.JobId;
            CronExpression = jobInfo.CronExpression;
            Enabled = jobInfo.Enabled;
            NextExecution = jobInfo.NextExecution;
            ExecutionCount = jobInfo.ExecutionCount;
            LastExecution = jobInfo.LastExecution;
            LastError = jobInfo.LastError;
        }

        public string JobId { get; set; }
        public string CronExpression { get; set; }
        public bool Enabled { get; set; }
        public DateTime? NextExecution { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime? LastExecution { get; set; }
        public string? LastError { get; set; }
    }

    // Simple RelayCommand implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T)parameter!) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute((T)parameter!);
        }
    }
}