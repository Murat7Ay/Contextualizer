use std::time::{Duration, Instant};

#[derive(Debug, Clone)]
pub struct CronJob {
    pub id: String,
    pub handler_name: String,
    pub cron_expression: String,
    pub enabled: bool,
    interval: Option<Duration>,
    last_run: Option<Instant>,
}

impl CronJob {
    pub fn new(id: &str, handler_name: &str, cron_expression: &str) -> Self {
        let interval = parse_simple_interval(cron_expression);
        Self {
            id: id.to_string(),
            handler_name: handler_name.to_string(),
            cron_expression: cron_expression.to_string(),
            enabled: true,
            interval,
            last_run: None,
        }
    }

    pub fn is_due(&self) -> bool {
        if !self.enabled {
            return false;
        }
        match (self.interval, self.last_run) {
            (Some(interval), Some(last)) => last.elapsed() >= interval,
            (Some(_), None) => true,
            _ => false,
        }
    }

    pub fn mark_run(&mut self) {
        self.last_run = Some(Instant::now());
    }

    pub fn interval(&self) -> Option<Duration> {
        self.interval
    }

    pub fn update_expression(&mut self, expression: &str) {
        self.cron_expression = expression.to_string();
        self.interval = parse_simple_interval(expression);
    }
}

/// Parse simple interval expressions: "every Ns", "every Nm", "every Nh"
fn parse_simple_interval(expr: &str) -> Option<Duration> {
    let trimmed = expr.trim().to_lowercase();

    if trimmed.starts_with("every ") {
        let rest = trimmed.strip_prefix("every ")?.trim();
        if let Some(secs) = rest.strip_suffix('s') {
            return secs.trim().parse::<u64>().ok().map(Duration::from_secs);
        }
        if let Some(mins) = rest.strip_suffix('m') {
            return mins.trim().parse::<u64>().ok().map(|m| Duration::from_secs(m * 60));
        }
        if let Some(hours) = rest.strip_suffix('h') {
            return hours.trim().parse::<u64>().ok().map(|h| Duration::from_secs(h * 3600));
        }
    }

    None
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_cron_job_creation() {
        let job = CronJob::new("job1", "handler1", "every 30s");
        assert_eq!(job.id, "job1");
        assert_eq!(job.handler_name, "handler1");
        assert!(job.enabled);
        assert_eq!(job.interval(), Some(Duration::from_secs(30)));
    }

    #[test]
    fn test_cron_job_is_due_first_time() {
        let job = CronJob::new("job1", "handler1", "every 1s");
        assert!(job.is_due(), "First run should always be due");
    }

    #[test]
    fn test_cron_job_not_due_after_run() {
        let mut job = CronJob::new("job1", "handler1", "every 60s");
        job.mark_run();
        assert!(!job.is_due(), "Should not be due immediately after run");
    }

    #[test]
    fn test_cron_job_disabled() {
        let mut job = CronJob::new("job1", "handler1", "every 1s");
        job.enabled = false;
        assert!(!job.is_due());
    }

    #[test]
    fn test_parse_intervals() {
        assert_eq!(parse_simple_interval("every 30s"), Some(Duration::from_secs(30)));
        assert_eq!(parse_simple_interval("every 5m"), Some(Duration::from_secs(300)));
        assert_eq!(parse_simple_interval("every 2h"), Some(Duration::from_secs(7200)));
        assert_eq!(parse_simple_interval("invalid"), None);
        assert_eq!(parse_simple_interval("every xyz"), None);
    }

    #[test]
    fn test_cron_job_invalid_expression() {
        let job = CronJob::new("job1", "handler1", "invalid");
        assert!(job.interval().is_none());
        assert!(!job.is_due());
    }

    #[test]
    fn test_cron_job_update_expression() {
        let mut job = CronJob::new("job1", "handler1", "every 30s");
        assert_eq!(job.interval(), Some(Duration::from_secs(30)));
        job.update_expression("every 5m");
        assert_eq!(job.cron_expression, "every 5m");
        assert_eq!(job.interval(), Some(Duration::from_secs(300)));
    }
}
