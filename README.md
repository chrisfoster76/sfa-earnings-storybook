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

To run a specific story directly from the command line, pass its id as an argument:

```bash
dotnet run -- short-course-completion
```

### Wipe

Wipe the databases without running a story. Also clears `adhoc/context.json` so stale context doesn't carry over into a new session:

```bash
dotnet run -- wipe
```

### Adhoc Mode

Run a single step interactively, without a pre-defined story. Steps are defined as JSON files in the `adhoc/` folder. Context (e.g. extracted `learningKey`) is automatically persisted to `adhoc/context.json` between invocations, so values from one step are available via `{variableName}` templating in subsequent steps.

```bash
dotnet run -- adhoc step.json
```

The adhoc step format is the same as a story step, but with an inline `body` instead of a `payloadFile`:

```json
{
  "name": "Create a short course learner",
  "verb": "POST",
  "route": "/providers/10005077/shortCourses",
  "body": { ... }
}
```

All step types are supported (`Http`, `Event`, `Sql`). Context is automatically cleared when you run `dotnet run -- wipe`.

### Multiple Providers

The UKPRN is templated into routes as `{ukprn}`. Use a Context step to set or switch it:

```bash
dotnet run -- adhoc step.json  # step.json sets type=Context, values: { "ukprn": "20009999" }
```

The same learner (by ULN) can exist under multiple providers simultaneously. Switch provider mid-story or mid-adhoc session with another Context step.

Commands must be run from `C:\code\sfa\misc\sfa-earnings-storybook`. Per-story commands:

| Story | Command |
|---|---|
| Short Course | `dotnet run -- short-course` |
| Short Course - Claim Milestones | `dotnet run -- short-course-claim-milestones` |
| Short Course - Completion | `dotnet run -- short-course-completion` |
| Short Course - Withdrawal | `dotnet run -- short-course-withdrawal` |
| Short Course - Earnings Return Scenarios | `dotnet run -- short-course-earnings-return` |
| Short Course - Deletion | `dotnet run -- short-course-deletion` |
| Short Course - Reinstatement | `dotnet run -- short-course-reinstatement` |

## Stories

Stories live in `stories/<story-name>/story.json`. Each story has a name, description, optional database wipe on run, and a list of steps.

### Available Stories

| ID | Name | Description |
|---|---|---|
| `short-course` | Short Course | Creates a short course claiming `ThirtyPercentLearningComplete` on POST, then fires the approval event |
| `short-course-claim-milestones` | Short Course - Claim Milestones | Creates a short course with no milestones, approves it, then claims `ThirtyPercentLearningComplete` via PUT |
| `short-course-completion` | Short Course - Completion | Creates a short course with no milestones, approves it, claims `ThirtyPercentLearningComplete`, then completes it via PUT with `completionDate` |
| `short-course-withdrawal` | Short Course - Withdrawal | Creates a short course with no milestones, approves it, then withdraws the learner via PUT with `withdrawalDate` |
| `short-course-earnings-return` | Short Course - Earnings Return Scenarios | POSTs 8 learners covering all FLP-1673 scenarios for whether earnings are returned to SLD for 25/26 |
| `short-course-deletion` | Short Course - Deletion | Creates a short course, approves it, then deletes it via DELETE |
| `short-course-reinstatement` | Short Course - Reinstatement | Creates a short course, approves it, deletes it, then reinstates it by PUTting with `withdrawalDate: null` |

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

#### Context

Sets key/value pairs directly into the run context. Useful for seeding variables like `ukprn` at the start of a story, or switching them mid-story.

```json
{
  "name": "Set provider to Provider A",
  "type": "Context",
  "values": { "ukprn": "10005077" }
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
