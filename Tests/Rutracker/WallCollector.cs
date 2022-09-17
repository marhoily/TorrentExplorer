using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class WallCollector
{
    public List<Dictionary<string, object>> Sections => _state.Sections;
    private WallCollectorState _state = null!;
    private HtmlNode? _currentNode;

    public void Parse(HtmlNode htmlNode)
    {
        _state = new WallCollectorState(htmlNode.GetTopicId());
        _currentNode = htmlNode;
        MoveOn();
        _state.PushCurrentSection();
    }

    private void MoveOn()
    {
        if (_state.TopicId == 3470993)
            1.ToString();
        var body = false;
        while (_currentNode != null)
        {
            if (_currentNode.IsHeader())
            {
                if (body)
                {
                    _state.PushCurrentSection();
                    body = false;
                }
                _state.AddHeader(_currentNode.InnerText);
                _currentNode = _currentNode.GoFurther();
            }
            else if (_currentNode.InnerText.IsKnownTag())
            {
                _currentNode = ProcessTag(_currentNode);
                body = true;
            }
            else if (_currentNode.HasClass("sp-wrap"))
            {
                _state.AddSpoiler(_currentNode
                    .SelectSubNode("div[@class='sp-head folded']")!
                    .InnerText);
                _currentNode = _currentNode.GoFurther();
                body = true;
            }
            else
            {
                _currentNode = _currentNode?.GoDeeper();
            }
        }
    }

    private HtmlNode ProcessTag(HtmlNode node)
    {
        var key = node.InnerText.TrimEnd();
        var seenColon = key.EndsWith(":");
        var moved = false;
        while (!seenColon)
        {
            if (node.NextSibling != null)
            {
                moved = true;
                node = node.NextSibling;
            }
            else node = node.ParentNode;

            var text = node.InnerText.Trim();
            seenColon = text.EndsWith(":") || text.StartsWith(":");
            if (!seenColon && !string.IsNullOrWhiteSpace(text))
                return node;
        }

        while (!moved || node.InnerText.Trim() is ":" or "")
        {
            if (node.NextSibling != null)
            {
                moved = true;
                node = node.NextSibling;
            }
            else node = node.ParentNode;
        }

        _state.AddAttribute(
            key.TrimEnd(':').Trim(),
            node.InnerText
                .TrimStart(':', ' ')
                .Replace("&#776;", "")
                .Trim());
        return node;
    }
}