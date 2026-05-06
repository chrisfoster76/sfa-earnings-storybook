using LearnerDataStorybook.Models;

namespace LearnerDataStorybook.Services;

public class MenuNavigator
{
    // ── Tree model ───────────────────────────────────────────────────────────

    private abstract record Node(string Label);
    private record FolderNode(string Label, List<Node> Children) : Node(Label);
    private record StoryNode(string Label, StoryEntry Entry) : Node(Label);

    // ── Flat display row (built once from the tree) ──────────────────────────

    private record DisplayRow
    {
        public string TreePrefix { get; init; } = "";
        public bool IsFolder { get; init; }
        public bool IsBlank { get; init; }
        public string Label { get; init; } = "";
        public int ChildStoryCount { get; init; }
        public StoryEntry? Entry { get; init; }

        public static DisplayRow Blank() => new() { IsBlank = true };
    }

    // ── Constants ────────────────────────────────────────────────────────────

    private static readonly string[] GroupOrder =
        ["Short Course", "Apprenticeship", "Change of Circumstance"];

    // Lines consumed outside the tree viewport:
    //   header (6) + blank-before-tree (1) + blank-after-tree (1)
    //   + separator (1) + detail (4) + separator (1) + hints (1) = 15
    private const int ReservedLines = 15;

    // ── State ────────────────────────────────────────────────────────────────

    private readonly List<DisplayRow> _rows;
    private readonly Action _renderHeader;
    private int _index;

    // ── Construction ─────────────────────────────────────────────────────────

    public MenuNavigator(List<StoryEntry> stories, Action renderHeader)
    {
        _renderHeader = renderHeader;
        _rows = BuildDisplayRows(BuildTree(stories));
        // Start on the first selectable (non-blank, non-folder) row
        _index = _rows.FindIndex(r => !r.IsBlank && !r.IsFolder);
        if (_index < 0) _index = 0;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Runs the interactive menu. Returns the chosen story, or null if the user quits.</summary>
    public StoryEntry? Run()
    {
        Render();
        while (true)
        {
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    Move(-1);
                    Render();
                    break;

                case ConsoleKey.DownArrow:
                    Move(1);
                    Render();
                    break;

                case ConsoleKey.Enter:
                    if (_rows[_index].Entry is { } entry) return entry;
                    break;

                case ConsoleKey.Q when key.Modifiers == 0:
                    return null;
            }
        }
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void Move(int delta)
    {
        var next = _index + delta;
        while (next >= 0 && next < _rows.Count && (_rows[next].IsBlank || _rows[next].IsFolder))
            next += delta;
        if (next >= 0 && next < _rows.Count)
            _index = next;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    private void Render()
    {
        Console.Clear();
        _renderHeader();

        var width = Math.Max(40, Console.WindowWidth - 1);
        var treeLines = Math.Max(5, Console.WindowHeight - ReservedLines);

        // Viewport: keep selected row centred
        var scrollStart = Math.Clamp(_index - treeLines / 2, 0, Math.Max(0, _rows.Count - treeLines));

        Console.WriteLine();

        for (int i = scrollStart; i < Math.Min(scrollStart + treeLines, _rows.Count); i++)
        {
            var row = _rows[i];

            if (row.IsBlank)
            {
                Console.WriteLine();
                continue;
            }

            var isSelected = i == _index;
            if (isSelected) Console.ForegroundColor = ConsoleColor.Cyan;

            var cursor = isSelected ? "►" : " ";

            if (row.IsFolder)
            {
                Console.WriteLine($" {cursor} {row.TreePrefix}📁 {row.Label}");
            }
            else
            {
                // Truncate long names to fit the terminal
                var maxLabel = width - row.TreePrefix.Length - 5;
                var label = row.Label.Length > maxLabel
                    ? row.Label[..(maxLabel - 1)] + "…"
                    : row.Label;
                var noWipe = row.Entry!.Story.WipeOnRun ? "" : "  [no wipe]";
                Console.WriteLine($" {cursor} {row.TreePrefix}{label}{noWipe}");
            }

            if (isSelected) Console.ResetColor();
        }

        // ── Detail panel ─────────────────────────────────────────────────────

        Console.WriteLine();
        Console.WriteLine(new string('─', width));

        var sel = _rows[_index];

        if (sel.IsFolder)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  📁 {sel.Label}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  —  {sel.ChildStoryCount} {(sel.ChildStoryCount == 1 ? "story" : "stories")}");
            Console.ResetColor();
        }
        else if (sel.Entry is { } selectedEntry)
        {
            var story = selectedEntry.Story;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  {story.Name}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {selectedEntry.Id}");
            Console.ResetColor();

            Console.WriteLine();

            var hasDesc = !string.IsNullOrWhiteSpace(story.Description);
            var hasExtra = !string.IsNullOrWhiteSpace(story.ExtraDescription);

            if (hasDesc)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                WriteWrapped(story.Description, width - 4);
                Console.ResetColor();
            }

            if (hasExtra)
            {
                if (hasDesc) Console.WriteLine();
                WriteWrapped(story.ExtraDescription, width - 4);
            }

            if (!hasDesc && !hasExtra)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  (no description)");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('─', width));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ↑/↓ navigate    Enter run story    Q quit");
        Console.ResetColor();
    }

    private static void WriteWrapped(string text, int maxWidth)
    {
        var words = text.Split(' ');
        var line = "  ";
        foreach (var word in words)
        {
            if (line.Length + word.Length + 1 > maxWidth && line.Length > 2)
            {
                Console.WriteLine(line);
                line = "  " + word;
            }
            else
            {
                if (line.Length > 2) line += " ";
                line += word;
            }
        }
        if (line.Length > 2) Console.WriteLine(line);
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private static List<DisplayRow> BuildDisplayRows(FolderNode root)
    {
        var rows = new List<DisplayRow>();
        var first = true;
        foreach (var child in root.Children)
        {
            if (child is not FolderNode group) continue;
            if (!first) rows.Add(DisplayRow.Blank());
            first = false;

            rows.Add(new DisplayRow
            {
                IsFolder = true,
                Label = group.Label,
                ChildStoryCount = CountStories(group)
            });

            AppendChildRows(group.Children, rows, "   ");
        }
        return rows;
    }

    private static void AppendChildRows(List<Node> nodes, List<DisplayRow> rows, string linePrefix)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var isLast = i == nodes.Count - 1;
            var connector = isLast ? "└─ " : "├─ ";
            var childPrefix = linePrefix + (isLast ? "   " : "│  ");

            if (node is FolderNode folder)
            {
                rows.Add(new DisplayRow
                {
                    TreePrefix = linePrefix + connector,
                    IsFolder = true,
                    Label = folder.Label,
                    ChildStoryCount = CountStories(folder)
                });
                AppendChildRows(folder.Children, rows, childPrefix);
            }
            else if (node is StoryNode story)
            {
                rows.Add(new DisplayRow
                {
                    TreePrefix = linePrefix + connector,
                    IsFolder = false,
                    Label = story.Label,
                    Entry = story.Entry
                });
            }
        }
    }

    private static int CountStories(FolderNode folder)
    {
        int count = 0;
        foreach (var child in folder.Children)
        {
            if (child is StoryNode) count++;
            else if (child is FolderNode sub) count += CountStories(sub);
        }
        return count;
    }

    private static FolderNode BuildTree(List<StoryEntry> stories)
    {
        var rootChildren = new List<Node>();

        var groups = stories
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Story.Group) ? "Other" : s.Story.Group)
            .OrderBy(g => GroupSortKey(g.Key))
            .ThenBy(g => g.Key);

        foreach (var group in groups)
        {
            var groupChildren = new List<Node>();

            foreach (var entry in group
                .Where(s => string.IsNullOrWhiteSpace(s.Story.SubGroup))
                .OrderBy(s => s.Story.Name))
                groupChildren.Add(new StoryNode(entry.Story.Name, entry));

            foreach (var subGroup in group
                .Where(s => !string.IsNullOrWhiteSpace(s.Story.SubGroup))
                .GroupBy(s => s.Story.SubGroup!)
                .OrderBy(sg => sg.Key))
            {
                var subChildren = subGroup
                    .OrderBy(e => e.Story.Name)
                    .Select(e => (Node)new StoryNode(e.Story.Name, e))
                    .ToList();
                groupChildren.Add(new FolderNode(subGroup.Key, subChildren));
            }

            rootChildren.Add(new FolderNode(group.Key, groupChildren));
        }

        return new FolderNode(string.Empty, rootChildren);
    }

    private static int GroupSortKey(string name)
    {
        var i = Array.IndexOf(GroupOrder, name);
        return i >= 0 ? i : GroupOrder.Length;
    }
}
