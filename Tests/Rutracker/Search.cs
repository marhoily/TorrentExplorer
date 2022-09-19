using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Tests.Html;
using Tests.Utilities;
using Xunit.Abstractions;
using static System.StringComparison;
using static Tests.Rutracker.SearchResultManagement;

namespace Tests.Rutracker;

public class Search
{
    private readonly ITestOutputHelper _testOutputHelper;

    private static readonly Http Html = new(
        new SqliteCache(CachingStrategy.Normal),
        Encoding.Default);

    public Search(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Search\output.json";

    [Fact]
    public async Task Do()
    {
        var negative = 0;
        var positive = 0;
        var topics = await CherryPickOnJson.Output.ReadJson<List<Story>>();
        var circuitBreaker = new CircuitBreaker();
        foreach (var topic in topics!)
        {
            if (topic.Title == null) continue;
            if (topic.Title.StartsWith("Цикл", OrdinalIgnoreCase)) continue;
            if (topic.Title.StartsWith("Серия", OrdinalIgnoreCase)) continue;
            if (topic.Title.Contains("Полный Сезон", OrdinalIgnoreCase)) continue;
            if (topic.Title.Contains("Антология", OrdinalIgnoreCase)) continue;
            if (topic.NumberInSeries is { } n)
                if (Regex.IsMatch(n, "\\s*\\d+\\s*-\\s*\\d+\\s*"))
                    continue;
            if (await GoThroughSearchEngines(circuitBreaker, topic))
                positive++;
            else
                negative++;
        }

        _testOutputHelper.WriteLine($"Positive: {positive}; Negative: {negative}");
    }

    [Fact]
    public void CompressIfPossibleTest()
    {
        "Н Е Ч Е Л О В Е К".CompressIfPossible().Should().Be("НЕЧЕЛОВЕК");
        "НЕ Ч Е Л О В  Е К".CompressIfPossible().Should().Be("НЕ Ч Е Л О В  Е К");
        "Н Ч".CompressIfPossible().Should().Be("НЧ");
    }

    private static async Task<bool> GoThroughSearchEngines(CircuitBreaker circuitBreaker, Story topic)
    {
        var q = GetQuery(topic);
        if (!await NeedToContinue(topic, RefreshWhen.Always, 1536667))
            return false;

        var negativeSearchResults = new List<SearchResult>();
        foreach (var searchEngine in SearchEngines.List)
        {
            var finished = await circuitBreaker.Execute(async () =>
            {
                var results = await searchEngine.Search(Html, topic, q);
                var result = results.Items.FirstOrDefault(
                    result => result.ValidateSearchResultMatches(topic));
                if (result != null)
                {
                    await Save(topic.TopicId, new { topic, q, result }, Outcome.Positive);
                    return true;
                }

                negativeSearchResults.Add(results);
                return false;
            });
            if (finished) return true;
        }

        await Save(topic.TopicId, new { topic, q, negativeSearchResults }, Outcome.Negative);
        return false;
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