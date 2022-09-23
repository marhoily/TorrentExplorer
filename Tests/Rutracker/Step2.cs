using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class Step2
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\step-2.json";

    [Fact]
    public async Task CherryPickOnJson()
    {
        var posts = await Step1.Output.ReadJson<JObject[][]>();
        await Output.SaveJson(posts!
            .SelectMany(p => p)
            .WhereNotNull()
            .Select(section => section.ParseRussianFantasyTopic())
            .WhereNotNull()
            .ToList());
    }
}