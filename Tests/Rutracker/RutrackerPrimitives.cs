using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public static class RutrackerPrimitives
{
    public static int? GetTopicId(this XNode node)
    {
        if (node is not XElement element) return null;
        var attributeValue = element.Attribute("data-ext_link_data")?.Value;
        if (attributeValue == null) return null;
        var jObject = JObject.Parse(attributeValue);
        var topicId = jObject["t"]!.Value<int>();
        return topicId;
    }
    public static bool IsHeader(this XElement node)
    {
        return (node.GetFontSize() ?? 0) > 20;
    }
}