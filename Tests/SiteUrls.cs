using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests;

public static class SiteUrls
{
    public static async Task<HtmlNode> DownloadKinozalFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get($"kinozal/fantasy/headers/{page:D3}", $"http://kinozal.tv/browse.php?c=2&page={page}");
        return html.ParseHtml();
    }
    public static async Task<HtmlNode> DownloadRussianFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get($"topics/fantasy/{page:D3}", $"https://rutracker.org/forum/viewforum.php?f=2387&start={page*50}");
        return html.ParseHtml();
    }
    public static async Task<HtmlNode> DownloadKinozalFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get($"kinozal/fantasy/topics/{topicId:D8}", $"http://kinozal.tv/details.php?id={topicId}");
        return html.ParseHtml();
    }
    public static async Task<HtmlNode> DownloadRussianFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get($"topic/fantasy/{topicId:D8}", $"https://rutracker.org/forum/viewtopic.php?t={topicId}");
        return html.ParseHtml();
    }
}