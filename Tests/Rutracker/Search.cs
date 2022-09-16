using System.Text;
using System.Web;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Search
{
    private static readonly Http Html = new(
        new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal),
        Encoding.Default);

    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Search\output.json";

    [Fact]
    public async Task AudioKnigi()
    {
        var topics = await CherryPick.Output.ReadJson<List<Story>>();
        var result = new Dictionary<string, List<string>>();
        foreach (var topic in topics!)
            if (topic.Title != null)
            {
                result[topic.Title] = new[]
                {
                    await VseAudioknigiCom(topic),
                    await ReadliNet(topic)
                }.OfType<string>().ToList();
            }

        await Output.SaveJson(result);
    }

    private static async Task<string?> VseAudioknigiCom(Story topic)
    {
        var q = topic.Title;
        var html = await Html.Get($"vse-audioknigi.com/{topic.TopicId:D8}",
            $"https://vse-audioknigi.com/search?text={HttpUtility.UrlEncode(q)}");
        var vseAudioknigiCom = html.ParseHtml()
            .SelectSingleNode("//li[@class='b-statictop__items_item']//a")?
            .InnerHtml;
        return vseAudioknigiCom;
    }

    private static async Task<string?> ReadliNet(Story topic)
    {
        var q = topic.Title;
        var html = await Html.Get($"readli.net/{topic.TopicId:D8}",
            $"https://readli.net/srch/?q={HttpUtility.UrlEncode(q)}");
        var selectSingleNode = html.ParseHtml()
            .SelectSingleNode("//div[@id='books']/article//img");
        return selectSingleNode?
            .GetAttributeValue("alt", null);
    }
}