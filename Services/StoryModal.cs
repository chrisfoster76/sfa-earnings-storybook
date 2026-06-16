using LearnerDataStorybook.Models;

namespace LearnerDataStorybook.Services;

public static class StoryModal
{
    // Mirror MenuNavigator constants so the modal sits exactly over the right panel.
    private const int HeaderRows = 4;
    private const int PanelGap   = 2;

    public static async Task ShowAsync(StoryEntry entry, StoryRunner runner, DatabaseWiper wiper)
    {
        // Compute layout (same arithmetic as MenuNavigator.Render)
        var totalW  = Math.Max(60, Console.WindowWidth - 1);
        var leftW   = Math.Clamp(totalW * 2 / 5 + 10, 42, 62);
        var rightW  = totalW - leftW - PanelGap;
        var left    = leftW + PanelGap;
        var top     = HeaderRows;
        var boxH    = Console.WindowHeight - top - 2;
        var innerW  = rightW - 4;
        var innerH  = Math.Max(3, boxH - 2);

        var lines      = new List<(string Text, ConsoleColor Color)>();
        var renderLock = new object();

        var spinnerFrames = new[] { "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷" };
        var spinnerFrame  = 0;

        void AddLine(string text, ConsoleColor color)
        {
            lock (renderLock)
            {
                lines.Add((text, color));
                RenderContent(lines, left, top, innerW, innerH);
            }
        }

        Console.CursorVisible = false;

        try
        {
            DrawFrame(entry.Story.Name, left, top, rightW, boxH);
            RenderContent(lines, left, top, innerW, innerH);

            // Wipe — silence the wiper's own console output
            if (entry.Story.WipeOnRun)
            {
                AddLine("  ⊗ Wiping database...", ConsoleColor.DarkYellow);
                var savedOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try   { await wiper.WipeAllAsync(); }
                finally { Console.SetOut(savedOut); }
            }

            // Add the spinner line and remember its index so we can update it in-place
            int spinnerIdx;
            lock (renderLock)
            {
                spinnerIdx = lines.Count;
                lines.Add(($"  {spinnerFrames[0]} Running story...", ConsoleColor.White));
                RenderContent(lines, left, top, innerW, innerH);
            }

            // Animate spinner on a background thread while the story runs
            var cts = new CancellationTokenSource();
            var spinnerTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try { await Task.Delay(120, cts.Token); }
                    catch (TaskCanceledException) { break; }

                    lock (renderLock)
                    {
                        spinnerFrame = (spinnerFrame + 1) % spinnerFrames.Length;
                        lines[spinnerIdx] = ($"  {spinnerFrames[spinnerFrame]} Running story...", ConsoleColor.White);
                        RenderContent(lines, left, top, innerW, innerH);
                    }
                }
            });

            var buffered = new BufferedConsoleWriter(AddLine);
            var result   = await runner.WithOutput(buffered).RunAsync(entry);

            cts.Cancel();
            await spinnerTask;

            // Resolve spinner to tick or cross
            lock (renderLock)
            {
                lines[spinnerIdx] = result.AllPassed
                    ? ("  ✓ Story completed", ConsoleColor.Green)
                    : ("  ✗ Story failed",    ConsoleColor.Red);
                RenderContent(lines, left, top, innerW, innerH);
            }

            // Park cursor safely inside the modal so the terminal can't scroll
            Console.SetCursorPosition(left + 2, top + innerH);
        }
        finally
        {
            Console.ReadKey(true);
            Console.CursorVisible = true;
        }
    }

    // ── Frame ─────────────────────────────────────────────────────────────────

    private static void DrawFrame(string title, int left, int top, int rightW, int boxH)
    {
        var innerW = rightW - 4;
        Console.ResetColor();

        // Top border:  ┌─ Title ─────────────────────────────────────────────┐
        Console.SetCursorPosition(left, top);
        var maxTitle = rightW - 6; // ┌─ {title} ─┐ overhead = 5 chars, min 1 for fill
        var t    = title.Length > maxTitle ? title[..(maxTitle - 1)] + "…" : title;
        var fill = Math.Max(0, rightW - 5 - t.Length);
        Console.Write("┌─ " + t + " " + new string('─', fill) + "┐");

        // Content rows — draw borders and clear interior
        for (var r = 1; r < boxH - 1; r++)
        {
            Console.SetCursorPosition(left, top + r);
            Console.Write("│ " + new string(' ', innerW) + " │");
        }

        // Bottom border with prompt
        Console.SetCursorPosition(left, top + boxH - 1);
        const string prompt = "── Press any key to close ";
        var promptFill = Math.Max(0, rightW - 2 - prompt.Length);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("└" + prompt + new string('─', promptFill) + "┘");
        Console.ResetColor();
    }

    // ── Content ───────────────────────────────────────────────────────────────

    private static void RenderContent(
        List<(string Text, ConsoleColor Color)> lines,
        int left, int top, int innerW, int innerH)
    {
        var scrollTop = Math.Max(0, lines.Count - innerH);

        for (var r = 0; r < innerH; r++)
        {
            Console.SetCursorPosition(left + 2, top + 1 + r);
            var idx = scrollTop + r;
            if (idx < lines.Count)
            {
                var (text, color) = lines[idx];
                Console.ForegroundColor = color;
                WriteFixed(text, innerW);
            }
            else
            {
                Console.Write(new string(' ', innerW));
            }
        }

        Console.ResetColor();
    }

    private static void WriteFixed(string text, int width)
    {
        if (text.Length >= width)
            Console.Write(text[..width]);
        else
        {
            Console.Write(text);
            Console.Write(new string(' ', width - text.Length));
        }
    }
}
