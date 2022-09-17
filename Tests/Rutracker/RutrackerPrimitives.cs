using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public static class RutrackerPrimitives
{
    public static int GetTopicId(this HtmlNode node)
    {
        var attributeValue = node.GetAttributeValue("data-ext_link_data", null);
        var jObject = JObject.Parse(attributeValue);
        var topicId = jObject["t"]!.Value<int>();
        return topicId;
    }
    public static bool IsHeader(this HtmlNode node)
    {
        return (node.GetFontSize() ?? 0) > 20;
    }
}