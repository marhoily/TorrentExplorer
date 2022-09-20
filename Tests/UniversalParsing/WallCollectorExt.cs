using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Tests.UniversalParsing;

public static class WallCollectorExt
{
    public static JArray ParseWall(this XNode htmlNode) =>
        new WallCollector(htmlNode).Parse();
}