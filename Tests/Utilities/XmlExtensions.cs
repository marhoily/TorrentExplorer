using System.Xml.Linq;
using static System.StringSplitOptions;

namespace Tests.Utilities;

public static class XmlExtensions
{
    public static string? Href(this XElement node, string skipPrefix = "")
    {
        return node.Attribute("href")?.Value[skipPrefix.Length..];
    }
    private static readonly string[] StyleKvSeparator = {":", "="};

    public static Dictionary<string, string>? GetStyles(this XElement node)
    {
        var style = node.Attribute("style")?.Value;
        return style?.Split(';', RemoveEmptyEntries)
            .Select(x => x.Split(StyleKvSeparator, RemoveEmptyEntries))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0], x => x[1]);
    }

    public static string? GetStyle(this XElement node, string propertyName)
    {
        var styles = node.GetStyles();
        if (styles == null) return default;
        return styles.TryGetValue(propertyName, out var propertyValue)
            ? propertyValue
            : default;
    }

    public static string InnerText(this XNode node) =>
        node switch
        {
            XText t => t.Value,
            XElement e => e.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(node), node, null)
        };

    public static int? GetFontSize(this XElement node)
    {
        return node
            .GetStyle("font-size")?
            .TrimPostfix("px")
            .ParseInt();
    }

    public static bool HasClass(this XElement node, string needle)
    {
        // BUG: .Contains is just a stub!
        return node.Attribute("class")?.Value.Contains(needle) == true;
    }

    public static XNode? GoFurther(this XNode n) =>
        n.NextNode ?? (n.Parent != null ? GoFurther(n.Parent) : null);

    public static XNode? GoDeeper(this XNode n) =>
        (n is XElement e ? e.FirstNode : null) ?? GoFurther(n);

    public static XNode? SkipWhile(this XNode start, Func<XNode, bool> predicate)
    {
        var current = start;
        while (current != null && predicate(current)) 
            current = current.GoFurther();
        return current;
    }
}