﻿using System.Net;
using System.Web;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Tests.Html;
using Tests.Rutracker;
using Tests.UniversalParsing;
using Tests.Utilities;
using Http = Tests.Html.Http;

namespace Tests.BookUnification;

[UsedImplicitly]
public sealed record SearchResult(string Url, List<SearchResultItem> Items);

[UsedImplicitly]
public sealed record SearchResultItem(
    string? Url,
    string Title,
    string Author,
    string? SeriesName,
    int? NumberInSeries);

public static class SearchEngines
{
    public static readonly SearchEngine[]
        List =
        {
            new(nameof(VseAudioknigiCom), VseAudioknigiCom,"https://vse-audioknigi.com", CachingStrategy.Normal),
            new(nameof(Knigorai), Knigorai,"https://knigorai.com", CachingStrategy.Normal),
            new(nameof(AudioknigaComUa), AudioknigaComUa,"https://audiokniga.com.ua", CachingStrategy.Normal),
            new(nameof(AbooksInfo), AbooksInfo,"https://abooks.info", CachingStrategy.Normal),
            new(nameof(ReadliNet), ReadliNet,"https://readli.net", CachingStrategy.AlwaysMiss),
            new(nameof(MyBookRu), MyBookRu,"https://mybook.ru", CachingStrategy.Normal),
            new(nameof(FanlabRu), FanlabRu,"https://fantlab.ru", CachingStrategy.Normal),
            new(nameof(AuthorToday), AuthorToday,"https://author.today", CachingStrategy.Normal),
            //nameof( FlibustaSeries),FlibustaSeries,
        };

    private static readonly char[] Dashes = { '-', '–' };

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

    private static async Task<SearchResult> AudioknigaComUa(Http http, Story topic, string q)
    {
        // The site reacts with an unhandled exception 500 to this type of token
        q = q.Replace('\"', '\'');
        var localUri = $"search?text={HttpUtility.UrlEncode(q)}";
        var html = await http.AudioknigaComUa(localUri);
        var results = await Task.WhenAll(
            html.SelectSubNodes("//li[@class='b-statictop__items_item']//a")
                .Select(x => AudioknigaComUa(http, x)));
        return new SearchResult(
            new Uri(new Uri("https://audiokniga.com.ua"), localUri).ToString(),
            results.WhereNotNull().ToList());
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

    private static async Task<SearchResult> AbooksInfo(Http http, Story topic, string q)
    {
        var relativeUri = $"?s={WebUtility.UrlEncode(q)}";
        var node = await http.AbooksInfo(relativeUri);
        return new SearchResult(
            new Uri(new Uri("https://abooks.info"), relativeUri).ToString(),
            node.SelectSubNodes("//article")
                .Select(ParseAbooksInfoItem)
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

    private static async Task<SearchResult> MyBookRu(Http http, Story topic, string q)
    {
        var requestUri = $"search/?q={WebUtility.UrlEncode(q)}";
        var node = await http.MyBookRu(requestUri);
        return new SearchResult(
            new Uri(new Uri("https://mybook.ru"), requestUri).ToString(),
            node.SelectSubNodes("//div[@class='e4xwgl-0 iJwsmp']")
                .Select(MyBookRu)
                .ToList());
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
        return SplitByDash(book) is var (author, title)
            ? new SearchResultItem(null, title, author, null, null)
            : null;
    }

    private static async Task<SearchResultItem?> AudioknigaComUa(Http http, HtmlNode book)
    {
        if (SplitByDash(book) is var (author, title))
            return new SearchResultItem(null, title, author, null, null);

        var href = book.Href()!;
        var details = await http.AudioknigaComUa(href);
        var items = details.SelectSubNodes("//ul[@class='breadcrumb']/li");
        var last = items.TakeLast(2).ToList();
        if (last.Count < 2) return null;
        return new SearchResultItem(href,
            WebUtility.HtmlDecode(last[1].InnerText.Trim()),
            WebUtility.HtmlDecode(last[0].InnerText.Trim()), null, null);
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

    private static SearchResultItem? ParseAbooksInfoItem(HtmlNode article)
    {
        var node = article.SelectSubNode(
            "//h2[contains(@class,'entry-title')]/a");
        var authorAndTitle = SplitByDash(node!);
        if (authorAndTitle is not var (title, authors))
            return null;

        var series = default(string);

        return new SearchResultItem(null,
            SearchUtilities.GetTitle(title, series),
            authors, series, null);
    }

    private static SearchResultItem MyBookRu(HtmlNode article)
    {
        var title = article.SelectSubNode(
            "//p[@class='lnjchu-1 hhskLb']")!.InnerText;
        var author = article.SelectSubNode(
            "//div[@class='m4n24q-0 gFPQgy']")!.InnerText;

        var series = default(string);

        return new SearchResultItem(null, title, author, series, null);
    }

    private static async Task<SearchResultItem?> ParseReadliItem(Http http, HtmlNode article)
    {
        var bookRef = article.SelectSubNode("//h4[@class='book__title']/a")!;
        var book = await http.ReadliNet(bookRef.Href()!);
        var series = book
            .SelectSubNode("//div[@class='js-from-4']")?
            .CleanUpToXml()
            .ParseWall().OfType<JObject>()
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
        var title = article.SelectSubNode("//div[@class='title']//a")!.InnerText;
        var authors = article.SelectSubNodes("//div[@class='autor']/a").StrJoin(a => a.InnerText);
        if (string.IsNullOrWhiteSpace(authors))
            return null;
        return new SearchResultItem(null,
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null);
    }

    private static SearchResultItem ParseAuthorTodayItem(HtmlNode article)
    {
        var title = article.SelectSubNode("//div[@class='book-title']")!.InnerText.Trim();
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
        var title = seriesPage.SelectSubNode("//table/tbody/tr[td/b='Авторы:']/td[2]")!.InnerText.Trim();
        var authors = seriesPage.SelectSubNodes("//table/tbody/tr[td/b='Авторы:']//a").StrJoin(a => a.InnerText.Trim());
        var result = new SearchResultItem(null,
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(authors), null, null);
        return new SearchResult(requestUri,
            new List<SearchResultItem> { result });
    }

    private static (string x, string y)? SplitByDash(HtmlNode link)
    {
        var input = WebUtility.HtmlDecode(link.InnerText);
        var idx = input.LastIndexOfAny(Dashes);
        if (idx == -1) return null;
        return (input[(idx + 1)..].Trim(), input[..idx].Trim());
    }
}