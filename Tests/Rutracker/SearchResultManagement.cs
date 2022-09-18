using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public static class SearchResultManagement
{
    public enum Outcome
    {
        Positive,
        Negative
    }

    public static async Task Save<T>(int id, T obj, Outcome outcome)
    {
        var opposite = outcome == Outcome.Positive
            ? Outcome.Negative
            : Outcome.Positive;
        var oppositeName = FileName(id, opposite);
        if (File.Exists(oppositeName))
            File.Delete(oppositeName);
        await FileName(id, outcome).SaveJson(obj);
    }

    private static async Task<string?> GetOldQ(int id)
    {
        var positive = FileName(id, Outcome.Positive);
        var negative = FileName(id, Outcome.Negative);
        var jObj = File.Exists(positive)
            ? await positive.ReadJson<JObject>()
            : File.Exists(negative)
                ? await negative.ReadJson<JObject>()
                : null;
        return jObj?["q"]?.Value<string>();
    }

    public static Outcome? GetOutcome(int id)
    {
        return File.Exists(FileName(id, Outcome.Positive))
            ? Outcome.Positive
            : File.Exists(FileName(id, Outcome.Negative))
                ? Outcome.Negative
                : default;
    }

    public enum RefreshWhen
    {
        Always,
        Missing,
        Positives,
        Negatives,
        QueryStringChange,
    }

    private static string FileName(int id, Outcome outcome) =>
        $@"C:\temp\TorrentsExplorerData\Extract\Search-{outcome}\{id:D8}.json";

    public static async Task<bool> NeedToContinue(
        Story topic, string q, RefreshWhen refreshWhen, int preferId = 6257895)
    {
        if (topic.TopicId == preferId) return true;
        return refreshWhen switch
        {
            RefreshWhen.Always => true,
            RefreshWhen.Missing => GetOutcome(topic.TopicId) is null,
            RefreshWhen.Positives => GetOutcome(topic.TopicId) is Outcome.Positive or null,
            RefreshWhen.Negatives => GetOutcome(topic.TopicId) is Outcome.Negative or null,
            RefreshWhen.QueryStringChange => await GetOldQ(topic.TopicId) is { } oldQ && q != oldQ,
            _ => throw new ArgumentOutOfRangeException(nameof(refreshWhen), refreshWhen, null)
        };
    }

}