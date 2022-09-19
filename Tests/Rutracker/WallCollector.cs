using HtmlAgilityPack;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class WallCollector
{
    private WallCollectorState _state = null!;
    private Cursor _cursor = null!;

    public List<Dictionary<string, object>> Parse(HtmlNode htmlNode)
    {
        _state = new WallCollectorState(htmlNode.GetTopicId());
        _cursor = new Cursor(htmlNode);
        MoveOn();
        _state.PushCurrentSection();
        return _state.Sections;
    }

    private void MoveOn()
    {
        if (_state.TopicId == 3470993)
            1.ToString();
        var body = false;
        while (_cursor.Node != null)
        {
            if (_cursor.Node.IsHeader())
            {
                if (body)
                {
                    _state.PushCurrentSection();
                    body = false;
                }

                _state.AddHeader(_cursor.Node.InnerText);
                _cursor.GoFurther();
            }
            else if (_cursor.Node.InnerText.IsKnownTag())
            {
                _cursor.Set(ProcessTag(_cursor.Node));
                body = true;
            }
            else if (_cursor.Node.HasClass("sp-wrap"))
            {
                _state.AddSpoiler(_cursor.Node
                    .SelectSubNode("div[@class='sp-head folded']")!
                    .InnerText);
                _cursor.GoFurther();
                body = true;
            }
            else
            {
                _cursor.GoDeeper();
            }
        }
    }

    private HtmlNode? ProcessTag(HtmlNode start)
    {
        var key = start.InnerText.Replace("&nbsp;", " ").TrimEnd();

        var colonNode = key.EndsWith(":") ? start : SkipEmpty(start);
        if (colonNode?.InnerText.Trim() is not { } text ||
            !text.Replace("&nbsp;", "").StartsWith(":") && 
            !text.Replace("&nbsp;", "").EndsWith(":"))
            return colonNode;

        var valueNode = start != colonNode ? colonNode : colonNode.GoFurther();
        while (valueNode?.InnerText.Trim() is ":" or "")
            valueNode = valueNode.GoFurther();

        if (valueNode != null)
            _state.AddAttribute(
                key.TrimEnd(':').Trim(),
                valueNode.InnerText
                    .TrimStart(':', ' ')
                    .Replace("&#776;", "")
                    .Trim());
        return valueNode;
    }

    private static HtmlNode? SkipEmpty(HtmlNode start)
    {
        var current = start;
        do
        {
            current = current.GoFurther();
            if (current == null) break;
        } while (string.IsNullOrWhiteSpace(current.InnerText));

        return current;
    }
}