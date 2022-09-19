using System.Text;
using System.Xml;
using System.Xml.Linq;
using Tests.Html;

namespace Tests.Kinozal;

public class Step0
{
    public const string Output= @"C:\temp\TorrentsExplorerData\Extract\kinozal\step0.xml";

    [Fact]
    public async Task DownloadRawHtml()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache, Encoding.GetEncoding(1251));
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await http.DownloadKinozalFantasyHeaders(i);
                return page.ParseKinozalFantasyHeaders();
            }));
     

        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await http.DownloadKinozalFantasyTopic(header);
                return topic.GetKinozalForumPost();
            });
        var htmlNodes = await Task.WhenAll(headers);
        SavePrettyXml(Output, htmlNodes.Select(x => x.Xml).ToArray());
    }

    private static void SavePrettyXml(string file, string[] xmlList)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true
        };

        using var xmlWriter = XmlWriter.Create(File.OpenWrite(file), settings);
        xmlWriter.WriteStartElement("blah");
        foreach (var xml in xmlList)
        {
            var element = XElement.Parse(xml);
            element.WriteTo(xmlWriter);
        }

        xmlWriter.WriteEndElement();
    }
}