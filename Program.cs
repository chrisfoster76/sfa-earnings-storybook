using LearnerDataStorybook.Models;
using LearnerDataStorybook.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

if (args.Length > 0)
{
    var command = args[0].ToLowerInvariant();

    if (command == "wipe")
    {
        PrintHeader();
        await wiper.WipeAllAsync();
        var contextFile = Path.Combine(Directory.GetCurrentDirectory(), "adhoc", "context.json");
        if (File.Exists(contextFile))
        {
            File.Delete(contextFile);
            Console.WriteLine("  Adhoc context cleared.");
        }
        if (endpointInstance is not null)
            await endpointInstance.Stop().ConfigureAwait(false);
        return;
    }

    if (command == "adhoc")
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run -- adhoc <filename>");
            if (endpointInstance is not null)
                await endpointInstance.Stop().ConfigureAwait(false);
            return;
        }

        var adhocFolder = Path.Combine(Directory.GetCurrentDirectory(), "adhoc");
        var adhocFile = Path.Combine(adhocFolder, args[1]);
        var contextFile = Path.Combine(adhocFolder, "context.json");

        if (!File.Exists(adhocFile))
        {
            Console.WriteLine($"Adhoc file not found: {adhocFile}");
            if (endpointInstance is not null)
                await endpointInstance.Stop().ConfigureAwait(false);
            return;
        }

        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(contextFile))
        {
            var saved = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(contextFile));
            if (saved is not null) context = saved;
        }

        PrintHeader();

        var step = JsonConvert.DeserializeObject<Step>(await File.ReadAllTextAsync(adhocFile))!;
        await runner.RunAdhocStepAsync(step, adhocFolder, context);

        Directory.CreateDirectory(adhocFolder);
        await File.WriteAllTextAsync(contextFile, JsonConvert.SerializeObject(context, Formatting.Indented));

        if (endpointInstance is not null)
            await endpointInstance.Stop().ConfigureAwait(false);
        return;
    }

    var storyId = args[0];
    var stories = loader.LoadAll();
    var entry = stories.FirstOrDefault(s => s.Id == storyId);

    if (entry is null)
    {
        Console.WriteLine($"No story found with id '{storyId}'.");
        Console.WriteLine($"Available ids: {string.Join(", ", stories.Select(s => s.Id))}");
        if (endpointInstance is not null)
            await endpointInstance.Stop().ConfigureAwait(false);
        return;
    }

    PrintHeader();
    if (entry.Story.WipeOnRun)
        await wiper.WipeAllAsync();
    await runner.RunAsync(entry);

    if (endpointInstance is not null)
        await endpointInstance.Stop().ConfigureAwait(false);
    return;
}

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
        Console.WriteLine($"  {i + 1}. {s.Name}  [{stories[i].Id}]{desc}{wipeIndicator}");
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
