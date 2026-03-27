# SFA Earnings Storybook

A .NET 8 console application for running integration test scenarios against the SFA Earnings system. Stories are JSON-defined multi-step workflows that can make HTTP calls, publish domain events to Azure Service Bus, and execute SQL queries — all chainable via a simple context templating system.

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (local) — if running stories that query the database
- Azure Service Bus access — if running stories that publish events

### Configuration

Copy `appsettings.example.json` to `appsettings.json` and fill in your values:

```json
{
  "BaseUrl": "https://localhost:<port>",
  "Verbosity": "Normal",
  "ServiceBusNamespace": "<your-namespace>.servicebus.windows.net",
  "Connections": {
    "Learning": "<connection-string>"
  },
  "DatabaseWipes": [
    {
      "ConnectionString": "<connection-string>",
      "ScriptFile": "scripts/wipe-learning.sql"
    }
  ]
}
```

| Setting | Description |
|---|---|
| `BaseUrl` | Base URL of the target API |
| `Verbosity` | Output level: `Quiet`, `Normal`, or `Verbose` |
| `ServiceBusNamespace` | Azure Service Bus namespace (leave empty to skip Event steps) |
| `Connections` | Named SQL connection strings, referenced by SQL steps |
| `DatabaseWipes` | Database cleanup configs run before stories with `wipeOnRun: true` |

### Run

```bash
dotnet run
```

You'll be presented with a menu of available stories to choose from.

## Stories

Stories live in `stories/<story-name>/story.json`. Each story has a name, description, optional database wipe on run, and a list of steps.

### Available Stories

| Story | Description |
|---|---|
| **Short Course** | Creates a short course with both milestones on POST, then fires the approval event |
| **Short Course - Claim Milestones** | Creates a short course with no milestones, approves it, then claims both milestones via PUT |

### Step Types

#### HTTP (default)

Makes a REST call to the configured `BaseUrl`.

```json
{
  "name": "Create a Short Course",
  "verb": "POST",
  "route": "/providers/10005077/shortCourses",
  "payloadFile": "payloads/01-post-short-course.json",
  "extract": {
    "myVar": "$.some.json.path"
  }
}
```

#### Event

Publishes an NServiceBus domain event to Azure Service Bus.

```json
{
  "name": "Employer Approves the Short Course",
  "type": "Event",
  "eventType": "SFA.DAS.CommitmentsV2.Messages.Events.ApprenticeshipCreatedEvent",
  "payloadFile": "payloads/02-apprenticeship-created-event.json",
  "delayMs": 3000
}
```

#### SQL

Executes a SQL query against a named connection and optionally extracts column values into context.

```json
{
  "name": "Get Learning Key",
  "type": "Sql",
  "connectionName": "Learning",
  "queryFile": "payloads/03-get-learning-key.sql",
  "extract": {
    "learningKey": "LearningKey"
  }
}
```

### Context Templating

Values extracted by one step are available to all subsequent steps using `{variableName}` syntax in routes and payloads:

```json
{
  "route": "/providers/10005077/shortCourses/{learningKey}"
}
```

### Step Options

| Option | Description |
|---|---|
| `disabled` | Set to `true` to skip a step without removing it |
| `delayMs` | Wait before executing the step (useful after async operations) |

## Writing a New Story

1. Create a folder under `stories/<your-story-name>/`
2. Add a `story.json`:
```json
{
  "name": "My Story",
  "description": "What this story tests",
  "wipeOnRun": true,
  "steps": []
}
```
3. Add payload files under `stories/<your-story-name>/payloads/`
4. Reference them in steps via `payloadFile` or `queryFile`

The story will appear automatically in the menu on next run.

## Database Wipe Scripts

SQL wipe scripts live in `scripts/`. They are run before any story that has `wipeOnRun: true`, using the connections configured in `DatabaseWipes`. Current scripts:

- `wipe-learning.sql` — truncates SFA.DAS.Learning.Database tables
- `wipe-earnings.sql` — truncates SFA.DAS.Funding.ApprenticeshipEarnings.Database tables
