using LearnerDataStorybook.Models;
using Newtonsoft.Json;

namespace LearnerDataStorybook.Services;

public class StoryLoader
{
    private static readonly string StoriesRoot =
        Path.Combine(AppContext.BaseDirectory, "stories");

    public List<StoryEntry> LoadAll()
    {
        if (!Directory.Exists(StoriesRoot))
            return [];

        var entries = new List<StoryEntry>();
        var loadErrors = 0;

        foreach (var storyFile in Directory.GetFiles(StoriesRoot, "story.json", SearchOption.AllDirectories).Order())
        {
            var dir = Path.GetDirectoryName(storyFile)!;
            try
            {
                var story = JsonConvert.DeserializeObject<Story>(File.ReadAllText(storyFile));
                if (story is not null)
                    entries.Add(new StoryEntry(story, dir, CategoryPath(dir)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [warn] Could not load {storyFile}: {ex.Message}");
                loadErrors++;
            }
        }

        if (loadErrors > 0)
        {
            Console.WriteLine($"\nPress any key to continue...");
            Console.ReadKey(intercept: true);
        }

        return entries;
    }

    private string[] CategoryPath(string storyDir)
    {
        var parts = Path.GetRelativePath(StoriesRoot, storyDir)
            .Split(Path.DirectorySeparatorChar);
        return parts.Length > 1 ? parts[..^1] : [];
    }
}
