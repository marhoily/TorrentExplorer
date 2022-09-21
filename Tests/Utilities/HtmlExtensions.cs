using HtmlAgilityPack;

namespace Tests.Utilities;

public static class HtmlExtensions
{
    public static int ParseIntAttribute(this HtmlNode node, string attributeName)
        => node.GetAttributeValue(attributeName, null).ParseInt();

    public static string? Href(this HtmlNode node, string skipPrefix = "")
    {
        return node.GetAttributeValue("href", null)?[skipPrefix.Length..];
    }

    public static HtmlNode ParseHtml(this string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        return htmlDocument.DocumentNode;
    }

    public static HtmlNode? SelectSubNode(this HtmlNode node, string xpath)
    {
        return node.SelectSubNodes(xpath).FirstOrDefault();
    }
    public static IEnumerable<HtmlNode> SelectSubNodes(this HtmlNode node, string xpath)
    {
        var xPath = node.XPath;
        var result = node.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>();
        return node.NodeType != HtmlNodeType.Document 
            ? result.Where(y => y.XPath.StartsWith(xPath)) 
            : result;
    }
}