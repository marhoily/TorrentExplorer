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
                ProcessTag();
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

    private void ProcessTag()
    {
        var keyNode = _cursor.Node!;
        var key = keyNode.InnerText.HtmlTrim();
        
        if (!key.EndsWith(":")) 
            _cursor.GoFurther().SkipWhile(c => c.InnerText.HtmlTrim() == "");
        
        if (_cursor.Node?.InnerText.HtmlTrim().StartsOrEndsWith(':') != true)
            return;

        if (_cursor.Node == keyNode)
            _cursor.GoFurther();

        _cursor.SkipWhile(c => c.InnerText.HtmlTrim() is ":" or "");

        if (_cursor.Node != null)
            _state.AddAttribute(
                key.TrimEnd(':').TrimEnd(),
                _cursor.Node.InnerText
                    .TrimStart(':', ' ')
                    .Replace("&#776;", "")
                    .Trim());
    }
}