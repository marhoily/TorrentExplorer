using Tests.Utilities;

namespace Tests.Rutracker;

public class WallParsing
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\wall.json";

    [Fact]
    public async Task Parse()
    {
        var raw = await Step0.Output.ReadJson<string[]>();
        var posts = raw ?? Array.Empty<string>();
        await Output.SaveJson(Extract(posts).ToList());
    }

    private static IEnumerable<List<Dictionary<string, object>>> Extract(string[] posts)
    {
        foreach (var post in posts.Select(html => html.ParseHtml()))
        {
            var collector = new WallCollector();
            collector.Parse(post.ChildNodes[0]);
            yield return collector.Sections;
        }
    }
}