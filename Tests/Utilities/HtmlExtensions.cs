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
            .ToDictionary(x => x.Split(": ")[0], x => x.Split(": ")[1]);
    }

    public static string? GetStyle(this HtmlNode node, string propertyName)
    {
        var styles = node.GetStyles();
        if (styles == null) return default;
        return styles.TryGetValue(propertyName, out var propertyValue)
            ? propertyValue
            : default;
    }

    public static DateOnly ParseDate(this HtmlNode node) =>
        DateOnly.Parse(node.InnerText.Replace("&nbsp;", ""));

    public static HtmlNode ParseHtml(this string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        return htmlDocument.DocumentNode;
    }

    public static IEnumerable<HtmlNode> SelectSubNodes(this HtmlNode node, string xpath)
    {
        var parentXPath = node.XPath;
        return (node.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
            .Where(y => y.XPath.StartsWith(parentXPath));
    }
}