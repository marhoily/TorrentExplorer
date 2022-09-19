using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class WallCollector
{
    private readonly WallCollectorState _state;
    private readonly Cursor _cursor;

    public WallCollector(HtmlNode htmlNode)
    {
        _state = new WallCollectorState();
        _cursor = new Cursor(htmlNode);
    }

    public JArray Parse()
    {
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

        _cursor.GoFurther().SkipWhile(c => c.InnerText.HtmlTrim() == "");

        if (!key.EndsWith(":"))
            if (_cursor.Node?.InnerText.HtmlTrim().StartsOrEndsWith(':') != true)
                return;

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