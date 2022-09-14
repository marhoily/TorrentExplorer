using Tests.Utilities;

namespace Tests;

public sealed class HtmlCache
{
    private readonly CachingStrategy _strategy;
    private readonly string _folder;

    public HtmlCache(CacheLocation location, CachingStrategy strategy)
    {
        _strategy = strategy;
        var locationPath = location switch
        {
            CacheLocation.Temp => @"C:\temp",
            CacheLocation.OneDrive => Environment.GetEnvironmentVariable("OneDriveConsumer") ??
                                      throw new Exception("One drive folder is not available"),
            CacheLocation.SystemTemp => Path.GetTempPath(),
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
        _folder = Path.Combine(locationPath, "TorrentsExplorerData", "HtmlCache");
    }

    private string Id(string key) =>
        $"{_folder}\\{key.Replace("/", "\\")}.html";

    public async Task<string?> TryGetValue(string key) =>
        _strategy == CachingStrategy.Normal
            ? await Id(key).ReadAllTextOrNullAsync()
            : null;

    public async Task SaveValue(string key, string value) => 
        await Id(key).WriteAllTextAsync(value);
}