using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class Step3
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\step-3.json";

    [Fact]
    public async Task CherryPickParse()
    {
        var posts = await Step2.Output.ReadJson<JObject[][]>();
        await Output.SaveJson(posts!
            .SelectMany(p => p)
            .WhereNotNull()
            .Select(section => section.ParseRussianFantasyTopic())
            .WhereNotNull()
            .ToList());
    }
}