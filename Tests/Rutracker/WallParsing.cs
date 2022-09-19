using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
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

        JArray ParseOne(HtmlNode post)
        {
            var topicId = post.GetTopicId();
            var result = post.ParseWall();
            foreach (var section in result.OfType<JObject>())
            {
                section.AddFirst(new JProperty("url", 
                    $"https://rutracker.org/forum/viewtopic.php?t={topicId}"));
                section.AddFirst(new JProperty("topic-id", topicId));
            }
            return result;
        }

        await Output.SaveJson(
            posts.Select(html => html.ParseHtml())
                .Select(ParseOne)
                .ToList());
    }
}