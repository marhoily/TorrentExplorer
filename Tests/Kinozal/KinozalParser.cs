using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Kinozal;

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

    public static HtmlNode GetKinozalForumPost(this HtmlNode node)
    {
        var post = node.SelectSubNode("//div[@class='mn1_content']")!;
        var doc = new HtmlDocument();
        var root = doc.CreateElement("root");
        root.SetAttributeValue("topic-id", post
            .SelectSubNode("//a")!
            .Href()!
            .Split("?id=")[1]);
        foreach (var div in post.SelectSubNodes("div[@class='bx1 justify']")) 
            root.AppendChild(div.Clone());
        doc.DocumentNode.AppendChild(root);
        return root;
    }
}