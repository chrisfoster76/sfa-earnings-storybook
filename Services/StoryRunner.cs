using System.Text;
using LearnerDataStorybook.Models;
using Microsoft.Data.SqlClient;
using NServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnerDataStorybook.Services;

public class StoryRunner
{
    private readonly AppConfig _config;
    private readonly IEndpointInstance? _endpointInstance;
    private readonly IConsoleWriter _out;
    private readonly Verbosity _verbosity;

    public StoryRunner(AppConfig config, IEndpointInstance? endpointInstance = null,
        IConsoleWriter? output = null, Verbosity? verbosityOverride = null)
    {
        _config = config;
        _endpointInstance = endpointInstance;
        _out = output ?? new SystemConsoleWriter();
        _verbosity = verbosityOverride ?? config.Verbosity;
    }

    public StoryRunner WithOutput(IConsoleWriter output) =>
        new(_config, _endpointInstance, output, Verbosity.Normal);

    // ── Story entry point ─────────────────────────────────────────────────────

    public async Task<bool> RunAsync(StoryEntry entry)
    {
        var story = entry.Story;
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(_config.BaseUrl) };

        for (int i = 0; i < story.Steps.Count; i++)
        {
            var step = story.Steps[i];
            var stepNum = i + 1;
            if (step.Disabled)
            {
                _out.ForegroundColor = ConsoleColor.DarkGray;
                _out.WriteLine($"  {stepNum}. [{step.Type.ToUpperInvariant()}] {step.Name}  —  skipped (disabled)");
                _out.ResetColor();
                continue;
            }

            var success = step.Type.ToUpperInvariant() switch
            {
                "EVENT"   => await RunEventStepAsync(stepNum, step, entry.FolderPath),
                "SQL"     => await RunSqlStepAsync(stepNum, step, entry.FolderPath, context),
                "ASSERT"  => await RunAssertStepAsync(stepNum, step, entry.FolderPath),
                "CONTEXT" => RunContextStep(stepNum, step, context),
                "WAIT"    => RunWaitForUserStep(stepNum, step),
                _         => await RunHttpStepAsync(stepNum, step, entry.FolderPath, http, context)
            };

            if (!success)
                return false;

            if (step.DelayMs > 0)
            {
                _out.ForegroundColor = ConsoleColor.DarkGray;
                _out.WriteLine($"     waiting {step.DelayMs}ms...");
                _out.ResetColor();
                await Task.Delay(step.DelayMs);
            }
        }

        _out.ForegroundColor = ConsoleColor.Green;
        _out.WriteLine("\n  All steps completed successfully.");
        _out.ResetColor();

        if (story.Assertions.Count > 0)
            return await RunAssertionsAsync(story.Assertions, entry.FolderPath);

        return true;
    }

    // ── Adhoc entry point ────────────────────────────────────────────────────

    public async Task RunAdhocStepAsync(Step step, string adhocFolder, Dictionary<string, string> context)
    {
        _out.WriteLine($"Adhoc: {step.Name}");
        _out.WriteLine(new string('─', 50));

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(_config.BaseUrl) };

        var success = step.Type.ToUpperInvariant() switch
        {
            "EVENT"   => await RunEventStepAsync(1, step, adhocFolder),
            "SQL"     => await RunSqlStepAsync(1, step, adhocFolder, context),
            "CONTEXT" => RunContextStep(1, step, context),
            "WAIT"    => RunWaitForUserStep(1, step),
            _         => await RunHttpStepAsync(1, step, adhocFolder, http, context)
        };

        if (success)
        {
            _out.ForegroundColor = ConsoleColor.Green;
            _out.WriteLine("\n  Step completed successfully.");
            _out.ResetColor();
        }

        if (step.DelayMs > 0)
        {
            _out.ForegroundColor = ConsoleColor.DarkGray;
            _out.WriteLine($"     waiting {step.DelayMs}ms...");
            _out.ResetColor();
            await Task.Delay(step.DelayMs);
        }
    }

    // ── Context step ─────────────────────────────────────────────────────────

    private bool RunContextStep(int stepNum, Step step, Dictionary<string, string> context)
    {
        _out.WriteLine($"  {stepNum}. {step.Name}");
        foreach (var (key, value) in step.Values)
        {
            context[key] = value;
            _out.ForegroundColor = ConsoleColor.DarkGray;
            _out.WriteLine($"     {key} = {value}");
            _out.ResetColor();
        }
        return true;
    }

    // ── Wait step ────────────────────────────────────────────────────────────

    private bool RunWaitForUserStep(int stepNum, Step step)
    {
        _out.ForegroundColor = ConsoleColor.Yellow;
        _out.WriteLine($"  {stepNum}. {step.Name}  —  press Enter to continue...");
        _out.ResetColor();
        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        _out.ForegroundColor = ConsoleColor.Green;
        _out.WriteLine($"     Resuming.");
        _out.ResetColor();
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
        if (_endpointInstance is null)
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

            await _endpointInstance.Publish(eventObj!).ConfigureAwait(false);
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
        if (string.IsNullOrWhiteSpace(step.ConnectionName) || !_config.Connections.TryGetValue(step.ConnectionName, out var connectionString))
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
                _out.ForegroundColor = ConsoleColor.Red;
                _out.WriteLine("no rows returned");
                _out.WriteLine("     Story stopped.");
                _out.ResetColor();
                return false;
            }

            foreach (var (contextKey, column) in step.Extract)
            {
                var ordinal = reader.GetOrdinal(column);
                context[contextKey] = reader.GetValue(ordinal).ToString()!;
            }

            _out.ForegroundColor = ConsoleColor.Green;
            _out.WriteLine("OK");
            _out.ResetColor();

            if (_verbosity == Verbosity.Verbose)
            {
                foreach (var (contextKey, value) in step.Extract.Select(e => (e.Key, context[e.Key])))
                {
                    _out.ForegroundColor = ConsoleColor.DarkGray;
                    _out.WriteLine($"     {contextKey} = {value}");
                    _out.ResetColor();
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

    // ── Assert step ──────────────────────────────────────────────────────────

    private async Task<bool> RunAssertStepAsync(int stepNum, Step step, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(step.ConnectionName) || !_config.Connections.TryGetValue(step.ConnectionName, out var connectionString))
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
            PrintError($"Assert step '{step.Name}' has no query or queryFile.");
            return false;
        }

        _out.WriteLine($"  {stepNum}. {step.Name}");

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                _out.ForegroundColor = ConsoleColor.Red;
                _out.WriteLine("     FAIL (query returned no rows)");
                _out.ResetColor();
                return false;
            }

            var fieldResults = new List<(string Field, string Expected, string Actual, bool Passed)>();
            foreach (var exp in step.Expected)
            {
                string actual;
                try
                {
                    var ordinal = reader.GetOrdinal(exp.Field);
                    actual = reader.IsDBNull(ordinal) ? "null" : reader.GetValue(ordinal).ToString()!;
                }
                catch { actual = "<column not found>"; }
                fieldResults.Add((exp.Field, exp.Value, actual,
                    string.Equals(actual, exp.Value, StringComparison.OrdinalIgnoreCase)));
            }

            var passed = fieldResults.All(r => r.Passed);
            _out.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;

            if (passed)
            {
                var summary = string.Join(", ", fieldResults.Select(r => $"{r.Field}={r.Actual}"));
                _out.WriteLine($"     PASS ({summary})");
            }
            else
            {
                _out.WriteLine("     FAIL");
                foreach (var (field, expected, actual, ok) in fieldResults)
                {
                    _out.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                    _out.WriteLine($"       {field}: expected {expected}, got {actual}");
                }
            }

            _out.ResetColor();
            return passed;
        }
        catch (Exception ex)
        {
            PrintError($"Assert step failed: {ex.Message}");
            return false;
        }
    }

    // ── Assertions ───────────────────────────────────────────────────────────

    private async Task<bool> RunAssertionsAsync(List<Assertion> assertions, string folderPath)
    {
        _out.WriteLine("");
        _out.WriteLine("  Assertions:");

        var allPassed = true;

        for (int i = 0; i < assertions.Count; i++)
        {
            var assertion = assertions[i];
            var num = i + 1;

            if (!string.Equals(assertion.Type, "Sql", StringComparison.OrdinalIgnoreCase))
            {
                _out.ForegroundColor = ConsoleColor.Yellow;
                _out.WriteLine($"  {num}. {assertion.Name}  —  unsupported assertion type '{assertion.Type}', skipped");
                _out.ResetColor();
                continue;
            }

            if (string.IsNullOrWhiteSpace(assertion.ConnectionName) || !_config.Connections.TryGetValue(assertion.ConnectionName, out var connectionString))
            {
                _out.ForegroundColor = ConsoleColor.Red;
                _out.WriteLine($"  {num}. {assertion.Name}  —  FAIL (connection '{assertion.ConnectionName}' not found)");
                _out.ResetColor();
                allPassed = false;
                continue;
            }

            string? query = assertion.Query;
            if (assertion.QueryFile is not null)
            {
                var queryPath = Path.Combine(folderPath, "payloads", assertion.QueryFile);
                if (!File.Exists(queryPath))
                {
                    _out.ForegroundColor = ConsoleColor.Red;
                    _out.WriteLine($"  {num}. {assertion.Name}  —  FAIL (query file not found: {queryPath})");
                    _out.ResetColor();
                    allPassed = false;
                    continue;
                }
                query = await File.ReadAllTextAsync(queryPath);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                _out.ForegroundColor = ConsoleColor.Red;
                _out.WriteLine($"  {num}. {assertion.Name}  —  FAIL (no query or queryFile specified)");
                _out.ResetColor();
                allPassed = false;
                continue;
            }

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    _out.ForegroundColor = ConsoleColor.Red;
                    _out.WriteLine($"  {num}. {assertion.Name}  —  FAIL (query returned no rows)");
                    _out.ResetColor();
                    allPassed = false;
                    continue;
                }

                var fieldResults = new List<(string Field, string Expected, string Actual, bool Passed)>();
                foreach (var exp in assertion.Expected)
                {
                    string actual;
                    try
                    {
                        var ordinal = reader.GetOrdinal(exp.Field);
                        actual = reader.IsDBNull(ordinal) ? "null" : reader.GetValue(ordinal).ToString()!;
                    }
                    catch { actual = "<column not found>"; }
                    fieldResults.Add((exp.Field, exp.Value, actual,
                        string.Equals(actual, exp.Value, StringComparison.OrdinalIgnoreCase)));
                }

                var assertionPassed = fieldResults.All(r => r.Passed);
                _out.ForegroundColor = assertionPassed ? ConsoleColor.Green : ConsoleColor.Red;
                _out.Write($"  {num}. {assertion.Name}  —  ");

                if (assertionPassed)
                {
                    var summary = string.Join(", ", fieldResults.Select(r => $"{r.Field}={r.Actual}"));
                    _out.WriteLine($"PASS ({summary})");
                }
                else
                {
                    _out.WriteLine("FAIL");
                    foreach (var (field, expected, actual, passed) in fieldResults)
                    {
                        _out.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
                        _out.WriteLine($"       {field}: expected {expected}, got {actual}");
                    }
                }

                _out.ResetColor();
                if (!assertionPassed) allPassed = false;
            }
            catch (Exception ex)
            {
                _out.ForegroundColor = ConsoleColor.Red;
                _out.WriteLine($"  {num}. {assertion.Name}  —  FAIL ({ex.Message})");
                _out.ResetColor();
                allPassed = false;
            }
        }

        _out.WriteLine("");
        _out.ForegroundColor = allPassed ? ConsoleColor.Green : ConsoleColor.Red;
        _out.WriteLine(allPassed ? "  All assertions passed." : "  One or more assertions failed.");
        _out.ResetColor();
        return allPassed;
    }

    // ── Printing ─────────────────────────────────────────────────────────────

    private void PrintHttpStepStart(int num, Step step, string route, string? body)
    {
        if (_verbosity == Verbosity.Quiet)
        {
            _out.Write($"  {num}. {step.Name}... ");
            return;
        }

        _out.WriteLine($"  {num}. {step.Name}");
        _out.ForegroundColor = ConsoleColor.DarkGray;

        if (_verbosity == Verbosity.Verbose && body is not null)
        {
            _out.WriteLine($"     [{step.Verb}] {route}");
            _out.WriteLine("     Request body:");
            _out.WriteLine(Indent(PrettyJson(body), 5));
            _out.Write("     Status: ");
        }
        else
        {
            _out.Write($"     [{step.Verb}] {route}  =>  ");
            _out.ResetColor();
        }
    }

    private void PrintHttpStepResult(HttpResponseMessage response, string body)
    {
        var statusText = $"{(int)response.StatusCode} {response.ReasonPhrase}";
        _out.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
        _out.WriteLine(statusText);
        _out.ResetColor();

        if (!response.IsSuccessStatusCode)
        {
            _out.ForegroundColor = ConsoleColor.Red;
            _out.WriteLine("     Story stopped.");
            _out.ResetColor();

            if (!string.IsNullOrWhiteSpace(body))
            {
                _out.ForegroundColor = ConsoleColor.DarkGray;
                _out.WriteLine("     Response body:");
                _out.WriteLine(Indent(PrettyJson(body), 5));
                _out.ResetColor();
            }
            return;
        }

        if (_verbosity == Verbosity.Verbose && !string.IsNullOrWhiteSpace(body))
        {
            _out.ForegroundColor = ConsoleColor.DarkGray;
            _out.WriteLine("     Response body:");
            _out.WriteLine(Indent(PrettyJson(body), 5));
            _out.ResetColor();
        }
    }

    private void PrintSqlStepStart(int num, Step step)
    {
        if (_verbosity == Verbosity.Quiet)
        {
            _out.Write($"  {num}. {step.Name}... ");
            return;
        }

        _out.WriteLine($"  {num}. {step.Name}");
        _out.ForegroundColor = ConsoleColor.DarkGray;

        if (_verbosity == Verbosity.Verbose)
        {
            _out.WriteLine($"     SQL  {step.QueryFile ?? step.Query}");
            _out.Write("     Result: ");
        }
        else
        {
            _out.Write($"     SQL  {step.QueryFile ?? step.Query}  =>  ");
            _out.ResetColor();
        }
    }

    private void PrintEventStepStart(int num, Step step, string? payload)
    {
        if (_verbosity == Verbosity.Quiet)
        {
            _out.Write($"  {num}. {step.Name}... ");
            return;
        }

        _out.WriteLine($"  {num}. {step.Name}");
        _out.ForegroundColor = ConsoleColor.DarkGray;

        if (_verbosity == Verbosity.Verbose)
        {
            _out.WriteLine($"     EVENT  {step.EventType}");
            if (payload is not null)
            {
                _out.WriteLine("     Payload:");
                _out.WriteLine(Indent(PrettyJson(payload), 5));
            }
            _out.Write("     Result: ");
        }
        else
        {
            _out.Write($"     EVENT  {step.EventType}  =>  ");
            _out.ResetColor();
        }
    }

    private void PrintEventStepResult()
    {
        _out.ForegroundColor = ConsoleColor.Green;
        _out.WriteLine("Published");
        _out.ResetColor();
    }

    private void PrintError(string message)
    {
        _out.ForegroundColor = ConsoleColor.Red;
        _out.WriteLine($"  ERROR: {message}");
        _out.ResetColor();
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

    private void ExtractValues(Step step, string responseBody, Dictionary<string, string> context)
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
                _out.WriteLine($"  [warn] Could not extract '{key}' using path '{jsonPath}'");
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
