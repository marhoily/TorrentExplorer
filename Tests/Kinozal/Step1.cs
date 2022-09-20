using Newtonsoft.Json.Linq;
using Tests.UniversalParsing;
using Tests.Utilities;

namespace Tests.Kinozal;

public class Step1
{
    private const string Output = @"C:\temp\TorrentsExplorerData\Extract\kinozal\Step1.json";

    [Fact]
    public async Task Convert()
    {
        var books = Step0.Output.ReadXml<KinozalBook[]>();

        static JObject Selector(KinozalBook post)
        {
            var jArray = post.Post.Xml.ParseWall();
            var jObj = (JObject)jArray[0];
            if (post.Series != null)
                jObj.Add("Цикл", post.Series.ToString());
            return jObj;
        }

        await Output.SaveJson(books!.Select(Selector));
    }
}