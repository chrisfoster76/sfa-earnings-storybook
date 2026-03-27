using LearnerDataStorybook.Models;
using Microsoft.Data.SqlClient;

namespace LearnerDataStorybook.Services;

public class DatabaseWiper(AppConfig config)
{
    public async Task WipeAllAsync()
    {
        if (config.DatabaseWipes.Count == 0)
            return;

        Console.WriteLine("Wiping databases...");

        foreach (var wipe in config.DatabaseWipes)
        {
            var dbName = ExtractDatabaseName(wipe.ConnectionString);
            Console.Write($"  {dbName}... ");

            var scriptPath = Path.Combine(AppContext.BaseDirectory, wipe.ScriptFile);
            if (!File.Exists(scriptPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"skipped (script not found: {wipe.ScriptFile})");
                Console.ResetColor();
                continue;
            }

            var script = await File.ReadAllTextAsync(scriptPath);

            try
            {
                await using var conn = new SqlConnection(wipe.ConnectionString);
                await conn.OpenAsync();

                // Split on GO statements, as SqlClient doesn't support them natively
                foreach (var batch in SplitBatches(script))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    await using var cmd = new SqlCommand(batch, conn);
                    await cmd.ExecuteNonQueryAsync();
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("done");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED — {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
    }

    private static string[] SplitBatches(string script) =>
        script.Split(["\nGO", "\r\nGO"], StringSplitOptions.RemoveEmptyEntries);

    private static string ExtractDatabaseName(string connectionString)
    {
        foreach (var part in connectionString.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 &&
                (kv[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase) ||
                 kv[0].Trim().Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase)))
                return kv[1].Trim();
        }
        return connectionString;
    }
}
