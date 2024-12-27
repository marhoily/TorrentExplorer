using System.Text;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step0
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-0.xml";

    [Fact]
    public async Task DownloadRawHtml()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache, Encoding.GetEncoding(1251));
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 127)
            .Select(async i =>
            {
                var page = await http.DownloadEnglishFantasyHeaders(i);
                return page.ParseRussianFantasyHeaders();
            }));

        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await http.DownloadRussianFantasyTopic(header.Id);
                return topic.GetForumPost();
            });
        await Output.SaveToXml(await Task.WhenAll(headers));
    }


}
