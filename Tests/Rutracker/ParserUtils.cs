using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Tests.Rutracker;

public sealed record Spoiler(string Header, HtmlNode Body);

public static class ParserUtils
{
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
                var trimEnd = d.InnerText
                    .TrimStart('�', ' ', '•')
                    .TrimEnd(':', ' ');
                if (trimEnd == value)
                    yield return d;
            }
        }
    }

    public static HtmlNode? FindTags(this HtmlNode node, params string[] values)
        => values
            .Select(value => FindTag(node, value))
            .FirstOrDefault(t => t != null);

    public static HtmlNode? FindTag(this HtmlNode node, string value)
    {
        var htmlNodes = node.FindTags(value).ToList();
        if (htmlNodes.Count == 0) return null;
        return htmlNodes[0];
    }

    public static string? FindTags(this Dictionary<string, object> dic, params string[] keys) =>
        keys.Select(dic.FindTag).FirstOrDefault(t => t != null);

    public static string? FindTag(this Dictionary<string, object> dic, string key) =>
        dic.TryGetValue(key, out var result) ? result.ToString() : default;

    public static string? FindTags(this JObject dic, params string[] keys) =>
        keys.Select(dic.FindTag).FirstOrDefault(t => t != null);

    public static string? FindTag(this JObject dic, string key) =>
        dic.TryGetValue(key, out var result) ? result.ToString() : default;
}