using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.BookUnification;

public static class SearchResultManagement
{
    public enum Outcome
    {
        Positive,
        Negative
    }

    public static async Task Save<T>(int id, T obj, Outcome outcome)
    {
        if (outcome == Outcome.Positive)
        {
            if (!File.Exists(FileName(id)))
                return;
        }

        await FileName(id).SaveJson(obj);
    }

    private static async Task<Outcome?> GetOutcome(int id)
    {
        if (!File.Exists(FileName(id)))
            return null;
        var readJson = await FileName(id).ReadJson<Dictionary<string, object>>();
        if (readJson == null)
            return null;
        return readJson.ContainsKey("result")
            ? Outcome.Positive
            : Outcome.Negative;
    }

    public enum RefreshWhen
    {
        Always,
        Missing,
        Positives,
        Negatives,
    }

    private static string FileName(int id) =>
        $@"C:\temp\TorrentsExplorerData\Extract\SearchResult\{id:D8}.json";

    public static async Task<bool> NeedToContinue(
        Story topic, RefreshWhen refreshWhen, int preferId = 6257895)
    {
        if (topic.TopicId == preferId) return true;
        return refreshWhen switch
        {
            RefreshWhen.Always => true,
            RefreshWhen.Missing => await GetOutcome(topic.TopicId) is null,
            RefreshWhen.Positives => await GetOutcome(topic.TopicId) is Outcome.Positive or null,
            RefreshWhen.Negatives => await GetOutcome(topic.TopicId) is Outcome.Negative or null,
            _ => throw new ArgumentOutOfRangeException(nameof(refreshWhen), refreshWhen, null)
        };
    }
}