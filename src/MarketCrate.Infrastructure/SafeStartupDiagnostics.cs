using MarketCrate.Core;
namespace MarketCrate.Infrastructure;

public static class SafeStartupDiagnostics
{
    // Fixed schema only. No exception text, paths, account/item names or arbitrary messages are accepted.
    public static void StorageFailure(string appDirectory)
    {
        try
        {
            var directory = Path.Combine(appDirectory, "logs"); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "startup.log");
            if (File.Exists(path) && new FileInfo(path).Length > 16384) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {DiagnosticCode.StorageFailure}\n");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
