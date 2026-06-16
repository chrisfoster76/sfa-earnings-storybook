namespace LearnerDataStorybook.Models;

public class Story
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExtraDescription { get; set; } = string.Empty;
    public bool WipeOnRun { get; set; } = true;
    public List<string> Tags { get; set; } = [];
    public List<Step> Steps { get; set; } = [];
    public List<Assertion> Assertions { get; set; } = [];
}

public record StoryEntry(Story Story, string FolderPath, string[] CategoryPath)
{
    public string Id => Path.GetFileName(FolderPath);
}
