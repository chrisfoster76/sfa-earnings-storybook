using LearnerDataStorybook.Models;
using LearnerDataStorybook.Services;
using Microsoft.Extensions.Configuration;
using NServiceBus;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.development.json", optional: true)
    .Build();

var appConfig = config.Get<AppConfig>() ?? new AppConfig();
var loader = new StoryLoader();
var wiper = new DatabaseWiper(appConfig);

IEndpointInstance? endpointInstance = null;
if (!string.IsNullOrWhiteSpace(appConfig.ServiceBusNamespace))
{
    Console.WriteLine("  Connecting to Service Bus...");
    endpointInstance = await NServiceBusService.CreateEndpointAsync(appConfig.ServiceBusNamespace);
}

var runner = new StoryRunner(appConfig, endpointInstance);

Console.Clear();
PrintHeader();

while (true)
{
    var stories = loader.LoadAll();

    if (stories.Count == 0)
    {
        Console.WriteLine("No stories found. Add a subfolder under stories/ with a story.json file.");
        break;
    }

    Console.WriteLine("Available stories:");
    for (int i = 0; i < stories.Count; i++)
    {
        var s = stories[i].Story;
        var desc = string.IsNullOrWhiteSpace(s.Description) ? "" : $"  —  {s.Description}";
        var wipeIndicator = s.WipeOnRun ? "" : "  [no wipe]";
        Console.WriteLine($"  {i + 1}. {s.Name}{desc}{wipeIndicator}");
    }
    Console.WriteLine("  Q. Quit");
    Console.WriteLine();
    Console.Write("Select a story: ");

    var input = Console.ReadLine()?.Trim();
    Console.WriteLine();

    if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
        break;

    if (int.TryParse(input, out int choice) && choice >= 1 && choice <= stories.Count)
    {
        var entry = stories[choice - 1];
        if (entry.Story.WipeOnRun)
            await wiper.WipeAllAsync();
        await runner.RunAsync(entry);
    }
    else
    {
        Console.WriteLine("  Invalid selection.");
    }

    Console.WriteLine();
    Console.Write("Press any key to return to menu...");
    Console.ReadKey(true);
    Console.Clear();
    PrintHeader();
}

if (endpointInstance is not null)
    await endpointInstance.Stop().ConfigureAwait(false);

void PrintHeader()
{
    Console.WriteLine("╔══════════════════════════════╗");
    Console.WriteLine("║   Learner Data Storybook     ║");
    Console.WriteLine("╚══════════════════════════════╝");
    Console.WriteLine($"  Base URL : {appConfig.BaseUrl}");
    Console.WriteLine($"  Verbosity: {appConfig.Verbosity}");
    Console.WriteLine();
}
