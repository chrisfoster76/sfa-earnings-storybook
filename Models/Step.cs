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

    /// <summary>
    /// If true, a non-2xx response from this HTTP step does not stop the story.
    /// Use for stories that deliberately exercise an error path.
    /// </summary>
    public bool IgnoreFailureStatus { get; set; } = false;

    /// <summary>
    /// If set, the numeric HTTP status code of this step's response is stored in the
    /// run context under this key (regardless of success/failure), for later assertion
    /// via a "Context" type assertion.
    /// </summary>
    public string? CaptureStatusAs { get; set; }

    /// <summary>
    /// If true, this HTTP step is sent without awaiting the response — the story moves
    /// on to the next step immediately. Useful for provoking race conditions between two
    /// in-flight requests. The response is still awaited (and logged) before the story
    /// finishes, but it cannot participate in status checks, Extract, or CaptureStatusAs
    /// since those need to run synchronously with the step.
    /// </summary>
    public bool FireAndForget { get; set; } = false;

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

    // ── Context step properties ──────────────────────────────────────────
    /// <summary>Key/value pairs to set directly in the run context. Used with type "Context".</summary>
    public Dictionary<string, string> Values { get; set; } = [];

    // ── Shared ───────────────────────────────────────────────────────────
    /// <summary>Filename relative to the story's payloads/ folder.</summary>
    public string? PayloadFile { get; set; }
    /// <summary>Inline JSON body. Takes precedence over PayloadFile. Used in adhoc steps.</summary>
    public JToken? Body { get; set; }

    // ── Assert step properties ───────────────────────────────────────────
    /// <summary>Expected column values for an Assert step. Each field is checked independently.</summary>
    public List<AssertionExpectation> Expected { get; set; } = [];
}
