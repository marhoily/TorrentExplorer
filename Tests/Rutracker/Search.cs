using System.Net;
using System.Text;
using System.Web;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Tests.Html;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Rutracker;

public sealed record SearchResult(
    string Title,
    string Author,
    string? SeriesName,
    int? NumberInSeries,
    string Source);

public class Search
{
    private static readonly Http Html = new(
        new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal),
        Encoding.Default);

    private static readonly Func<Story, string, Task<SearchResult?>>[]
        SearchEngines =
        {
            VseAudioknigiCom,
            Knigorai,
            ReadliNet,
            FanlabRu,
            AuthorToday,
            //FlibustaSeries,
        };

    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Search\output.json";

    [Fact]
    public async Task Do()
    {
        var topics = await CherryPickParsing.Output.ReadJson<List<Story>>();
        var circuitBreaker = new CircuitBreaker();
        foreach (var topic in topics!)
            if (topic.Title != null)
                await GoThroughSearchEngines(circuitBreaker, topic);
    }

    private string FileName(int id, Outcome outcome) =>
        $@"C:\temp\TorrentsExplorerData\Extract\Search-{outcome}\{id:D8}.json";

    private enum Outcome
    {
        Positive,
        Negative
    }

    private async Task Save<T>(int id, T obj, Outcome outcome)
    {
        var opposite = outcome == Outcome.Positive
            ? Outcome.Negative
            : Outcome.Positive;
        var oppositeName = FileName(id, opposite);
        if (File.Exists(oppositeName))
            File.Delete(oppositeName);
        await FileName(id, outcome).SaveJson(obj);
    }

    [UsedImplicitly]
    private async Task<string?> GetOldQ(int id)
    {
        var positive = FileName(id, Outcome.Positive);
        var negative = FileName(id, Outcome.Negative);
        var jObj = File.Exists(positive)
            ? await positive.ReadJson<JObject>()
            : File.Exists(negative)
                ? await negative.ReadJson<JObject>()
                : null;
        return jObj?["q"]?.Value<string>();
    }

    [UsedImplicitly]
    private Outcome? GetOutcome(int id)
    {
        return File.Exists(FileName(id, Outcome.Positive))
            ? Outcome.Positive
            : File.Exists(FileName(id, Outcome.Negative))
                ? Outcome.Negative
                : default;
    }

    private async Task GoThroughSearchEngines(CircuitBreaker circuitBreaker, Story topic)
    {
        if (GetOutcome(topic.TopicId) == Outcome.Positive) return;
        //if (await GetOldQ(topic.TopicId) is not { } oldQ) return;

         var q = GetQuery(topic);
        //if (topic.TopicId != 588161)
        //     if (q == oldQ)
        //         return;
        var negativeSearchResults = new List<SearchResult>();

        foreach (var searchEngine in SearchEngines)
        {
            var finished = await circuitBreaker.X(async () =>
            {
                var result = await searchEngine(topic, q);
                if (result == null) return false;

                if (ValidateSearchResult(result, topic))
                {
                    await Save(topic.TopicId, new { topic, q, result }, Outcome.Positive);
                    return true;
                }

                negativeSearchResults.Add(result);
                return false;
            });
            if (finished) return;
        }

        await Save(topic.TopicId, new { topic, q, negativeSearchResults }, Outcome.Negative);
    }

    private static string GetQuery(Story topic)
    {
        var title = topic.Title;
        if (title!.StartsWith("Книга "))
        {
            var n = title["Книга ".Length..].Trim().TryParseIntOrWord();
            if (n != null && n == topic.NumberInSeries?.ParseInt())
            {
                //title = "Том " + n + ". " + title;
                title = topic.Series + ". " + title;
            }
        }

        return title + " - " + topic.Author;
    }

    private static bool ValidateSearchResult(SearchResult result, Story topic)
    {
        if (topic.Author != null)
        {
            if (!CompareAuthors(Sanitize(result.Author), Sanitize(topic.Author)))
            {
                Console.WriteLine(result.Author + " != " + topic.Author);
                return false;
            }
        }

        string Scrape(string s) => s
            .Replace('ё', 'e')
            .Replace('й', 'и')
            .Replace('е', 'e')
            .Replace('c', 'с') //русский..topic
            .Replace('а', 'a') //русский..topic
            .Replace('о', 'o') //русский..topic
            .Replace('х', 'x') //русский..topic
            .Replace("«", "")
            .Replace("»", "")
            .Replace("\"", "")
            .Replace(",", ", ")
            .Replace("  ", " ")
            .Split(' ', '.', '-')
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.ToLower())
            .StrJoin(" ");

        string Sanitize(string s) => s
            .ToLower()
            .Replace("&", ",")
            .Replace("_", " ")
            .Replace('ё', 'e')
            .Replace('е', 'e')
            .Replace('c', 'с') //русский..topic
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace("[", " ")
            .Replace("]", " ");

        var resultTitle = Scrape(result.Title);
        var topicTitle = Scrape(topic.Title!);
        if (resultTitle.Contains(topicTitle) ||
            topicTitle.Contains(resultTitle))
            return true;

        Console.WriteLine(result.Title + " != " + topic.Title);
        return false;
    }

    private static bool CompareAuthors(string formal, string manual)
    {
        if (formal.Contains(',') || manual.Contains(','))
        {
            foreach (var f in formal.Split(',', RemoveEmptyEntries))
            foreach (var m in manual.Split(',', RemoveEmptyEntries))
                if (CompareAuthors(f, m))
                    return true;
        }

        var ff = formal.Split(' ', RemoveEmptyEntries).ToHashSet();
        var mm = manual
            // for the case like "Змагаевы Алекс и Ангелина"
            .Replace(" и ", " ")
            .Split(new[] { ' ', '.' }, RemoveEmptyEntries)
            .ToList();

        bool Eq(string x, string y)
        {
            if (x == y) return true;
            if (y.Length < x.Length) return Eq(y, x);
            // Змагаевы -> Змагаев     
            if (x == y.TrimEnd('ы', 'и')) return true;
            // Стругацкий -> Стругацкие     
            if (x.Length > 3 && x[..^1] == y[..^1]) return true;
            if (x.Length == 1 && x[0] == y[0]) return true;
            return false;
        }

        bool IsSubset(IEnumerable<string> a, ICollection<string> b) =>
            a.All(word => b.Any(x => Eq(x, word)));

        if (IsSubset(ff, mm) || IsSubset(mm, ff))
            return true;
        return false;
    }

    private static async Task<SearchResult?> VseAudioknigiCom(Story topic, string q)
    {
        var localUri = $"https://vse-audioknigi.com/search?text={HttpUtility.UrlEncode(q)}";
        var html = await Html.Get($"vse-audioknigi.com/{topic.TopicId:D8}", localUri);
        var r = WebUtility.HtmlDecode(html.ParseHtml()
            .SelectSingleNode("//li[@class='b-statictop__items_item']//a")?
            .InnerText);
        if (r == null) return null;
        var idx = r.LastIndexOf("-", StringComparison.InvariantCulture);
        if (idx == -1) return null;
        var vseAudioknigiCom = new SearchResult(
            r[..idx].Trim(), r[(idx + 1)..].Trim(), null, null, localUri);
        return vseAudioknigiCom;
    }

    private static async Task<SearchResult?> ReadliNet(Story topic, string q)
    {
        var requestUri = $"https://readli.net/srch/?q={HttpUtility.UrlEncode(q)}";
        var html = await Html.Get($"readli.net/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    {
                        "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0"
                    },
                    {
                        "Cookie",
                        "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D"
                    },
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
        return new SearchResult(
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null, requestUri);
    }

    private static async Task<SearchResult?> Knigorai(Story topic, string q)
    {
        var requestUri = $"https://knigorai.com/?q={HttpUtility.UrlEncode(q)}";
        var html = await Html.Get($"knigorai.com/{topic.TopicId:D8}", requestUri);
        var article = html.ParseHtml()
            .SelectSingleNode("//div[@class='book-item panel panel-default']");
        if (article == null) return null;
        var title = article.SelectSingleNode("//a[@class='book-title']").InnerText;
        var authors = article.SelectSubNodes("//div[@class='col-lg-12 book-author']//a")
            .StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return new SearchResult(
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null, requestUri);
    }

    private static async Task<SearchResult?> FanlabRu(Story topic, string q)
    {
        var requestUri = $"https://fantlab.ru/searchmain?searchstr={HttpUtility.UrlEncode(q)}";
        var html = await Html.Get($"fantlab.ru/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get, requestUri)
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
        return new SearchResult(
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null, requestUri);
    }

    private static async Task<SearchResult?> AuthorToday(Story topic, string q)
    {
        var requestUri = $"https://author.today/search?category=works&q={HttpUtility.UrlEncode(q)}";
        var html = await Html.Get($"author.today/{topic.TopicId:D8}",
            new HttpRequestMessage(HttpMethod.Get, requestUri)
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
        return new SearchResult(
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null, requestUri);
    }

    [UsedImplicitly]
    private static async Task<SearchResult?> FlibustaSeries(Story topic, string q)
    {
        var requestUri = $"https://flibusta.site/booksearch?chs=on&ask={HttpUtility.UrlEncode(topic.Series)}";
        var searchHtml = await Html.Get($"flibusta.site/{topic.TopicId:D8}", requestUri);


        var seriesHref = searchHtml.ParseHtml()
            .SelectNodes("//li/a")
            .Select(a => a.Href())
            .OfType<string>()
            .FirstOrDefault(href => href.StartsWith("/sequence/"));
        if (seriesHref == null) return null;
        var seriesHtml = await Html.Get(
            $"flibusta.site{seriesHref}",
            $"https://flibusta.site{seriesHref}");
        var seriesPage = seriesHtml.ParseHtml();
        var title = seriesPage.SelectSingleNode("//table/tbody/tr[td/b='Авторы:']/td[2]").InnerText.Trim();
        var authors = seriesPage.SelectSubNodes("//table/tbody/tr[td/b='Авторы:']//a").StrJoin(a => a.InnerText.Trim());
        return new SearchResult(
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null, requestUri);
    }
}