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
    public async Task Do()
    {
        var topics = await CherryPick.Output.ReadJson<List<Story>>();

        var successCount = 0;
        var millisecondsDelay = 100;
        foreach (var topic in topics!)
            if (topic.Title != null)
            {
                var file = $@"C:\temp\TorrentsExplorerData\Extract\Search\{topic.TopicId:D8}.json";
                if (File.Exists(file)) continue;

                var title = topic.Title
                    .Replace("Том 1", "")
                    .Replace("Том 2", "")
                    .Replace("Том 3", "")
                    .Replace("Том 4", "")
                    .Replace("Том 5", "")
                    .Replace("Том 6", "")
                    .Replace("Том I", "")
                    .Replace("Том II", "")
                    .Replace("Том III", "")
                    .Replace("Том IV", "")
                    .Replace("Том V", "")
                    .Replace("Том VI", "");
                var q = title + " " + topic.Author;

                try
                {
                    await file
                        .SaveJson(new Dictionary<string, string?>
                        {
                            ["Original"] = q,
                            ["VseAudioknigi"] = await VseAudioknigiCom(topic, q),
                            ["Readli"] = await ReadliNet(topic, q),
                            ["Fanlab"] = await FanlabRu(topic, q),
                            ["AuthorToday"] = await AuthorToday(topic, q)
                        });
                    successCount++;
                }
                catch (Exception)
                {
                    await Task.Delay(millisecondsDelay = successCount switch
                    {
                        < 2 => 1000 + millisecondsDelay * 2,
                        < 5 => millisecondsDelay * 2,
                        > 20 => millisecondsDelay / 2,
                        _ => millisecondsDelay
                    });
                    successCount = 0;
                }
            }
    }

    private static async Task<string?> VseAudioknigiCom(Story topic, string q)
    {
        var html = await Html.Get($"vse-audioknigi.com/{topic.TopicId:D8}",
            $"https://vse-audioknigi.com/search?text={HttpUtility.UrlEncode(q)}");
        var vseAudioknigiCom = html.ParseHtml()
            .SelectSingleNode("//li[@class='b-statictop__items_item']//a")?
            .InnerText;
        return vseAudioknigiCom;
    }

    private static async Task<string?> ReadliNet(Story topic, string q)
    {
        var html = await Html.Get($"readli.net/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get,
                $"https://readli.net/srch/?q={HttpUtility.UrlEncode(q)}")
            {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0" },
                    { "Cookie", "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D" },
                }
            }
        );
        var article = html.ParseHtml()
            .SelectSingleNode("//div[@id='books']/article");
        if (article == null) return null;
        var title = article.SelectSingleNode("//h4[@class='book__title']/a").InnerText;
        var authors = article.SelectSubNodes("//div[@class='book__authors-wrap']//a[@class='book__link']")
            .Concat(article.SelectSubNodes("//div[@class='book__authors-wrap']//a[@class='authors-hide__link']"))
            .StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return title + " - " + authors;
    }

    private static async Task<string?> FanlabRu(Story topic, string q)
    {
        var html = await Html.Get($"fantlab.ru/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get,
                $"https://fantlab.ru/searchmain?searchstr={HttpUtility.UrlEncode(q)}")
            {
                Headers =
                {
                    {
                        "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0"
                    },
                    {
                        "Accept",
                        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
                    },
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
        var article = html.ParseHtml().SelectSingleNode("//div[@class='one']");
        if (article == null) return null;
        var title = article.SelectSingleNode("//div[@class='title']/a").InnerText;
        var authors = article.SelectSubNodes("//div[@class='autor']/a").StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return title + " - " + authors;
    }

    private static async Task<string?> AuthorToday(Story topic, string q)
    {
        var html = await Html.Get($"author.today/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get,
                $"https://author.today/search?category=works&q={HttpUtility.UrlEncode(q)}")
            {
                Headers =
                {
                    {
                        "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0"
                    },
                    {
                        "Cookie",
                        "cf_clearance=A5_mD_kB1YWIoFzfEeU0zIbqvj.z10Msu0RtAitJLqs-1663341274-0-150; backend_route=web08; mobile-mode=false; CSRFToken=R5hHyUTpt0v1r-AfEUQq3d8VQBMc87bWeVNyYVoXZ24kEAsFxSb_fJqUmZEmBrXkELGfvGl2VMberYKix1z-tF3oL2A1; ngSessnCookie=AAAAANqSJGN1LP2AAZMgBg=="
                    },
                }
            });


        var article = html.ParseHtml().SelectSingleNode("//div[@class='book-row']");
        if (article == null) return null;
        var title = article.SelectSingleNode("//div[@class='book-title']").InnerText.Trim();
        var authors = article.SelectSubNodes("//div[@class='book-author']/a").StrJoin(a => a.InnerText.Trim());
        return title + " - " + authors;
    }
}