using LearnerDataStorybook.Models;

namespace LearnerDataStorybook.Services;

public static class RunAllView
{
    // Column widths for story name and group (not including leading icon+space).
    private const int NameW  = 40;
    private const int GroupW = 22;

    public static async Task ShowAsync(
        List<StoryEntry> stories,
        StoryRunner runner,
        DatabaseWiper wiper,
        Action renderHeader)
    {
        Console.Clear();
        renderHeader();

        var runnable = stories.Where(s => s.Story.WipeOnRun).ToList();
        var skipped  = stories.Where(s => !s.Story.WipeOnRun).ToList();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {runnable.Count} stories to run · {skipped.Count} skipped (no wipeOnRun)");
        Console.ResetColor();
        Console.WriteLine();

        // Table header
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {"  Story".PadRight(NameW + 2)}  {"Group".PadRight(GroupW)}  {"Steps",-5}  Assertions");
        Console.WriteLine("  " + new string('─', NameW + GroupW + 22));
        Console.ResetColor();

        int passCount = 0, failCount = 0;

        for (int i = 0; i < runnable.Count; i++)
        {
            var entry = runnable[i];

            // Print pending indicator (no newline — will be overwritten with result)
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(PendingRow(entry));
            Console.ResetColor();

            // Wipe silently before each story
            var savedOut = Console.Out;
            Console.SetOut(TextWriter.Null);
            try   { await wiper.WipeAllAsync(); }
            finally { Console.SetOut(savedOut); }

            // Run story, suppressing all step-level output
            StoryRunResult result;
            try
            {
                result = await runner.WithOutput(new NullConsoleWriter()).RunAsync(entry, skipWaits: true);
            }
            catch (Exception ex)
            {
                _ = ex;
                result = new StoryRunResult(false, 0, entry.Story.Assertions.Count, TimeSpan.Zero);
            }

            // Overwrite the pending row with the result
            var ok = result.AllPassed;
            Console.Write('\r');
            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write(ResultRow(entry, result));
            Console.ResetColor();
            Console.WriteLine();

            if (ok) passCount++; else failCount++;
        }

        // Skipped stories
        foreach (var entry in skipped)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(SkippedRow(entry));
            Console.ResetColor();
        }

        // Footer
        Console.WriteLine();
        Console.WriteLine("  " + new string('─', NameW + GroupW + 22));
        Console.ForegroundColor = failCount == 0 ? ConsoleColor.Green : ConsoleColor.Red;

        var summary = $"  {passCount} passed";
        if (failCount > 0) summary += $" · {failCount} failed";
        if (skipped.Count > 0) summary += $" · {skipped.Count} skipped (no wipeOnRun)";
        Console.WriteLine(summary);

        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Press any key to return to the menu...");
        Console.ResetColor();
        Console.ReadKey(true);
    }

    // ── Row builders ──────────────────────────────────────────────────────────

    // Pending row: shows the story is about to run (no newline written by caller).
    private static string PendingRow(StoryEntry entry) =>
        $"  · {Trunc(entry.Story.Name, NameW).PadRight(NameW)}  {Trunc(Group(entry), GroupW).PadRight(GroupW)}";

    // Result row: fixed width so it fully overwrites the pending row on \r.
    private static string ResultRow(StoryEntry entry, StoryRunResult result)
    {
        var icon       = result.AllPassed ? "✓" : "✗";
        var stepsStr   = result.StepsSucceeded ? "✓" : "✗";
        var assertStr  = result.AssertionsTotal == 0 ? "—" : $"{result.AssertionsPassed}/{result.AssertionsTotal}";
        var duration   = $"({result.Duration.TotalSeconds:0.0}s)";
        var name       = Trunc(entry.Story.Name, NameW).PadRight(NameW);
        var group      = Trunc(Group(entry), GroupW).PadRight(GroupW);
        return $"  {icon} {name}  {group}  {stepsStr,-5}  {assertStr,-10}  {duration}";
    }

    private static string SkippedRow(StoryEntry entry)
    {
        var name  = Trunc(entry.Story.Name, NameW).PadRight(NameW);
        var group = Trunc(Group(entry), GroupW).PadRight(GroupW);
        return $"  — {name}  {group}  {"—",-5}  skipped";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Group(StoryEntry entry) =>
        string.Join(" / ", entry.CategoryPath.Select(p =>
            string.Concat(p.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()))));

    private static string Trunc(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..(maxLen - 1)] + "…";
}
