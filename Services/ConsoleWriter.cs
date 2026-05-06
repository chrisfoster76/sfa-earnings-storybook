using System.Text;

namespace LearnerDataStorybook.Services;

public interface IConsoleWriter
{
    void Write(string text);
    void WriteLine(string text = "");
    ConsoleColor ForegroundColor { get; set; }
    void ResetColor();
}

public sealed class SystemConsoleWriter : IConsoleWriter
{
    public void Write(string text) => Console.Write(text);
    public void WriteLine(string text = "") => Console.WriteLine(text);
    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set => Console.ForegroundColor = value;
    }
    public void ResetColor() => Console.ResetColor();
}

// Accumulates text per-line; fires onLine(text, color) on each WriteLine.
// ForegroundColor set before WriteLine determines the whole line's color.
public sealed class BufferedConsoleWriter : IConsoleWriter
{
    private readonly Action<string, ConsoleColor> _onLine;
    private readonly StringBuilder _pending = new();
    private ConsoleColor _color = ConsoleColor.Gray;

    public BufferedConsoleWriter(Action<string, ConsoleColor> onLine) => _onLine = onLine;

    public ConsoleColor ForegroundColor { get => _color; set => _color = value; }
    public void ResetColor() => _color = ConsoleColor.Gray;
    public void Write(string text) => _pending.Append(text);

    public void WriteLine(string text = "")
    {
        _pending.Append(text);
        var full = _pending.ToString();
        _pending.Clear();
        // Split on embedded newlines (e.g. PrettyJson/Indent in verbose output)
        foreach (var line in full.Split('\n'))
            _onLine(line.TrimEnd('\r'), _color);
        _color = ConsoleColor.Gray;
    }
}
