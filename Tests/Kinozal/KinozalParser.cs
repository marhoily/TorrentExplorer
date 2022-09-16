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
        return node.SelectSingleNode("//div[@class='bx1 justify']");
    }
}