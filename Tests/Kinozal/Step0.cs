using System.Text;
using System.Xml;
using System.Xml.Linq;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Kinozal;

public sealed record KinozalBook(KinozalForumPost Post, string Series);

public class Step0
{
    private readonly Http _http;
    private readonly Http _httpUtf8;
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\kinozal\step0.xml";

    static Step0()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Step0()
    {
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        _http = new Http(htmlCache, Encoding.GetEncoding(1251));
        _httpUtf8 = new Http(new SqliteCache(CachingStrategy.Normal), Encoding.Default);
    }

    [Fact]
    public async Task DownloadRawHtml()
    {
        var htmlNodes = await GetKinozalForumPosts();
        await Output.SaveJson(htmlNodes);
    }

    private async Task<KinozalBook[]> GetKinozalForumPosts()
    {
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await _http.DownloadKinozalFantasyHeaders(i);
                return page.ParseKinozalFantasyHeaders();
            }));


        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await _http.DownloadKinozalFantasyTopic(header);
                var post = topic.GetKinozalForumPost();
                var html = await _httpUtf8.Get($"http://kinozal.tv/get_srv_details.php?id={post.Id}&pagesd=0");
                return new KinozalBook(post, html);
            });

        return await Task.WhenAll(headers);
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

