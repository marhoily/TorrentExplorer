using Tests.Utilities;

namespace Tests.Rutracker;

public class CherryPick
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\cherry-pick.json";

    [Fact]
    public async Task Test1()
    {
        var htmlList = await Step0.Output.ReadJson<string[]>();
        var result = new List<Topic>();
        foreach (var htmlNode in htmlList!)
        {
            var russianFantasyTopic = htmlNode.ParseHtml().ParseRussianFantasyTopic();
            if (russianFantasyTopic != null)
                result.Add(russianFantasyTopic);
        }

        await Output.SaveJson(result);
    }
}