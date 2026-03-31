using System.Text;
using LearnerDataStorybook.Models;
using Microsoft.Data.SqlClient;
using NServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnerDataStorybook.Services;

public class StoryRunner(AppConfig config, IEndpointInstance? endpointInstance = null)
{
    public async Task RunAsync(StoryEntry entry)
    {
        var story = entry.Story;
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"Story: {story.Name}");
        Console.WriteLine(new string('─', 50));

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(config.BaseUrl) };

        for (int i = 0; i < story.Steps.Count; i++)
        {
            var step = story.Steps[i];
            var stepNum = i + 1;
            if (step.Disabled)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {stepNum}. [{step.Type.ToUpperInvariant()}] {step.Name}  —  skipped (disabled)");
                Console.ResetColor();
                continue;
            }

            var success = step.Type.ToUpperInvariant() switch
            {
                "EVENT"   => await RunEventStepAsync(stepNum, step, entry.FolderPath),
                "SQL"     => await RunSqlStepAsync(stepNum, step, entry.FolderPath, context),
                "CONTEXT" => RunContextStep(stepNum, step, context),
                _         => await RunHttpStepAsync(stepNum, step, entry.FolderPath, http, context)
            };

            if (!success)
                return;

            if (step.DelayMs > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"     waiting {step.DelayMs}ms...");
                Console.ResetColor();
                await Task.Delay(step.DelayMs);
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n  All steps completed successfully.");
        Console.ResetColor();
    }

    // ── Adhoc entry point ────────────────────────────────────────────────────

    public async Task RunAdhocStepAsync(Step step, string adhocFolder, Dictionary<string, string> context)
    {
        Console.WriteLine($"Adhoc: {step.Name}");
        Console.WriteLine(new string('─', 50));

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(config.BaseUrl) };

        var success = step.Type.ToUpperInvariant() switch
        {
            "EVENT"   => await RunEventStepAsync(1, step, adhocFolder),
            "SQL"     => await RunSqlStepAsync(1, step, adhocFolder, context),
            "CONTEXT" => RunContextStep(1, step, context),
            _         => await RunHttpStepAsync(1, step, adhocFolder, http, context)
        };

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Step completed successfully.");
            Console.ResetColor();
        }

        if (step.DelayMs > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"     waiting {step.DelayMs}ms...");
            Console.ResetColor();
            await Task.Delay(step.DelayMs);
        }
    }

    // ── Context step ─────────────────────────────────────────────────────────

    private bool RunContextStep(int stepNum, Step step, Dictionary<string, string> context)
    {
        Console.WriteLine($"  {stepNum}. {step.Name}");
        foreach (var (key, value) in step.Values)
        {
            context[key] = value;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"     {key} = {value}");
            Console.ResetColor();
        }
        return true;
    }

    // ── HTTP step ────────────────────────────────────────────────────────────

    private async Task<bool> RunHttpStepAsync(
        int stepNum, Step step, string folderPath,
        HttpClient http, Dictionary<string, string> context)
    {
        var route = ResolveTemplate(step.Route, context);
        string? body = null;

        if (step.Body is not null)
        {
            body = ResolveTemplate(step.Body.ToString(Newtonsoft.Json.Formatting.None), context);
        }
        else if (step.PayloadFile is not null)
        {
            var payloadPath = Path.Combine(folderPath, "payloads", step.PayloadFile);
            if (!File.Exists(payloadPath))
            {
                PrintError($"Payload file not found: {payloadPath}");
                return false;
            }
            body = await File.ReadAllTextAsync(payloadPath);
        }

        PrintHttpStepStart(stepNum, step, route, body);

        HttpResponseMessage response;
        try
        {
            response = await SendAsync(http, step.Verb, route, body);
        }
        catch (Exception ex)
        {
            PrintError($"Request failed: {ex.Message}");
            return false;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        PrintHttpStepResult(response, responseBody);

        if (!response.IsSuccessStatusCode)
            return false;

        ExtractValues(step, responseBody, context);
        return true;
    }

    // ── Event step ───────────────────────────────────────────────────────────

    private async Task<bool> RunEventStepAsync(int stepNum, Step step, string folderPath)
    {
        if (endpointInstance is null)
        {
            PrintError("ServiceBusNamespace is not configured in appsettings.json — cannot publish events.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(step.EventType))
        {
            PrintError($"Step '{step.Name}' is type Event but has no eventType specified.");
            return false;
        }

        var eventType = ResolveEventType(step.EventType);
        if (eventType is null)
        {
            PrintError($"Could not find event type '{step.EventType}' in any loaded assembly.");
            return false;
        }

        string? payload = null;
        if (step.Body is not null)
        {
            payload = step.Body.ToString(Newtonsoft.Json.Formatting.None);
        }
        else if (step.PayloadFile is not null)
        {
            var payloadPath = Path.Combine(folderPath, "payloads", step.PayloadFile);
            if (!File.Exists(payloadPath))
            {
                PrintError($"Payload file not found: {payloadPath}");
                return false;
            }
            payload = await File.ReadAllTextAsync(payloadPath);
        }

        PrintEventStepStart(stepNum, step, payload);

        try
        {
            var eventObj = payload is not null
                ? JsonConvert.DeserializeObject(payload, eventType)
                : Activator.CreateInstance(eventType);

            await endpointInstance.Publish(eventObj!).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PrintError($"Failed to publish event: {ex.Message}");
            return false;
        }

        PrintEventStepResult();
        return true;
    }

    // ── SQL step ─────────────────────────────────────────────────────────────

    private async Task<bool> RunSqlStepAsync(int stepNum, Step step, string folderPath, Dictionary<string, string> context)
    {
        if (string.IsNullOrWhiteSpace(step.ConnectionName) || !config.Connections.TryGetValue(step.ConnectionName, out var connectionString))
        {
            PrintError($"Connection '{step.ConnectionName}' not found in appsettings.json Connections.");
            return false;
        }

        string? query = step.Query;
        if (step.QueryFile is not null)
        {
            var queryPath = Path.Combine(folderPath, "payloads", step.QueryFile);
            if (!File.Exists(queryPath))
            {
                PrintError($"Query file not found: {queryPath}");
                return false;
            }
            query = await File.ReadAllTextAsync(queryPath);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            PrintError($"SQL step '{step.Name}' has no query or queryFile.");
            return false;
        }

        PrintSqlStepStart(stepNum, step);

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("no rows returned");
                Console.WriteLine("     Story stopped.");
                Console.ResetColor();
                return false;
            }

            foreach (var (contextKey, column) in step.Extract)
            {
                var ordinal = reader.GetOrdinal(column);
                context[contextKey] = reader.GetValue(ordinal).ToString()!;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
            Console.ResetColor();

            if (config.Verbosity == Verbosity.Verbose)
            {
                foreach (var (contextKey, value) in step.Extract.Select(e => (e.Key, context[e.Key])))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     {contextKey} = {value}");
                    Console.ResetColor();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            PrintError($"SQL step failed: {ex.Message}");
            return false;
        }
    }

    // ── Printing ─────────────────────────────────────────────────────────────

    private void PrintHttpStepStart(int num, Step step, string route, string? body)
    {
        if (config.Verbosity == Verbosity.Quiet)
        {
            Console.Write($"  {num}. {step.Name}... ");
            return;
        }

        Console.WriteLine($"  {num}. {step.Name}");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        if (config.Verbosity == Verbosity.Verbose && body is not null)
        {
            Console.WriteLine($"     [{step.Verb}] {route}");
            Console.WriteLine("     Request body:");
            Console.WriteLine(Indent(PrettyJson(body), 5));
            Console.Write("     Status: ");
        }
        else
        {
            Console.Write($"     [{step.Verb}] {route}  =>  ");
            Console.ResetColor();
        }
    }

    private void PrintHttpStepResult(HttpResponseMessage response, string body)
    {
        var statusText = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(statusText);
        Console.ResetColor();

        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("     Story stopped.");
            Console.ResetColor();

            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("     Response body:");
                Console.WriteLine(Indent(PrettyJson(body), 5));
                Console.ResetColor();
            }
            return;
        }

        if (config.Verbosity == Verbosity.Verbose && !string.IsNullOrWhiteSpace(body))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("     Response body:");
            Console.WriteLine(Indent(PrettyJson(body), 5));
            Console.ResetColor();
        }
    }

    private void PrintSqlStepStart(int num, Step step)
    {
        if (config.Verbosity == Verbosity.Quiet)
        {
            Console.Write($"  {num}. {step.Name}... ");
            return;
        }

        Console.WriteLine($"  {num}. {step.Name}");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        if (config.Verbosity == Verbosity.Verbose)
        {
            Console.WriteLine($"     SQL  {step.QueryFile ?? step.Query}");
            Console.Write("     Result: ");
        }
        else
        {
            Console.Write($"     SQL  {step.QueryFile ?? step.Query}  =>  ");
            Console.ResetColor();
        }
    }

    private void PrintEventStepStart(int num, Step step, string? payload)
    {
        if (config.Verbosity == Verbosity.Quiet)
        {
            Console.Write($"  {num}. {step.Name}... ");
            return;
        }

        Console.WriteLine($"  {num}. {step.Name}");
        Console.ForegroundColor = ConsoleColor.DarkGray;

        if (config.Verbosity == Verbosity.Verbose)
        {
            Console.WriteLine($"     EVENT  {step.EventType}");
            if (payload is not null)
            {
                Console.WriteLine("     Payload:");
                Console.WriteLine(Indent(PrettyJson(payload), 5));
            }
            Console.Write("     Result: ");
        }
        else
        {
            Console.Write($"     EVENT  {step.EventType}  =>  ");
            Console.ResetColor();
        }
    }

    private void PrintEventStepResult()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Published");
        Console.ResetColor();
    }

    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ERROR: {message}");
        Console.ResetColor();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient http, string verb, string route, string? body)
    {
        HttpContent? content = body is not null
            ? new StringContent(body, Encoding.UTF8, "application/json")
            : null;

        return verb.ToUpperInvariant() switch
        {
            "GET"    => await http.GetAsync(route),
            "POST"   => await http.PostAsync(route, content),
            "PUT"    => await http.PutAsync(route, content),
            "PATCH"  => await http.PatchAsync(route, content),
            "DELETE" => await http.DeleteAsync(route),
            _        => throw new InvalidOperationException($"Unsupported HTTP verb: {verb}")
        };
    }

    private static void ExtractValues(Step step, string responseBody, Dictionary<string, string> context)
    {
        if (step.Extract.Count == 0)
            return;

        JObject json;
        try { json = JObject.Parse(responseBody); }
        catch { return; }

        foreach (var (key, jsonPath) in step.Extract)
        {
            var value = json.SelectToken(jsonPath)?.ToString();
            if (value is not null)
                context[key] = value;
            else
                Console.WriteLine($"  [warn] Could not extract '{key}' using path '{jsonPath}'");
        }
    }

    private static string ResolveTemplate(string template, Dictionary<string, string> context)
    {
        foreach (var (key, value) in context)
            template = template.Replace($"{{{key}}}", value);
        return template;
    }

    private static Type? ResolveEventType(string typeName)
    {
        // Ensure all DLLs in the output directory are loaded before scanning
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try { System.Reflection.Assembly.LoadFrom(dll); } catch { /* ignore */ }
        }

        bool isFullName = typeName.Contains('.');
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => isFullName
                ? t.FullName == typeName
                : t.Name == typeName);
    }

    private static string PrettyJson(string json)
    {
        try { return JToken.Parse(json).ToString(Formatting.Indented); }
        catch { return json; }
    }

    private static string Indent(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join('\n', text.Split('\n').Select(l => pad + l));
    }
}
