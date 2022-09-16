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
        try
        {
            foreach (var topic in topics!)
                if (topic.Title != null)
                {
                    await Task.Delay(100);
                    result[topic.Title] = new[]
                    {
                        await VseAudioknigiCom(topic),
                        await ReadliNet(topic),
                        await FanlabRu(topic),
                        await AuthorToday(topic)
                    }.OfType<string>().ToList();
                }

        }
        catch (Exception)
        {
            // skip
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
            new HttpRequestMessage(HttpMethod.Get, 
                $"https://readli.net/srch/?q={HttpUtility.UrlEncode(q)}")
            {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0" },
                    { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8" },
                    { "Accept-Language", "en-US,en;q=0.5" },
                    { "Referer", "https://readli.net/lyudi-i-bogi-tom-1/" },
                    { "Connection", "keep-alive" },
                    { "Cookie", "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D" },
                    { "Upgrade-Insecure-Requests", "1" },
                    { "Sec-Fetch-Dest", "document" },
                    { "Sec-Fetch-Mode", "navigate" },
                    { "Sec-Fetch-Site", "same-origin" },
                }
            }
);
        var selectSingleNode = html.ParseHtml()
            .SelectSingleNode("//div[@id='books']/article//img");
        return selectSingleNode?.GetAttributeValue("alt", null);
    }

    private static async Task<string?> FanlabRu(Story topic)
    {
        var q = topic.Title;
        var html = await Html.Get($"fantlab.ru/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get, 
            $"https://fantlab.ru/searchmain?searchstr={HttpUtility.UrlEncode(q)}") {
           //"https://fantlab.ru/searchmain?searchstr=%D0%9C%D0%B0%D1%80%D0%BA%D1%83%D1%81.%20%D0%9C%D0%B0%D0%B3%20%D0%B8%D0%B7%20%D0%B4%D1%80%D1%83%D0%B3%D0%BE%D0%B3%D0%BE%20%D0%BC%D0%B8%D1%80%D0%B0%20(%D0%9A%D0%BD%D0%B8%D0%B3%D0%B0%2001-05)") {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0" },
                    { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8" },
                    { "Accept-Language", "en-US,en;q=0.5" },
                    { "Connection", "keep-alive" },
                    { "Cookie", "_ym_uid=166312055796018485; _ym_d=1663120557; _ym_isad=1" },
                    { "Upgrade-Insecure-Requests", "1" },
                    { "Sec-Fetch-Dest", "document" },
                    { "Sec-Fetch-Mode", "navigate" },
                    { "Sec-Fetch-Site", "cross-site" },
                }
            });
        if (html.Contains("Ничего не найдено."))
            return null;
        var selectSingleNode = html.ParseHtml()
            .SelectSingleNode("//div[@class='one']/div[@class='title']/a")?
            .InnerHtml;
        return selectSingleNode;
    }
    private static async Task<string?> AuthorToday(Story topic)
    {
        var q = topic.Title;
        var html = await Html.Get($"author.today/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get, 
                $"https://author.today/search?category=works&q={HttpUtility.UrlEncode(q)}")
            {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0" },
                    { "Cookie", "cf_clearance=A5_mD_kB1YWIoFzfEeU0zIbqvj.z10Msu0RtAitJLqs-1663341274-0-150; backend_route=web08; mobile-mode=false; CSRFToken=R5hHyUTpt0v1r-AfEUQq3d8VQBMc87bWeVNyYVoXZ24kEAsFxSb_fJqUmZEmBrXkELGfvGl2VMberYKix1z-tF3oL2A1; ngSessnCookie=AAAAANqSJGN1LP2AAZMgBg==" },              
                }
            });


        var singleNode = html.ParseHtml()
            .SelectSingleNode("//div[@class='book-row']//div[@class='book-title']");
        return singleNode?.InnerText.Trim();
    }
}