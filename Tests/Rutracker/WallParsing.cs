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
        await Output.SaveJson(
            posts.Select(html => html.ParseHtml())
                .Select(post => post.ChildNodes[0].ParseWall())
                .ToList());
    }
}