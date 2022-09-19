using System.Text;
using System.Xml;
using HtmlAgilityPack;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Kinozal;

public sealed record KinozalForumPost(int Id, string Xml);

public static class KinozalParser
{
    public static int[] ParseKinozalFantasyHeaders(this HtmlNode node)
    {
        var rows = node.SelectNodes("//table[@class='t_peer w100p']/tr/td[2]/a").Skip(1);
        return rows
            .Select(n => n.Href(skipPrefix: "/details.php?id=")?.ParseIntOrNull())
            .OfType<int>()
            .ToArray();
    }

    private static readonly XmlWriterSettings Settings = new()
    {
        OmitXmlDeclaration = true,
        Async = true
    };

    public static KinozalForumPost GetKinozalForumPost(this HtmlNode node)
    {
        var post = node.SelectSubNode("//div[@class='mn1_content']")!;
        var id = post
            .SelectSubNode("//a")!
            .Href()!
            .Split("?id=")[1].ToInt();
    
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, Settings);
        writer.WriteStartElement("root");
        writer.WriteAttributeString("topic-id", id.ToString());
        foreach (var div in post.SelectSubNodes("div[@class='bx1 justify']"))
            div.WriteTo(writer);
        writer.WriteEndElement();
        writer.Flush();
        return new KinozalForumPost(id, sb.ToString());
    }
}