using Contextualizer.PluginContracts;
using Contextualizer.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class CronManagerWindow : Window
    {
        private readonly ICronService _cronService;
        private readonly ObservableCollection<CronJobViewModel> _jobs;

        public CronManagerWindow()
        {
            InitializeComponent();
            
            _cronService = ServiceLocator.Get<ICronService>();
            _jobs = new ObservableCollection<CronJobViewModel>();
            
            JobsListBox.ItemsSource = _jobs;
            DataContext = this;
            
            LoadJobs();
            UpdateSchedulerStatus();
        }

        public ICommand ToggleJobCommand => new RelayCommand<string>(ToggleJob);
        public ICommand TriggerJobCommand => new RelayCommand<string>(TriggerJob);

        private void LoadJobs()
        {
            _jobs.Clear();
            
            var scheduledJobs = _cronService.GetScheduledJobs();
            foreach (var job in scheduledJobs.Values)
            {
                _jobs.Add(new CronJobViewModel(job));
            }
            
            UpdateJobCount();
        }

        private void UpdateSchedulerStatus()
        {
            var isRunning = _cronService.IsRunning;
            SchedulerStatusText.Text = $"Scheduler: {(isRunning ? "Running" : "Stopped")}";
            SchedulerStatusText.Foreground = isRunning 
                ? (Brush)FindResource("Carbon.Brush.Support.Success") 
                : (Brush)FindResource("Carbon.Brush.Support.Error");
        }

        private void UpdateJobCount()
        {
            JobCountText.Text = $"{_jobs.Count} job{(_jobs.Count != 1 ? "s" : "")} scheduled";
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
                
                ServiceLocator.Get<IUserInteractionService>()?.Log(
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
                    ServiceLocator.Get<IUserInteractionService>()?.Log(
                        LogType.Info, 
                        $"Manually triggered cron job: {jobId}"
                    );
                }
                else
                {
                    ServiceLocator.Get<IUserInteractionService>()?.Log(
                        LogType.Error, 
                        $"Failed to trigger cron job: {jobId}"
                    );
                }
            }
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