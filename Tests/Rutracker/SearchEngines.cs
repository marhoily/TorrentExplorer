using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record SearchResult(string Url, List<SearchResultItem> Items);

public sealed record SearchResultItem(
    string? Url,
    string Title,
    string Author,
    string? SeriesName,
    int? NumberInSeries);

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
    public static readonly Func<Http, Story, string, Task<SearchResult>>[]
        List =
        {
            VseAudioknigiCom,
            Knigorai,
            ReadliNet,
            FanlabRu,
            AuthorToday,
            //FlibustaSeries,
        };

    private static async Task<SearchResult> VseAudioknigiCom(Http http, Story topic, string q)
    {
        var localUri = $"search?text={HttpUtility.UrlEncode(q)}";
        var html = await http.VseAudioknigiCom(localUri);
        return new SearchResult(
            new Uri(new Uri("https://vse-audioknigi.com"), localUri).ToString(),
            html.SelectSubNodes("//li[@class='b-statictop__items_item']//a")
                .Select(ParseVseAudioknigiItem)
                .WhereNotNull()
                .ToList());
    }

    private static async Task<SearchResult> Knigorai(Http http, Story topic, string q)
    {
        var relativeUri = $"?q={WebUtility.UrlEncode(q)}";
        var node = await http.Knigorai(relativeUri);
        return new SearchResult(
            new Uri(new Uri("https://knigorai.com"), relativeUri).ToString(),
            node.SelectSubNodes("//div[@class='book-item panel panel-default']")
                .Select(ParseKnigoraiItem)
                .WhereNotNull()
                .ToList());
    }

    private static async Task<SearchResult> ReadliNet(Http http, Story topic, string q)
    {
        var requestUri = $"srch/?q={WebUtility.UrlEncode(q)}";
        var node = await http.ReadliNet(requestUri);
        var items = await Task.WhenAll(
            node.SelectSubNodes("//div[@id='books']/article")
            .Select(x => ParseReadliItem(http, x)));
        return new SearchResult(
            new Uri(new Uri("https://readli.net"), requestUri).ToString(),
            items.WhereNotNull().ToList());
    }

    private static async Task<SearchResult> FanlabRu(Http http, Story topic, string q)
    {
        var requestUri = $"searchmain?searchstr={HttpUtility.UrlEncode(q)}";
        var html = await http.FanlabRu($"fantlab.ru/{q}", requestUri);

        return new SearchResult(
            new Uri(new Uri("https://fantlab.ru"), requestUri).ToString(),
            html.SelectSubNodes("//div[@class='one']")
                .Select(ParseFanlabItem)
                .WhereNotNull()
                .ToList());
    }

    private static async Task<SearchResult> AuthorToday(Http http, Story topic, string q)
    {
        var requestUri = $"search?category=works&q={HttpUtility.UrlEncode(q)}";
        var html = await http.AuthorToday($"author.today/{q}", requestUri);
        return new SearchResult(
            new Uri(new Uri("https://author.today"), requestUri).ToString(),
            html.SelectSubNodes("//div[@class='book-row']")
                .Select(ParseAuthorTodayItem)
                .ToList());
    }

    private static SearchResultItem? ParseVseAudioknigiItem(HtmlNode book)
    {
        return ParseAuthorAndTitle(book) is var (author, title)
            ? new SearchResultItem(null, title, author, null, null)
            : null;

        static (string author, string title)? ParseAuthorAndTitle(HtmlNode link)
        {
            var authorAndTitle = WebUtility.HtmlDecode(link.InnerText);
            var idx = authorAndTitle.LastIndexOf("-", StringComparison.InvariantCulture);
            if (idx == -1) return null;
            var title = authorAndTitle[..idx].Trim();
            var author = authorAndTitle[(idx + 1)..].Trim();
            return (author, title);
        }
    }

    private static SearchResultItem? ParseKnigoraiItem(HtmlNode article)
    {
        var title = WebUtility.HtmlDecode(
            article.SelectSubNode("//a[@class='book-title']")!.InnerText);
        var series = WebUtility.HtmlDecode(
            article.SelectSubNode("//div[@class='row book-series']//a")?.InnerText);
        var authors = WebUtility.HtmlDecode(
            article.SelectSubNodes("//div[@class='col-lg-12 book-author']//a")
                .StrJoin(a => a.InnerText));
        if (string.IsNullOrWhiteSpace(authors))
            return null;

        return new SearchResultItem(null, 
            SearchUtilities.GetTitle(title, series),
            authors, series, null);
    }

    private static async Task<SearchResultItem?> ParseReadliItem(Http http, HtmlNode article)
    {
        var bookRef = article.SelectSubNode("//h4[@class='book__title']/a")!;
        var book = await http.ReadliNet(bookRef.Href()!);
        var series = book
            .SelectSubNode("//div[@class='js-from-4']")?
            .ParseWall()
            .Single()
            .FindTags("Серия");
       
        var title = bookRef.InnerText.Trim();
        var authors = article.SelectSubNodes("//div[@class='book__authors-wrap']//a[@class='book__link']")
            .Concat(article.SelectSubNodes("//div[@class='book__authors-wrap']//a[@class='authors-hide__link']"))
            .StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;

        return new SearchResultItem(null, 
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), series, null);
    }

    private static SearchResultItem? ParseFanlabItem(HtmlNode article)
    {
        var title = article.SelectSingleNode("//div[@class='title']/a").InnerText;
        var authors = article.SelectSubNodes("//div[@class='autor']/a").StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return new SearchResultItem(null, 
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null);
    }

    private static SearchResultItem ParseAuthorTodayItem(HtmlNode article)
    {
        var title = article.SelectSingleNode("//div[@class='book-title']").InnerText.Trim();
        var authors = article.SelectSubNodes("//div[@class='book-author']/a").StrJoin(a => a.InnerText.Trim());
        return new SearchResultItem(null, 
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null);
    }

    [UsedImplicitly]
    private static async Task<SearchResult> FlibustaSeries(Http http, Story topic, string q)
    {
        var requestUri = $"https://flibusta.site/booksearch?chs=on&ask={HttpUtility.UrlEncode(topic.Series)}";
        var searchHtml = await http.Get($"flibusta.site/{q}", requestUri);

        var seriesHref = searchHtml.ParseHtml()
            .SelectNodes("//li/a")
            .Select(a => a.Href())
            .OfType<string>()
            .FirstOrDefault(href => href.StartsWith("/sequence/"));
        if (seriesHref == null)
            return new SearchResult(requestUri, new List<SearchResultItem>());

        var seriesHtml = await http.Get(
            $"flibusta.site{seriesHref}",
            $"https://flibusta.site{seriesHref}");
        var seriesPage = seriesHtml.ParseHtml();
        var title = seriesPage.SelectSingleNode("//table/tbody/tr[td/b='Авторы:']/td[2]").InnerText.Trim();
        var authors = seriesPage.SelectSubNodes("//table/tbody/tr[td/b='Авторы:']//a").StrJoin(a => a.InnerText.Trim());
        var result = new SearchResultItem(null, 
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null);
        return new SearchResult(requestUri,
            new List<SearchResultItem> { result });
    }
}