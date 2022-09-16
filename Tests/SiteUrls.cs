using HtmlAgilityPack;
using Tests.Html;
using Tests.Utilities;

namespace Tests;

public static class SiteUrls
{
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
            $"https://rutracker.org/forum/viewforum.php?f=2387&start={page*50}");
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
}