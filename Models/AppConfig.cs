namespace LearnerDataStorybook.Models;

public class AppConfig
{
    public string BaseUrl { get; set; } = "https://localhost:7091";
    public Verbosity Verbosity { get; set; } = Verbosity.Normal;
    public string? ServiceBusNamespace { get; set; }
    public Dictionary<string, string> Connections { get; set; } = [];
    public List<DatabaseWipeConfig> DatabaseWipes { get; set; } = [];
}

public class DatabaseWipeConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ScriptFile { get; set; } = string.Empty;
}

public enum Verbosity
{
    Quiet,
    Normal,
    Verbose
}
