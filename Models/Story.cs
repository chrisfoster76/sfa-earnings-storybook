namespace LearnerDataStorybook.Models;

public class Story
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool WipeOnRun { get; set; } = true;
    public List<Step> Steps { get; set; } = [];
}

public record StoryEntry(Story Story, string FolderPath);
