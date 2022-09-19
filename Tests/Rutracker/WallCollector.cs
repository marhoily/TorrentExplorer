using HtmlAgilityPack;
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

    private HtmlNode? ProcessTag(HtmlNode keyNode)
    {
        var key = keyNode.InnerText.HtmlTrim();

        var afterKey = !key.EndsWith(":") 
            ? keyNode.GoFurther()?.SkipWhile(c => c.InnerText.HtmlTrim() == "") 
            : keyNode;
        if (afterKey?.InnerText.HtmlTrim().StartsOrEndsWith(':') != true)
            return afterKey;

        var interim = afterKey == keyNode ? afterKey.GoFurther() : afterKey;
        var valueNode = interim?.SkipWhile(c => c.InnerText.HtmlTrim() is ":" or "");

        if (valueNode != null)
            _state.AddAttribute(
                key.TrimEnd(':').TrimEnd(),
                valueNode.InnerText
                    .TrimStart(':', ' ')
                    .Replace("&#776;", "")
                    .Trim());
        return valueNode;
    }
}