using System.Net;
using System.Text;
using System.Xml;
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

    public static void WriteAttributes(this HtmlNode node, XmlWriter writer)
    {
        if (!node.HasAttributes) return;
        foreach (var htmlAttribute in node.Attributes)
            writer.WriteAttributeString(
                GetXmlName(htmlAttribute.Name, true, true),
                WebUtility.HtmlDecode(htmlAttribute.Value));
    }

    public static string GetXmlName(string name, bool isAttribute, bool preserveXmlNamespaces)
    {
        string empty = string.Empty;
        bool flag = true;
        for (int index = 0; index < name.Length; ++index)
        {
            if (name[index] >= 'a' && name[index] <= 'z' || 
                name[index] >= 'A' && name[index] <= 'Z' ||
                name[index] >= '0' && name[index] <= '9' ||
                isAttribute | preserveXmlNamespaces && name[index] == ':' || 
                name[index] == '_' || 
                name[index] == '-' ||
                name[index] == '.')
            {
                empty += name[index].ToString();
            }
            else
            {
                flag = false;
                var utF8 = Encoding.UTF8;
                char[] chars = { name[index] };
                foreach (var num in utF8.GetBytes(chars))
                    empty += num.ToString("x2");
                empty += "_";
            }
        }
        return flag ? empty : "_" + empty;
    }

    public static string GetXmlComment(this HtmlCommentNode comment)
    {
        string comment1 = comment.Comment;
        return comment1.Substring(4, comment1.Length - 7).Replace("--", " - -");
    }

    public static void CleanUpAndWriteTo(this HtmlNode node, XmlWriter writer)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Element:
                writer.WriteStartElement(node.OriginalName);
                node.WriteAttributes(writer);
                if (node.HasChildNodes)
                    foreach (var childNode in node.ChildNodes)
                        childNode.WriteTo(writer);
                writer.WriteEndElement();
                break;
            case HtmlNodeType.Comment:
                writer.WriteComment(((HtmlCommentNode) node).GetXmlComment());
                break;
            case HtmlNodeType.Text:
                writer.WriteString(((HtmlTextNode) node).Text);
                break;
            default:
                throw new ArgumentOutOfRangeException(node.NodeType.ToString());
        }
    }
    private static readonly ApplyResultMarker Stub = new();
    private static readonly string[] StyleKvSeparator = {":", "="};

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
        n.ChildNodes.FirstOrDefault() ?? GoFurther(n);


    public static HtmlNode? SkipWhile(this HtmlNode start, Func<HtmlNode, bool> predicate)
    {
        var current = start;
        while (current != null && predicate(current)) 
            current = current.GoFurther();
        return current;
    }
}