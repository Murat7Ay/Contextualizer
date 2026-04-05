use super::job::CronJob;
use std::collections::HashMap;

#[derive(Debug, Default)]
pub struct CronScheduler {
    jobs: HashMap<String, CronJob>,
    running: bool,
}

impl CronScheduler {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn add_job(&mut self, job: CronJob) -> Result<(), String> {
        if self.jobs.contains_key(&job.id) {
            return Err(format!("Job '{}' already exists", job.id));
        }
        self.jobs.insert(job.id.clone(), job);
        Ok(())
    }

    pub fn remove_job(&mut self, id: &str) -> bool {
        self.jobs.remove(id).is_some()
    }

    pub fn get_job(&self, id: &str) -> Option<&CronJob> {
        self.jobs.get(id)
    }

    pub fn get_job_mut(&mut self, id: &str) -> Option<&mut CronJob> {
        self.jobs.get_mut(id)
    }

    pub fn due_jobs(&self) -> Vec<&CronJob> {
        self.jobs.values().filter(|j| j.is_due()).collect()
    }

    pub fn tick(&mut self) -> Vec<String> {
        let due_ids: Vec<String> = self
            .jobs
            .values()
            .filter(|j| j.is_due())
            .map(|j| j.id.clone())
            .collect();

        for id in &due_ids {
            if let Some(job) = self.jobs.get_mut(id) {
                job.mark_run();
            }
        }

        due_ids
    }

    pub fn list_jobs(&self) -> Vec<&CronJob> {
        self.jobs.values().collect()
    }

    pub fn job_count(&self) -> usize {
        self.jobs.len()
    }

    pub fn is_running(&self) -> bool {
        self.running
    }

    pub fn start(&mut self) {
        self.running = true;
    }

    pub fn stop(&mut self) {
        self.running = false;
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_scheduler_add_and_get() {
        let mut sched = CronScheduler::new();
        let job = CronJob::new("j1", "handler1", "every 30s");
        sched.add_job(job).unwrap();
        assert_eq!(sched.job_count(), 1);
        assert!(sched.get_job("j1").is_some());
    }

    #[test]
    fn test_scheduler_duplicate_job() {
        let mut sched = CronScheduler::new();
        sched.add_job(CronJob::new("j1", "h1", "every 30s")).unwrap();
        assert!(sched.add_job(CronJob::new("j1", "h2", "every 60s")).is_err());
    }

    #[test]
    fn test_scheduler_remove_job() {
        let mut sched = CronScheduler::new();
        sched.add_job(CronJob::new("j1", "h1", "every 30s")).unwrap();
        assert!(sched.remove_job("j1"));
        assert!(!sched.remove_job("j1"));
        assert_eq!(sched.job_count(), 0);
    }

    #[test]
    fn test_scheduler_tick_marks_due_jobs() {
        let mut sched = CronScheduler::new();
        sched.add_job(CronJob::new("j1", "h1", "every 1s")).unwrap();
        sched.add_job(CronJob::new("j2", "h2", "every 1s")).unwrap();

        let due = sched.tick();
        assert_eq!(due.len(), 2);

        let due_after = sched.tick();
        assert!(due_after.is_empty(), "Jobs should not be due immediately after tick");
    }

    #[test]
    fn test_scheduler_start_stop() {
        let mut sched = CronScheduler::new();
        assert!(!sched.is_running());
        sched.start();
        assert!(sched.is_running());
        sched.stop();
        assert!(!sched.is_running());
    }

    #[test]
    fn test_scheduler_list_jobs() {
        let mut sched = CronScheduler::new();
        sched.add_job(CronJob::new("j1", "h1", "every 30s")).unwrap();
        sched.add_job(CronJob::new("j2", "h2", "every 60s")).unwrap();
        let jobs = sched.list_jobs();
        assert_eq!(jobs.len(), 2);
    }

    #[test]
    fn test_scheduler_due_jobs_filter() {
        let mut sched = CronScheduler::new();
        sched.add_job(CronJob::new("j1", "h1", "every 1s")).unwrap();
        let mut disabled_job = CronJob::new("j2", "h2", "every 1s");
        disabled_job.enabled = false;
        sched.add_job(disabled_job).unwrap();

        let due = sched.due_jobs();
        assert_eq!(due.len(), 1);
        assert_eq!(due[0].id, "j1");
    }
}
