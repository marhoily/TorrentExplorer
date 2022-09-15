using HtmlAgilityPack;

namespace Tests.Utilities;

public sealed record Spoiler(string Header, HtmlNode Body);
public static class HtmlExtensions
{
    public static int ParseInt(this HtmlNode node) => node.InnerText.ParseHtmlInt();
    public static IEnumerable<HtmlNode> FindTags(this HtmlNode node, string value)
    {
        IEnumerable<HtmlNode> Rec(HtmlNode n)
        {
            if (n.Name == "div" &&
                n.GetAttributeValue("class", null) == "sp-wrap")
                yield break;
            yield return n;
            if (n.ChildNodes == null) yield break;
            foreach (var child in n.ChildNodes)
                    foreach (var grand in Rec(child))
                        yield return grand;
        }
        foreach (var d in Rec(node))
        {
            if (d.NodeType == HtmlNodeType.Text && d.ParentNode.Name == "span")
            {
                var trimEnd = d.InnerText.TrimStart('•', ' ').TrimEnd(':', ' ');
                if (trimEnd == value)
                    yield return d;
            }
        }
    }

    public static HtmlNode? FindTag(this HtmlNode node, string value)
    {
        var htmlNodes = node.FindTags(value).ToList();
        if (htmlNodes.Count == 0) return null;
        if (htmlNodes.Count > 1)
            throw new InvalidOperationException("blah");
        return htmlNodes[0];
    }
    public static HtmlNode FindTagB(this HtmlNode node, string value)
    {
        var htmlNodes = node.FindTags(value).ToList();
        if (htmlNodes.Count == 0)
            throw new InvalidOperationException("blah");
        if (htmlNodes.Count > 1)
            throw new InvalidOperationException("blah");
        return htmlNodes[0];
    }
    public static IEnumerable<Spoiler> GetSpoilers(this HtmlNode node)
    {
        var htmlNodeCollection = node.SelectNodes("//div[@class='sp-wrap']");
        if (htmlNodeCollection == null) yield break;
        foreach (var selectNode in htmlNodeCollection)
        {
            yield return new Spoiler(selectNode
                    .SelectSingleNode("div[@class='sp-head folded']")
                    .InnerText
            .Replace("&#58;", ":")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
                    .Trim(),
                selectNode.SelectSingleNode("div[@class='post-b']"));
        }
    }
    public static string TagValue(this HtmlNode node)
    {
        while (node.NextSibling == null)
            node = node.ParentNode;
        if (node.NextSibling.InnerText.Trim() == ":")
            node = node.NextSibling;

        return node.NextSibling.InnerText.TrimStart(':', ' ');
    }

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