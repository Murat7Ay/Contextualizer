# Cron & Scheduling

## Core Scheduler
- `CronScheduler`: [Contextualizer.Core/Services/CronScheduler.cs](Contextualizer.Core/Services/CronScheduler.cs)

## Scheduler Behavior
- Uses Quartz to schedule jobs and triggers.
- Graceful shutdown waits for active jobs (timeout fallback).
- Supports job enable/disable, update, and manual trigger.
- Tracks job metadata (execution count, last error, next run).

## Cron Handler
- `CronHandler`: [Contextualizer.Core/CronHandler.cs](Contextualizer.Core/CronHandler.cs)

## Handler Registration Flow
- When `CronHandler` is constructed, it registers a Quartz job using `CronExpression` and `CronTimezone`.
- The handler clones its config into an “actual” handler config for execution.
- Manual trigger and enable/disable map to `ICronService`.

## Contract
- `ICronService`: [Contextualizer.PluginContracts/ICronService.cs](Contextualizer.PluginContracts/ICronService.cs)
