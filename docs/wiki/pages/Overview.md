# Overview

## Purpose
Provide a complete, A-to-Z documentation set for Contextualizer, covering all audiences: users, power users, developers, and ops.

## What Contextualizer Does
- Monitors clipboard and other triggers.
- Runs handler pipelines to create context.
- Executes actions with conditions and templates.
- Exposes capabilities via UI and MCP.

## Core Entry Points (Source)
- Handler orchestration: [Contextualizer.Core/HandlerManager.cs](Contextualizer.Core/HandlerManager.cs)
- Execution pipeline: [Contextualizer.Core/Dispatch.cs](Contextualizer.Core/Dispatch.cs)
- Action orchestration: [Contextualizer.Core/ActionService.cs](Contextualizer.Core/ActionService.cs)

## Audience Map
- Users: Quick Start, UI, Troubleshooting
- Power users: Configuration, Handlers, Actions
- Developers: Architecture, Plugins, MCP, Build
- Ops: Cron, Security & Privacy, Build
