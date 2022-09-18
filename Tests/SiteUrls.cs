using HtmlAgilityPack;
using Tests.Html;
using Tests.Utilities;

namespace Tests;

public static class SiteUrls
{
    private const string Firefox = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0";

    public static async Task<HtmlNode> DownloadKinozalFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get(
            $"kinozal/headers/{page:D3}",
            $"http://kinozal.tv/browse.php?c=2&page={page}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadRussianFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get(
            $"rutracker/headers/{page:D3}",
            $"https://rutracker.org/forum/viewforum.php?f=2387&start={page * 50}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadKinozalFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get(
            $"kinozal/topics/{topicId:D8}",
            $"http://kinozal.tv/details.php?id={topicId}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadRussianFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get(
            $"rutracker/topics/{topicId:D8}",
            $"https://rutracker.org/forum/viewtopic.php?t={topicId}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> VseAudioknigiCom(this Http http, string localUrl)
    {
        var html = await http.Get(new Uri(
            new Uri("https://vse-audioknigi.com"),
            localUrl));
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> Knigorai(this Http http, string localUrl)
    {
        var uri = new Uri(new Uri("https://knigorai.com"), localUrl);
        var html = await http.Get(uri);
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> ReadliNet(this Http http, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://readli.net"), localUrl);
        const string cookie = "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D";
        var html = await http.Get(
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "User-Agent", Firefox },
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> FanlabRu(this Http http, string key, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://fantlab.ru"), localUrl);
        const string cookie = "_ym_uid=166312055796018485; _ym_d=1663120557; _ym_isad=1";
        var html = await http.Get(key,
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> AuthorToday(this Http http, string key, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://author.today.ru"), localUrl);
        const string cookie = "cf_clearance=A5_mD_kB1YWIoFzfEeU0zIbqvj.z10Msu0RtAitJLqs-1663341274-0-150; backend_route=web08; mobile-mode=false; CSRFToken=R5hHyUTpt0v1r-AfEUQq3d8VQBMc87bWeVNyYVoXZ24kEAsFxSb_fJqUmZEmBrXkELGfvGl2VMberYKix1z-tF3oL2A1; ngSessnCookie=AAAAANqSJGN1LP2AAZMgBg==";
        var html = await http.Get(key,
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
     
    }
}