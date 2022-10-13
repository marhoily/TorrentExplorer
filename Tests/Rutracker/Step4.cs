using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class Step4
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4.json";

    [Fact]
    public async Task SelectPoliglot()
    {
        var posts = await Step3.Output.ReadJson<Story[]>();
        await Output.SaveJson(posts!
            .Where(p=> p.ReleaseGroup == "Полиглот")
            .GroupBy(p=>p.Genre)
            .ToDictionary(g => $"{g.Key ?? "???"} {g.Count()}"));
    }
}