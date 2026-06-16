using LearnerDataStorybook.Models;

namespace LearnerDataStorybook.Services;

public class MenuNavigator
{
    // ── Tree model ────────────────────────────────────────────────────────────

    private abstract record Node(string Label);
    private record FolderNode(string Label, List<Node> Children) : Node(Label);
    private record StoryNode(string Label, StoryEntry Entry) : Node(Label);
    private record ActionNode(string Label, List<StoryEntry> Stories) : Node(Label);

    // ── Config ────────────────────────────────────────────────────────────────

    private const int HeaderRows = 4; // lines consumed by PrintHeader()
    private const int PanelGap   = 2; // columns between the two boxes

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly FolderNode _root;
    private readonly Action _renderHeader;
    private readonly Stack<(FolderNode Folder, int Index)> _stack = new();

    public MenuNavigator(List<StoryEntry> stories, Action renderHeader)
    {
        _renderHeader = renderHeader;
        _root = BuildTree(stories);
        _stack.Push((_root, 0));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public MenuSelection Run()
    {
        Render();
        while (true)
        {
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    Scroll(-1); Render(); break;

                case ConsoleKey.DownArrow:
                    Scroll(1); Render(); break;

                case ConsoleKey.Enter:
                case ConsoleKey.RightArrow:
                    var result = Activate();
                    if (result is not null) return result;
                    Render();
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.Backspace:
                case ConsoleKey.LeftArrow:
                    Back(); Render(); break;

                case ConsoleKey.Q when key.Modifiers == 0:
                    return new QuitSelection();
            }
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void Scroll(int delta)
    {
        var (folder, idx) = _stack.Pop();
        _stack.Push((folder, Math.Clamp(idx + delta, 0, folder.Children.Count - 1)));
    }

    private MenuSelection? Activate()
    {
        var (folder, idx) = _stack.Peek();
        return folder.Children[idx] switch
        {
            ActionNode action      => new RunAllSelection(action.Stories),
            FolderNode child       => EnterFolder(child),
            StoryNode story        => new RunStorySelection(story.Entry),
            _                      => null
        };
    }

    private MenuSelection? EnterFolder(FolderNode folder)
    {
        _stack.Push((folder, 0));
        return null;
    }

    private void Back()
    {
        if (_stack.Count > 1) _stack.Pop();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    private void Render()
    {
        Console.Clear();
        _renderHeader();

        var totalW  = Math.Max(60, Console.WindowWidth - 1);
        var leftW   = Math.Clamp(totalW * 2 / 5 + 10, 42, 62);
        var rightW  = totalW - leftW - PanelGap;
        var leftInner  = leftW  - 4;   // content cols: box width − 2 borders − 2 padding
        var rightInner = rightW - 4;

        var startRow = HeaderRows;
        var boxH   = Console.WindowHeight - startRow - 2; // −2: bottom border + hints line
        var innerH = Math.Max(3, boxH - 2);

        var (folder, selIdx) = _stack.Peek();
        var scrollTop = Math.Clamp(selIdx - innerH / 2, 0, Math.Max(0, folder.Children.Count - innerH));

        var rightContent = BuildRightContent(folder, selIdx, rightInner);

        // ── Top borders ───────────────────────────────────────────────────────

        Console.SetCursorPosition(0, startRow);
        DrawLeftTopBorder(leftW, BuildBreadcrumb());

        Console.SetCursorPosition(leftW + PanelGap, startRow);
        Console.Write("┌" + new string('─', rightW - 2) + "┐");

        // ── Content rows ──────────────────────────────────────────────────────

        for (int r = 0; r < innerH; r++)
        {
            // Left panel
            Console.SetCursorPosition(0, startRow + 1 + r);
            Console.Write("│ ");
            var itemIdx = scrollTop + r;
            if (itemIdx < folder.Children.Count)
                RenderItem(folder.Children[itemIdx], itemIdx == selIdx, leftInner);
            else
                Console.Write(new string(' ', leftInner));
            Console.Write(" │");

            // Right panel
            Console.SetCursorPosition(leftW + PanelGap, startRow + 1 + r);
            Console.Write("│ ");
            if (r < rightContent.Count)
                rightContent[r](rightInner);
            else
                Console.Write(new string(' ', rightInner));
            Console.Write(" │");
        }

        // ── Bottom borders ────────────────────────────────────────────────────

        var bottomRow = startRow + boxH - 1;
        Console.SetCursorPosition(0, bottomRow);
        Console.Write("└" + new string('─', leftW - 2) + "┘");
        Console.SetCursorPosition(leftW + PanelGap, bottomRow);
        Console.Write("└" + new string('─', rightW - 2) + "┘");

        // ── Hints ─────────────────────────────────────────────────────────────

        Console.SetCursorPosition(0, startRow + boxH);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  ↑/↓ navigate    Enter/→ open/run    ←/Esc back    Q quit");
        Console.ResetColor();
    }

    private static void DrawLeftTopBorder(int boxW, string breadcrumb)
    {
        if (string.IsNullOrEmpty(breadcrumb))
        {
            Console.Write("┌" + new string('─', boxW - 2) + "┐");
            return;
        }

        // ┌─ breadcrumb ─────┐
        var maxBcVw  = boxW - 6; // reserve: ┌─ [bc] ─┐ = 6 + bc visual cols
        var bc       = Vw(breadcrumb) > maxBcVw ? TruncToVw(breadcrumb, maxBcVw - 1) + "…" : breadcrumb;
        var fill     = Math.Max(0, boxW - 5 - Vw(bc)); // 5 = ┌ ─ space space ┐
        Console.Write("┌─ " + bc + " " + new string('─', fill) + "┐");
    }

    // Renders exactly `contentWidth` visual columns for one list item.
    private static void RenderItem(Node node, bool selected, int contentWidth)
    {
        if (selected)
        {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.Write(" ");                             // 1 col indent (highlighted when selected)

        var icon = node switch { FolderNode => "📁", ActionNode => "▶ ", _ => "📄" };
        Console.Write(icon + " ");                      // 3 visual cols (emoji=2, space=1)

        // Remaining label area = contentWidth − 1 (indent) − 3 (icon) = contentWidth − 4
        var labelArea = contentWidth - 4;
        var label     = node.Label;

        if (node is StoryNode { Entry.Story.WipeOnRun: false })
            label += "  [~]";

        var labelVw = Vw(label);
        if (labelVw > labelArea)
        {
            Console.Write(TruncToVw(label, labelArea - 1));
            Console.Write("…");
        }
        else
        {
            Console.Write(label);
            Console.Write(new string(' ', labelArea - labelVw)); // fill with highlighted bg
        }

        if (selected) Console.ResetColor();
    }

    // Builds right-panel content as a list of actions; each writes exactly w cols.
    private static List<Action<int>> BuildRightContent(FolderNode folder, int selIdx, int contentWidth)
    {
        var lines = new List<Action<int>>();

        static Action<int> Blank() =>
            w => Console.Write(new string(' ', w));

        static Action<int> Styled(string text, ConsoleColor color)
        {
            var t = text; var c = color;
            return w =>
            {
                Console.ForegroundColor = c;
                var vw = Vw(t);
                if (vw >= w) Console.Write(TruncToVw(t, w));
                else { Console.Write(t); Console.Write(new string(' ', w - vw)); }
                Console.ResetColor();
            };
        }

        if (selIdx < 0 || selIdx >= folder.Children.Count)
        {
            lines.Add(Blank());
            return lines;
        }

        var node = folder.Children[selIdx];

        if (node is ActionNode an)
        {
            var runCount  = an.Stories.Count(s => s.Story.WipeOnRun);
            var skipCount = an.Stories.Count - runCount;

            lines.Add(Styled("▶  " + an.Label, ConsoleColor.Cyan));
            lines.Add(Blank());

            var runLine = $"{runCount} {(runCount == 1 ? "story" : "stories")} will run";
            if (skipCount > 0) runLine += $" · {skipCount} skipped (no wipeOnRun)";
            lines.Add(Styled(runLine, ConsoleColor.Gray));

            lines.Add(Blank());
            lines.Add(Styled("WAIT steps are auto-skipped.", ConsoleColor.DarkGray));
            lines.Add(Blank());
            lines.Add(Styled("Press Enter or → to start.", ConsoleColor.DarkGray));
        }
        else if (node is FolderNode fn)
        {
            lines.Add(Styled("📁 " + fn.Label, ConsoleColor.Yellow));
            var count = CountStories(fn);
            lines.Add(Styled($"{count} {(count == 1 ? "story" : "stories")}", ConsoleColor.DarkGray));
            lines.Add(Blank());
            lines.Add(Styled("Press Enter or → to open", ConsoleColor.DarkGray));
        }
        else if (node is StoryNode sn)
        {
            var story = sn.Entry.Story;
            var hasDesc  = !string.IsNullOrWhiteSpace(story.Description);
            var hasExtra = !string.IsNullOrWhiteSpace(story.ExtraDescription);

            lines.Add(Styled(story.Name, ConsoleColor.Yellow));
            lines.Add(Styled(sn.Entry.Id, ConsoleColor.DarkGray));
            lines.Add(Blank());

            if (hasDesc)
                foreach (var l in Wrap(story.Description, contentWidth))
                    lines.Add(Styled(l, ConsoleColor.Gray));

            if (hasExtra)
            {
                if (hasDesc) lines.Add(Blank());
                foreach (var l in Wrap(story.ExtraDescription, contentWidth))
                    lines.Add(Styled(l, ConsoleColor.Gray));
            }

            if (!hasDesc && !hasExtra)
                lines.Add(Styled("(no description)", ConsoleColor.DarkGray));

            if (story.Tags.Count > 0)
            {
                lines.Add(Blank());
                lines.Add(TagBadgeLine(story.Tags));
            }

            var steps = story.Steps.Where(s => !s.Disabled).ToList();
            if (steps.Count > 0)
            {
                lines.Add(Blank());
                lines.Add(StepSeparator("Steps", contentWidth));
                foreach (var step in steps)
                    lines.Add(StepLine(step, contentWidth));
            }

            if (story.Assertions.Count > 0)
            {
                lines.Add(Blank());
                lines.Add(StepSeparator("Assertions", contentWidth));
                foreach (var assertion in story.Assertions)
                    lines.Add(AssertionLine(assertion, contentWidth));
            }
        }

        return lines;
    }

    // Renders tag badges: white text on blue background, space-separated.
    private static Action<int> TagBadgeLine(List<string> tags)
    {
        var t = tags;
        return w =>
        {
            int used = 0;
            foreach (var tag in t)
            {
                var badge = " " + tag + " ";
                var bw = Vw(badge);
                if (used + bw > w) break;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(badge);
                Console.ResetColor();
                used += bw;
                if (used < w) { Console.Write(" "); used++; }
            }
            if (used < w) Console.Write(new string(' ', w - used));
        };
    }

    // ── Step rendering ────────────────────────────────────────────────────────

    // "── Steps ──────────" separator line, exactly w cols.
    private static Action<int> StepSeparator(string label, int contentWidth)
    {
        var text = $"── {label} " + new string('─', Math.Max(0, contentWidth - label.Length - 5));
        return w =>
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            var vw = Vw(text);
            if (vw >= w) Console.Write(TruncToVw(text, w));
            else { Console.Write(text); Console.Write(new string(' ', w - vw)); }
            Console.ResetColor();
        };
    }

    // One step line: icon + badge + name, exactly w cols.
    private static Action<int> StepLine(Models.Step step, int contentWidth)
    {
        var s = step;
        // icon (1 col) + space (1) + badge padded to 6 + space (1) = 9 cols before name
        const int prefixCols = 9;
        return w =>
        {
            var (icon, iconColor, badge) = StepBadge(s);

            Console.ForegroundColor = iconColor;
            Console.Write(icon + " "); // 2 cols
            Console.Write(badge.PadRight(6) + " "); // 7 cols
            Console.ResetColor();

            // name in the remaining width
            var nameWidth = Math.Max(1, w - prefixCols);
            var name = s.Name;
            Console.ForegroundColor = ConsoleColor.Gray;
            var vw = Vw(name);
            if (vw > nameWidth) { Console.Write(TruncToVw(name, nameWidth - 1)); Console.Write("…"); }
            else { Console.Write(name); Console.Write(new string(' ', nameWidth - vw)); }
            Console.ResetColor();
        };
    }

    // One assertion line: icon + badge + name + expected, exactly w cols.
    private static Action<int> AssertionLine(Models.Assertion assertion, int contentWidth)
    {
        var a = assertion;
        const int prefixCols = 9; // matches StepLine prefix
        return w =>
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("? "); // 2 cols
            Console.Write(a.Type.ToUpperInvariant().PadRight(6) + " "); // 7 cols
            Console.ResetColor();

            var nameWidth = Math.Max(1, w - prefixCols);
            var expectedPart = a.Expected.Count > 0
                ? "  " + string.Join(", ", a.Expected.Select(e => $"{e.Field}={e.Value}"))
                : "";
            var text = a.Name + expectedPart;
            Console.ForegroundColor = ConsoleColor.Gray;
            var vw = Vw(text);
            if (vw > nameWidth) { Console.Write(TruncToVw(text, nameWidth - 1)); Console.Write("…"); }
            else { Console.Write(text); Console.Write(new string(' ', nameWidth - vw)); }
            Console.ResetColor();
        };
    }

    private static (string icon, ConsoleColor color, string badge) StepBadge(Models.Step step) =>
        step.Type.ToUpperInvariant() switch
        {
            "EVENT"   => ("◉", ConsoleColor.Yellow,    "EVENT"),
            "SQL"     => ("◆", ConsoleColor.Magenta,   "SQL"),
            "ASSERT"  => ("?", ConsoleColor.DarkYellow, "ASSERT"),
            "CONTEXT" => ("·", ConsoleColor.DarkGray,  "CTX"),
            _ => step.Verb.ToUpperInvariant() switch     // Http
            {
                "POST"   => ("▲", ConsoleColor.Green,   "POST"),
                "PUT"    => ("◈", ConsoleColor.Cyan,    "PUT"),
                "PATCH"  => ("◈", ConsoleColor.Cyan,    "PATCH"),
                "GET"    => ("▼", ConsoleColor.DarkGray,"GET"),
                "DELETE" => ("■", ConsoleColor.Red,     "DELETE"),
                _        => ("→", ConsoleColor.Gray,    step.Verb),
            }
        };

    // ── String/width helpers ──────────────────────────────────────────────────

    private string BuildBreadcrumb() =>
        string.Join(" › ", _stack.Reverse()
            .Select(s => s.Folder.Label)
            .Where(l => !string.IsNullOrEmpty(l)));

    private static List<string> Wrap(string text, int maxVw)
    {
        var lines = new List<string>();
        var line  = "";
        foreach (var word in text.Split(' '))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (Vw(candidate) > maxVw)
            {
                if (line.Length > 0) lines.Add(line);
                line = word;
            }
            else line = candidate;
        }
        if (line.Length > 0) lines.Add(line);
        return lines;
    }

    // Visual (terminal column) width: surrogate pairs (emoji) = 2, everything else = 1.
    private static int Vw(string s)
    {
        int w = 0, i = 0;
        while (i < s.Length)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            { w += 2; i += 2; }
            else { w += 1; i += 1; }
        }
        return w;
    }

    // Truncate string to at most maxVw visual columns (no padding).
    private static string TruncToVw(string s, int maxVw)
    {
        var sb = new System.Text.StringBuilder();
        int w = 0, i = 0;
        while (i < s.Length)
        {
            int cw, adv;
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            { cw = 2; adv = 2; }
            else { cw = 1; adv = 1; }
            if (w + cw > maxVw) break;
            sb.Append(s, i, adv); w += cw; i += adv;
        }
        return sb.ToString();
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private static int CountStories(FolderNode folder)
    {
        int n = 0;
        foreach (var child in folder.Children)
        {
            if (child is StoryNode) n++;
            else if (child is FolderNode sub) n += CountStories(sub);
        }
        return n;
    }

    private static FolderNode BuildTree(List<StoryEntry> stories)
    {
        var rootChildren = new List<Node> { new ActionNode("Run All Stories", stories) };
        AddChildNodes(rootChildren, stories, depth: 0);
        return new FolderNode(string.Empty, rootChildren);
    }

    private static void AddChildNodes(List<Node> target, List<StoryEntry> stories, int depth)
    {
        foreach (var entry in stories.Where(s => s.CategoryPath.Length == depth).OrderBy(s => s.Story.Name))
            target.Add(new StoryNode(entry.Story.Name, entry));

        foreach (var group in stories
            .Where(s => s.CategoryPath.Length > depth)
            .GroupBy(s => s.CategoryPath[depth])
            .OrderBy(g => g.Key))
        {
            var folderStories = group.ToList();
            var subChildren = new List<Node> { new ActionNode("Run All in Folder", folderStories) };
            AddChildNodes(subChildren, folderStories, depth + 1);
            target.Add(new FolderNode(ToDisplayName(group.Key), subChildren));
        }
    }

    private static string ToDisplayName(string folderName) =>
        string.Concat(folderName.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}
