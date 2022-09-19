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

    private HtmlNode? ProcessTag(HtmlNode node)
    {
        var key = node.InnerText.Replace("&nbsp;", " ").TrimEnd();
        var seenColon = key.EndsWith(":");
        var start = node;

        if (!seenColon)
        {
            node = SkipEmpty(node);
            if (node == null)
                return null;
            var text = node.InnerText.Trim();
            if (!text.EndsWith(":") && !text.StartsWith(":"))
                return node;
        }        

        var goFurther = start != node ? node : node.GoFurther();
        while (goFurther?.InnerText.Trim() is ":" or "")
            goFurther = goFurther.GoFurther();

        if (goFurther != null)
            _state.AddAttribute(
                key.TrimEnd(':').Trim(),
                goFurther.InnerText
                    .TrimStart(':', ' ')
                    .Replace("&#776;", "")
                    .Trim());
        return goFurther;
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