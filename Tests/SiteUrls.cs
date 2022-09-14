using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests;

public static class SiteUrls
{
    public static async Task<HtmlNode> DownloadRussianFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get($"fantasy/{page:D3}", $"forum/viewforum.php?f=2387&start={page*50}");
        return html.ParseHtml();
    }
    public static async Task<HtmlNode> DownloadRussianFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get($"fantasy/{topicId:D8}", $"forum/viewtopic.php?t={topicId}");
        return html.ParseHtml();
    }
}