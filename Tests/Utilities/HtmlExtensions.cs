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
        return htmlDocument.DocumentNode;
    }

    public static HtmlNode? SelectSubNode(this HtmlNode node, string xpath)
    {
        var parentXPath = node.XPath;
        return (node.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
            .FirstOrDefault(y => y.XPath.StartsWith(parentXPath));
    }
    public static IEnumerable<HtmlNode> SelectSubNodes(this HtmlNode node, string xpath)
    {
        var parentXPath = node.XPath;
        return (node.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
            .Where(y => y.XPath.StartsWith(parentXPath));
    }

    private static readonly ApplyResultMarker Stub = new();
    public sealed class WalkConfig<TResult>
    {
        public bool Stop { get; private set; }
        public bool GoDeep { get; private set; }
        public List<TResult> Collector { get; } = new();

        public ApplyResultMarker Yield(TResult item, WalkInstruction cmd)
        {
             Collector.Add(item);
             GoDeep = cmd == WalkInstruction.GoDeep;
             return Stub;
        }

        public ApplyResultMarker YieldBreak(WalkInstruction cmd)
        {
            Stop = true;
            GoDeep = cmd == WalkInstruction.GoDeep;
            return Stub;
        }
        public ApplyResultMarker Continue(WalkInstruction cmd)
        {
            Stop = false;
            GoDeep = cmd == WalkInstruction.GoDeep;
            return Stub;
        }
    }

    public enum WalkInstruction{GoBy, GoDeep}

    public sealed class ApplyResultMarker
    {
    }

    public static List<TResult> Walk<TResult>(this HtmlNode root,
        Func<HtmlNode, WalkConfig<TResult>, ApplyResultMarker> apply)
    {
        var cfg = new WalkConfig<TResult>();
        bool Inner(HtmlNode x)
        {
            foreach (var child in x.ChildNodes)
            {
                apply(child, cfg);
                if (cfg.Stop) return true; 
                if (!cfg.GoDeep) continue;
                if (Inner(child)) return true;
                if (cfg.Stop) return true;
            }
            return cfg.Stop;
        }

        Inner(root);
        return cfg.Collector;
    }
    
    public static HtmlNode? GoFurther(this HtmlNode n) =>
        n.NextSibling ?? (
            n.ParentNode != null 
                ? GoFurther(n.ParentNode) 
                : null);

    public static HtmlNode? GoDeeper(this HtmlNode n) =>
        n.ChildNodes.FirstOrDefault(
            x => x.NodeType == HtmlNodeType.Element) 
        ?? GoFurther(n);        
}