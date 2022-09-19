using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Tests.Rutracker;

public static class WallCollectorExt
{
    public static JArray ParseWall(this HtmlNode htmlNode) =>
        new WallCollector(htmlNode).Parse();
}