using Tests.Html;
using Tests.Rutracker;
using StringExtensions = ServiceStack.StringExtensions;

namespace Tests.BookUnification;

public sealed class SearchEngine
{
    private readonly string _name;
    private readonly Func<Http, Story, string, Task<SearchResult>> _search;
    public Uri Uri { get; }
    private readonly SqliteCache _cache;

    public SearchEngine(string name,
        Func<Http, Story, string, Task<SearchResult>> search, string url,
        CachingStrategy cachingStrategy)
    {
        _name = name;
        _search = search;
        Uri = new Uri(url);
        _cache = new SqliteCache(cachingStrategy);
    }

    public async Task<SearchResult> Search(Http http, Story topic, string q)
    {
        var key = $"SearchResult: {_name}: {q}";
        var cache = await _cache.TryGetValue(key);
        if (cache != null) return StringExtensions.FromJsv<SearchResult>(cache);
        var result = await _search(http, topic, q);
        await _cache.SaveValue(key, StringExtensions.ToJsv(result));
        return result;
    }
}