using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record SearchResult(
    string Title,
    string Author,
    string? SeriesName,
    int? NumberInSeries,
    string Source);

public static class SearchUtilities
{
    public static string GetTitle(string title, string? series)
    {
        if (series == null)
            return title;

        return Regex.Replace(title,
            series + "\\s*(:?|-)\\s*\\d+(\\.|:)\\s*", "",
            RegexOptions.IgnoreCase);
    }

}
public static class SearchEngines
{
    public static readonly Func<Http, Story, string, Task<SearchResult?>>[]
        List =
        {
            VseAudioknigiCom,
            Knigorai,
            ReadliNet,
            FanlabRu,
            AuthorToday,
            //FlibustaSeries,
        };
    private static async Task<SearchResult?> VseAudioknigiCom(Http http, Story topic, string q)
    {
        var localUri = $"https://vse-audioknigi.com/search?text={HttpUtility.UrlEncode(q)}";
        var searchHtml = await http.Get($"vse-audioknigi.com/{q}", localUri);
        var bookA = searchHtml.ParseHtml()
            .SelectSingleNode("//li[@class='b-statictop__items_item']//a");
        if (ParseAuthorAndTitle(bookA) is not var (author, title)) return null;

       // var bookHtml = await http.Get($"https://vse-audioknigi.com{bookA.Href()}");
       // var breadcrumb = bookHtml.ParseHtml().SelectSingleNode("//ui[@class='breadcrumb']");

        var result = new SearchResult(
            title, author, null, null, localUri);
        return result;
    }

    private static (string author, string title)? ParseAuthorAndTitle(HtmlNode? link)
    {
        var authorAndTitle = WebUtility.HtmlDecode(link?.InnerText);
        if (authorAndTitle == null) return null;
        var idx = authorAndTitle.LastIndexOf("-", StringComparison.InvariantCulture);
        if (idx == -1) return null;
        var title = authorAndTitle[..idx].Trim();
        var author = authorAndTitle[(idx + 1)..].Trim();
        return(author, title);
    }

    private static async Task<SearchResult?> Knigorai(Http http, Story topic, string q)
    {
        var requestUri = $"https://knigorai.com/?q={HttpUtility.UrlEncode(q)}";
        var html = await http.Get($"knigorai.com/{q}", requestUri);
        var article = html.ParseHtml()
            .SelectSingleNode("//div[@class='book-item panel panel-default']");
        if (article == null) return null;
        var title = WebUtility.HtmlDecode(
            article.SelectSingleNode("//a[@class='book-title']").InnerText);
        var series = WebUtility.HtmlDecode(
            article.SelectSubNode("//div[@class='row book-series']//a")?.InnerText);
        var authors = WebUtility.HtmlDecode(
            article.SelectSubNodes("//div[@class='col-lg-12 book-author']//a")
                .StrJoin(a => a.InnerText));
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return new SearchResult(
            SearchUtilities.GetTitle(title, series),
            authors, series, null, requestUri);
    }


    private static async Task<SearchResult?> ReadliNet(Http http, Story topic, string q)
    {
        var requestUri = $"https://readli.net/srch/?q={HttpUtility.UrlEncode(q)}";
        var html = await http.Get($"readli.net/{q}",
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

    private static async Task<SearchResult?> FanlabRu(Http http, Story topic, string q)
    {
        var requestUri = $"https://fantlab.ru/searchmain?searchstr={HttpUtility.UrlEncode(q)}";
        var html = await http.Get($"fantlab.ru/{q}",
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    {
                        "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0"
                    },
                    {
                        "Accept",
                        "text/http,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8"
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

    private static async Task<SearchResult?> AuthorToday(Http http, Story topic, string q)
    {
        var requestUri = $"https://author.today/search?category=works&q={HttpUtility.UrlEncode(q)}";
        var html = await http.Get($"author.today/{q}",
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
    private static async Task<SearchResult?> FlibustaSeries(Http http, Story topic, string q)
    {
        var requestUri = $"https://flibusta.site/booksearch?chs=on&ask={HttpUtility.UrlEncode(topic.Series)}";
        var searchHtml = await http.Get($"flibusta.site/{q}", requestUri);


        var seriesHref = searchHtml.ParseHtml()
            .SelectNodes("//li/a")
            .Select(a => a.Href())
            .OfType<string>()
            .FirstOrDefault(href => href.StartsWith("/sequence/"));
        if (seriesHref == null) return null;
        var seriesHtml = await http.Get(
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