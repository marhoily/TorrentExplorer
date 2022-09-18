using System.Text;
using Tests.Html;
using Tests.Utilities;
using static Tests.Rutracker.SearchResultManagement;

namespace Tests.Rutracker;

public class Search
{
    private static readonly Http Html = new(
        new SqliteCache(CachingStrategy.AlwaysMiss),
        Encoding.Default);


    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Search\output.json";
    /*
    [Fact]
    public async Task ChangeFormat()
    {
        var negative = @"C:\temp\TorrentsExplorerData\Extract\SearchResults";
        foreach (var n in Directory.GetFiles(negative))
            yield return await n.ReadJson<Dictionary<string, string>>();
        var positive = @"C:\temp\TorrentsExplorerData\Extract\Search-Positive";
        foreach (var p in Directory.GetFiles(positive))
            yield return await p.ReadJson<Dictionary<string, string>>();

    }  */  

    [Fact]
    public async Task Do()
    {
        var topics = await CherryPickOnJson.Output.ReadJson<List<Story>>();
        var circuitBreaker = new CircuitBreaker();
        foreach (var topic in topics!)
            if (topic.Title != null)
                await GoThroughSearchEngines(circuitBreaker, topic);
    }

    private static async Task GoThroughSearchEngines(CircuitBreaker circuitBreaker, Story topic)
    {
        var q = GetQuery(topic);
        if (!await NeedToContinue(topic, q, RefreshWhen.Always))
            return;

        var negativeSearchResults = new List<SearchResult>();
        foreach (var searchEngine in SearchEngines.List)
        {
            var finished = await circuitBreaker.Execute(async () =>
            {
                var result = await searchEngine(Html, topic, q);
                if (result == null) return false;

                if (result.ValidateSearchResultMatches(topic))
                {
                    await Save(topic.TopicId, new { topic, q, result }, Outcome.Positive);
                    return true;
                }

                negativeSearchResults.Add(result);
                return false;
            });
            if (finished) return;
        }

        await Save(topic.TopicId, new { topic, q, negativeSearchResults }, Outcome.Negative);
    }

    private static string GetQuery(Story topic)
    {
        var title = topic.Title;
        if (title!.StartsWith("Книга "))
        {
            var n = title["Книга ".Length..].Trim().TryParseIntOrWord();
            if (n != null && n == topic.NumberInSeries?.ParseInt())
            {
                //title = "Том " + n + ". " + title;
                title = topic.Series + ". " + title;
            }
        }

        return title + " - " + topic.Author;
    }
}