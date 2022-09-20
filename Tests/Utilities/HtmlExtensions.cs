using HtmlAgilityPack;
using static System.StringSplitOptions;

namespace Tests.Utilities;

public static class HtmlExtensions
{
    public static int ParseInt(this HtmlNode node) => node.InnerText.ParseHtmlInt();

    public static int ParseIntAttribute(this HtmlNode node, string attributeName)
        => node.GetAttributeValue(attributeName, null).ParseInt();

    public static string? Href(this HtmlNode node, string skipPrefix = "")
    {
        return node.GetAttributeValue("href", null)?[skipPrefix.Length..];
    }

    public static Dictionary<string, string>? GetStyles(this HtmlNode node)
    {
        var style = node.GetAttributeValue("style", null);
        return style?.Split(';', RemoveEmptyEntries)
            .Select(x => x.Split(StyleKvSeparator, RemoveEmptyEntries))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0], x => x[1]);
    }

    public static string? GetStyle(this HtmlNode node, string propertyName)
    {
        var styles = node.GetStyles();
        if (styles == null) return default;
        return styles.TryGetValue(propertyName, out var propertyValue)
            ? propertyValue
            : default;
    }
    public static int? GetFontSize(this HtmlNode node)
    {
        return node
            .GetStyle("font-size")?
            .TrimPostfix("px")
            .ParseInt();
    }

    public static DateOnly ParseDate(this HtmlNode node) =>
        DateOnly.Parse(node.InnerText.Replace("&nbsp;", ""));

    public static HtmlNode ParseHtml(this string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        return htmlDocument.DocumentNode.ChildNodes
            .FirstOrDefault(n=>n.NodeType == HtmlNodeType.Element)!;
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

    private static readonly string[] StyleKvSeparator = {":", "="};

    public static HtmlNode? GoFurther(this HtmlNode n) =>
        n.NextSibling ?? (
            n.ParentNode != null 
                ? GoFurther(n.ParentNode) 
                : null);

    public static HtmlNode? GoDeeper(this HtmlNode n) =>
        n.ChildNodes.FirstOrDefault() ?? GoFurther(n);

    public static HtmlNode? SkipWhile(this HtmlNode start, Func<HtmlNode, bool> predicate)
    {
        var current = start;
        while (current != null && predicate(current)) 
            current = current.GoFurther();
        return current;
    }
}