using LearnerDataStorybook.Models;
using LearnerDataStorybook.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NServiceBus;

Console.OutputEncoding = System.Text.Encoding.UTF8;

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
    var allStories = loader.LoadAll();
    var entry = allStories.FirstOrDefault(s => s.Id == storyId);

    if (entry is null)
    {
        Console.WriteLine($"No story found with id '{storyId}'.");
        Console.WriteLine($"Available ids: {string.Join(", ", allStories.Select(s => s.Id))}");
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

var stories = loader.LoadAll();

if (stories.Count == 0)
{
    Console.Clear();
    PrintHeader();
    Console.WriteLine("No stories found. Add a subfolder under stories/ with a story.json file.");
}
else
{
    var nav = new MenuNavigator(stories, PrintHeader);
    while (true)
    {
        var entry = nav.Run();
        if (entry is null) break;

        Console.Clear();
        PrintHeader();
        if (entry.Story.WipeOnRun)
            await wiper.WipeAllAsync();
        await runner.RunAsync(entry);

        Console.WriteLine();
        Console.Write("Press any key to return to menu...");
        Console.ReadKey(true);
    }
}

if (endpointInstance is not null)
    await endpointInstance.Stop().ConfigureAwait(false);

void PrintHeader()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  Learner Data Storybook");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Base URL : {appConfig.BaseUrl}");
    Console.WriteLine($"  Verbosity: {appConfig.Verbosity}");
    Console.ResetColor();
    Console.WriteLine();
}
