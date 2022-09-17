using System.Net;
using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class WallCollector
{
    public List<Dictionary<string, object>> Sections { get; } = new();
    private readonly Dictionary<string, object> _currentSection = new();
    private HtmlNode? _currentNode;
    private List<string> _currentHeaders = new();
    private int _topicId;

    public void Parse(HtmlNode htmlNode)
    {
        _topicId = htmlNode.GetTopicId();
        _currentNode = htmlNode;
        MoveOn();
        PushCurrentSection();
    }

    private void MoveOn()
    {
        var body = false;
        while (_currentNode != null)
        {
            if (_currentNode.IsHeader())
            {
                if (body)
                {
                    PushCurrentSection();
                    body = false;
                }
                _currentHeaders.Add(WebUtility.
                    HtmlDecode(_currentNode.InnerText));
                _currentNode = GoFurther(_currentNode);
            }
            else if (_currentNode.InnerText.IsKnownTag())
            {
                _currentNode = ProcessTag(_currentNode);
                body = true;
            }
            else if (_currentNode.HasClass("sp-wrap"))
            {
                _currentSection[WebUtility.HtmlDecode(_currentNode
                    .SelectSubNode("div[@class='sp-head folded']")!
                    .InnerText)] = "spoiler";
                _currentNode = GoFurther(_currentNode);
                body = true;
            }
            else if (_currentNode != null)
                _currentNode = GoDeeper(_currentNode);
        }
    }

    private static HtmlNode? GoFurther(HtmlNode n) =>
        n.NextSibling ?? (
            n.ParentNode != null 
                ? GoFurther(n.ParentNode) 
                : null);

    private static HtmlNode? GoDeeper(HtmlNode n) =>
        n.ChildNodes.FirstOrDefault(
            x => x.NodeType == HtmlNodeType.Element) 
        ?? GoFurther(n);

    private HtmlNode ProcessTag(HtmlNode node)
    {
        var key = node.InnerText.TrimEnd();
        var seenComma = key.EndsWith(":");
        var moved = false;
        while (!seenComma)
        {
            if (node.NextSibling != null)
            {
                moved = true;
                node = node.NextSibling;
            }
            else node = node.ParentNode;

            var text = node.InnerText.Trim();
            seenComma = text.EndsWith(":") || text.StartsWith(":");
        }

        while (!moved || node.InnerText.Trim() == ":")
        {
            if (node.NextSibling != null)
            {
                moved = true;
                node = node.NextSibling;
            }
            else node = node.ParentNode;
        }

        _currentSection[WebUtility.HtmlDecode(key.TrimEnd(':').Trim())]
            = WebUtility.HtmlDecode(node.InnerText
                .TrimStart(':', ' ')
                .Replace("&#776;", "")
                .Trim());
        return node;
    }

    private void PushCurrentSection()
    {
        if (_currentSection.Count <= 0) return;
        var tmp = new Dictionary<string, object>
        {
            [_topicId.ToString()] = $"https://rutracker.org/forum/viewtopic.php?t={_topicId}",
            ["headers"] = _currentHeaders
        };
        foreach (var (k, v) in _currentSection) tmp.Add(k, v);
        Sections.Add(tmp);
        _currentHeaders = new List<string>();
        _currentSection.Clear();
    }
}