# Cron Handler Implementation Roadmap

## Overview
Implementation of a cron-type handler for Contextualizer that enables time-based automation while leveraging the existing handler architecture through a hybrid approach.

## Architecture Strategy
- **Separate Service**: `ICronService` manages scheduling independently
- **Hybrid Approach**: Generate synthetic clipboard events to trigger existing handlers
- **Minimal Changes**: Leverage existing `Dispatch` base class and handler pipeline

## Use Cases

### Content Generation
- Daily reports from database queries (DatabaseHandler + scheduled execution)
- Periodic API data fetches (ApiHandler + cron timing)
- Regular file backups or log rotations (FileHandler + scheduling)
- Automated status updates or notifications

### Maintenance Tasks
- Cleanup temporary files on schedule
- Database maintenance queries
- Cache clearing or data refresh
- System health checks with notifications

### Business Workflows
- Daily sales reports generation
- Weekly backup reminders
- Monthly metric calculations
- Periodic data synchronization

## Implementation Phases

### Phase 1: Core Infrastructure ✅ COMPLETED
- [x] Create `ICronService` interface - manage scheduled tasks
- [x] Implement `CronScheduler` service - handles timing/execution
- [x] Add cron expression parsing - standard cron syntax support (NCrontab)
- [x] Register with ServiceLocator - integrate with existing DI
- [x] Fix console app compilation issues - updated interface implementations

### Phase 2: Handler Integration ✅ COMPLETED
- [x] Extend `SyntheticHandler` - or create `CronTriggeredHandler` 
- [x] Create synthetic clipboard content - when cron fires
- [x] Route through existing pipeline - `HandlerManager.ExecuteHandlerConfig()`
- [x] Leverage existing actions - no new action types needed
- [x] Create CronHandler for automatic job registration
- [x] Extend HandlerConfig with cron-specific properties
- [x] Integrate CronScheduler with HandlerManager execution

### Phase 3: Configuration ✅ COMPLETED
- [x] Extend `handlers.json` - add cron scheduling config
- [x] Add cron-specific properties - schedule, timezone, enabled/disabled
- [x] Create example configurations for different use cases
- [x] Handler condition integration - use existing ConditionEvaluator
- [x] Migrate from NCrontab to Quartz.NET for enterprise reliability
- [x] Test with real API endpoint every minute

### Phase 4: UI Integration 📋 PLANNED
- [ ] Cron management screen - view/edit scheduled tasks
- [ ] Execution history - logs and status  
- [ ] Manual trigger capability - test schedules
- [ ] Real-time job status monitoring
- [ ] Cron expression builder/validator UI

## Technical Design

### Service Architecture
```
ICronService -> CronScheduler -> Quartz.NET Scheduler
     ↓
CronHandlerJob -> Generate Synthetic Content -> HandlerManager.ExecuteHandlerConfig()
     ↓
Existing Handler Pipeline (ApiHandler/DatabaseHandler/etc -> Actions)
```

### Configuration Schema
```json
{
  "type": "CronHandler",
  "name": "Daily Report Generator",
  "cronExpression": "0 8 * * MON-FRI",
  "timezone": "UTC",
  "enabled": true,
  "syntheticContent": {
    "type": "database_query",
    "content": "SELECT * FROM daily_metrics WHERE date = GETDATE()"
  },
  "targetHandler": "DatabaseHandler",
  "conditions": [...],
  "actions": [...]
}
```

### Key Components
- **ICronService**: Interface for cron scheduling operations
- **CronScheduler**: Implementation with Timer-based execution
- **CronExpression**: Parser for standard cron syntax
- **SyntheticContentGenerator**: Creates clipboard content for scheduled tasks
- **CronConfiguration**: Configuration model for scheduled handlers

## Dependencies
- **Quartz.NET** for cron expression parsing and scheduling ✅ IMPLEMENTED
- Integration with existing ServiceLocator pattern ✅ IMPLEMENTED
- **Migration**: Successfully migrated from NCrontab to Quartz.NET for better reliability

## Notes
- Maintains existing handler interface compatibility
- Leverages current plugin system without modifications
- Uses established error handling and logging patterns
- Follows existing configuration and condition evaluation systems