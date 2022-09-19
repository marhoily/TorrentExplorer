using HtmlAgilityPack;

namespace Tests.Rutracker;

public static class WallCollectorExt
{
    public static List<Dictionary<string, object>> ParseWall(this HtmlNode htmlNode) =>
        new WallCollector().Parse(htmlNode);
}