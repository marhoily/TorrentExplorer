using System.Text;
using System.Xml;
using HtmlAgilityPack;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step0
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\step0.xml";

    [Fact]
    public async Task DownloadRawHtml()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache, Encoding.GetEncoding(1251));
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
        await SaveToXml(await Task.WhenAll(headers));
    }

    private static async Task SaveToXml(HtmlNode[] htmlNodes)
    {
        await using var fileStream = File.OpenWrite(Output);
        await using var writer = XmlWriter.Create(fileStream,
            new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Async = true
            });
        writer.WriteStartElement("many");
        foreach (var htmlNode in htmlNodes)
            htmlNode.CleanUpAndWriteTo(writer);
        await writer.WriteEndElementAsync();
    }
}
