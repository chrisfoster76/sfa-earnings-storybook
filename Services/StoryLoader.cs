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

        foreach (var dir in Directory.GetDirectories(StoriesRoot).Order())
        {
            var storyFile = Path.Combine(dir, "story.json");
            if (!File.Exists(storyFile))
                continue;

            try
            {
                var story = JsonConvert.DeserializeObject<Story>(File.ReadAllText(storyFile));
                if (story is not null)
                    entries.Add(new StoryEntry(story, dir));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [warn] Could not load {storyFile}: {ex.Message}");
            }
        }

        return entries;
    }
}
