using System.Xml.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;
using RegExtract;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Kinozal;

public sealed record KinozalForumPost(int Id, int? SeriesId, XElement Xml)
{
    [UsedImplicitly]
    [Obsolete("For deserialization only", true)]
    public KinozalForumPost() : this(0, null!, null!)
    {
    }
}

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
    
    public static KinozalForumPost GetKinozalForumPost(this HtmlNode node)
    {
        var post = node.SelectSubNode("//div[@class='mn1_content']")!;
        var id = post
            .SelectSubNode("//a")!
            .Href()!
            .Split("?id=")[1].ToInt();

        var seriesId = post
            .SelectSubNodes("div[@class='bx1'][1]//ul[@class='lis']/li/a")
            .FirstOrDefault(a => a.InnerText.Trim() == "Цикл")?
            .GetAttributeValue("onclick", null)
            .Extract<int>($"showtab\\({id},(\\d+)\\); return false;");

        var sections = post
            .SelectSubNodes("div[@class='bx1 justify']")
            .Select(div => div.CleanUpToXml());
        var root = new XElement("root", new XAttribute("topic-id", id));
        foreach (var section in sections) root.Add(section);
        return new KinozalForumPost(id, seriesId, root);
    }
}