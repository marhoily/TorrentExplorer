using Newtonsoft.Json.Linq;
using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Kinozal;

public class KinozalStep1
{
    private const string Output = @"C:\temp\TorrentsExplorerData\Extract\kinozal\Step1.json";

    [Fact]
    public async Task Convert()
    {
        var books = await Step0.Output.ReadJson<KinozalBook[]>();

        JObject Selector(KinozalBook post)
        {
            var jArray = post.Post.Xml.ParseHtml().ParseWall();
            var jObj = (JObject)jArray[0];
            return jObj;
        }

        await Output.SaveJson(books!.Select(Selector));
    }
}