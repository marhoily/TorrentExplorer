using Tests.Utilities;

namespace Tests.Rutracker;

public class CherryPickOnJson
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\cherry-pick.json";

    [Fact]
    public async Task Do()
    {
        var posts = await Step1.Output.ReadJson<Dictionary<string, object>[][]>();
        await Output.SaveJson(posts!
            .SelectMany(p => p)
            .WhereNotNull()
            .Select(section => section.ParseRussianFantasyTopic())
            .Where(result => result != null)
            .ToList());
    }
}