using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step0
{
    [Fact]
    public async Task DownloadRawHtml()
    {
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache);
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await http.DownloadRussianFantasyHeaders(i);
                return page.ParseRussianFantasyHeaders();
            }));

        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await http.DownloadRussianFantasyTopic(header.Id);
                return topic.GetForumPost();
            });
        var htmlNodes = await Task.WhenAll(headers);
        await @"c:\temp\bulk.json".SaveJson(htmlNodes.Select(x => x.OuterHtml));
    }
}
