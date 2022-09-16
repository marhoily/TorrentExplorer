using HtmlAgilityPack;

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

    public static DateOnly ParseDate(this HtmlNode node) =>
        DateOnly.Parse(node.InnerText.Replace("&nbsp;", ""));

    public static HtmlNode ParseHtml(this string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        return htmlDocument.DocumentNode;
    }
}