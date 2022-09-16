﻿using System.Text;
using System.Xml;
using System.Xml.Linq;
using Tests.Html;

namespace Tests.Kinozal;

public class KinozalRawHtml
{
    [Fact]
    public async Task Download()
    {
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache);
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await http.DownloadKinozalFantasyHeaders(i);
                return page.ParseKinozalFantasyHeaders();
            }));
        var settings = new XmlWriterSettings()
        {
            OmitXmlDeclaration = true,
            Async = true
        };

        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await http.DownloadKinozalFantasyTopic(header);
                var kinozalForumPost = topic.GetKinozalForumPost();
                var sb = new StringBuilder();
                await using var writer = XmlWriter.Create(sb, settings);
                kinozalForumPost.WriteTo(writer);
                await writer.FlushAsync();
                return sb.ToString();
            });
        var htmlNodes = await Task.WhenAll(headers);
        PrettyXml(@"c:\temp\kinozal-bulk.xml", htmlNodes);
    }

    private static void PrettyXml(string file, string[] xmlList)
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