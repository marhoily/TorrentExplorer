using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using Tests.Utilities;

namespace Tests.Html;

public interface IHtmlCache
{
    Task<string?> TryGetValue(string key);
    Task SaveValue(string key, string value);
}

public sealed record CacheLine([property: PrimaryKey] string Key, string Value);
public sealed class SqliteCache : IHtmlCache
{
    private readonly IDbConnection _db;

    public SqliteCache()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            @"C:\temp\TorrentsExplorerData\HtmlCache.db",
            SqliteDialect.Provider);
        _db = dbFactory.OpenDbConnection();
        _db.CreateTableIfNotExists<CacheLine>();
    }

    public async Task<string?> TryGetValue(string key)
    {
        var cacheLine = await _db.SingleByIdAsync<CacheLine>(key);
        return cacheLine?.Value;
    }

    public async Task SaveValue(string key, string value)
    {
        await _db.SaveAsync(new CacheLine(key, value));
    }
}

public sealed class HtmlCache : IHtmlCache
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