using FluentAssertions;
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

    public sealed class WalkConfig<TResult>
    {
        public bool Return { get; private set; }
        public bool GoDeep { get; set; }
        public TResult Result { get; private set; }

        public void SetResult(TResult result)
        {
            Return.Should().BeFalse();
            Return = true;
            Result = result;
        }
    }

    public static TResult Walk<TResult>(this HtmlNode root,
        Action<HtmlNode, WalkConfig<TResult>> apply)
    {
        var cfg = new WalkConfig<TResult>();
        bool Inner(HtmlNode x)
        {
            foreach (var child in x.ChildNodes)
            {
                apply(child, cfg);
                if (cfg.Return) return true; 
                if (!cfg.GoDeep) continue;
                if (Inner(child)) return true;
                if (cfg.Return) return true;
            }
            return cfg.Return;
        }

        Inner(root);
        return cfg.Result;
    }
}