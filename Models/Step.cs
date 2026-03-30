using Newtonsoft.Json.Linq;

namespace LearnerDataStorybook.Models;

public class Step
{
    /// <summary>"Http" (default) or "Event".</summary>
    public string Type { get; set; } = "Http";

    public bool Disabled { get; set; } = false;
    public int DelayMs { get; set; } = 0;

    public string Name { get; set; } = string.Empty;

    // ── Http step properties ─────────────────────────────────────────────
    public string Verb { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Values to extract from the HTTP response body and store in the run context.
    /// Key = variable name used in later route templates (e.g. "learnerId").
    /// Value = JSONPath expression (e.g. "$.data.id").
    /// </summary>
    public Dictionary<string, string> Extract { get; set; } = [];

    // ── Event step properties ────────────────────────────────────────────
    /// <summary>Full type name of the NServiceBus event to publish (e.g. "SFA.DAS.CommitmentsV2.Messages.Events.ApprenticeshipCreatedEvent").</summary>
    public string? EventType { get; set; }

    // ── Sql step properties ──────────────────────────────────────────────
    /// <summary>Key into the Connections dictionary in appsettings.json.</summary>
    public string? ConnectionName { get; set; }
    /// <summary>Inline SQL query. Ignored if QueryFile is set.</summary>
    public string? Query { get; set; }
    /// <summary>Filename relative to the story's payloads/ folder. Takes precedence over Query.</summary>
    public string? QueryFile { get; set; }

    // ── Shared ───────────────────────────────────────────────────────────
    /// <summary>Filename relative to the story's payloads/ folder.</summary>
    public string? PayloadFile { get; set; }
    /// <summary>Inline JSON body. Takes precedence over PayloadFile. Used in adhoc steps.</summary>
    public JToken? Body { get; set; }
}
