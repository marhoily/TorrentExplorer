using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step1
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\wall.json";

    [Fact]
    public async Task WallParse()
    {
        await using var file = File.OpenRead(Step0.Output);
        var many = await XDocument.LoadAsync(
            file, LoadOptions.PreserveWhitespace, CancellationToken.None);

        static JArray ParseOne(XNode post)
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

        var xElements = many.Elements().Single().Elements();
        await Output.SaveJson(xElements.Select(ParseOne).ToList());
    }
}