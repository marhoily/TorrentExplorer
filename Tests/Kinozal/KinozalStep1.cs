using System.Text;
using System.Xml.Linq;
using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Kinozal;

public class KinozalStep1
{
    [Fact]
    public async Task Convert()
    {
        await using var fileStream = File.OpenRead(@"c:\temp\kinozal-bulk.xml");
        var xml = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
        var result = new List<List<string>>();
        foreach (var htmlNode in xml.Root!.Elements())
        {
            var collector = new Collector();
            var s = htmlNode.ToString();
            collector.Parse(s.ParseHtml());
            result.Add(collector.Elements);
        }
        await @"C:\temp\TorrentsExplorerData\KinozalExtract\Step1.json"
            .SaveJson(result);
    }
}

public abstract record Style;

public sealed record FontSize(int Value) : Style;
public sealed record Color : Style;
public sealed record Bold : Style;
public sealed record Center : Style;
public sealed record FontFamily : Style;
public sealed record LineBreak : Style;
public sealed record Italics : Style;
public sealed record Underline : Style;
public sealed record Normal : Style;

public sealed class Collector
{
    public List<string> Elements { get; } = new();
    private readonly StringBuilder _buffer = new();
    private readonly HashSet<string> _tags = new();
    public void Parse(HtmlNode htmlNode)
    {
        ParseRoot(htmlNode);
        PushTheBuffer();
    }

    public void ParseRoot(HtmlNode htmlNode)
    {
        foreach (var n in htmlNode.ChildNodes)
        {
            switch (n.Name)
            {
                case "a":
                    Elements.Add("<link> " + n.InnerText);
                    break;
                case "br":
                    _buffer.AppendLine();
                    break;
                case "var":
                case "img":
                    // image. skip
                    break;
                case "ol":
                case "ul":
                    PushTheBuffer();
                    break;
                case "hr":
                    PushTheBuffer();
                    break;
                case "div":
                    var attributesSnapshot = GetAttributesSnapshot(n);
                    switch (n.GetAttributeValue("class", null))
                    {
                        case "bx1 justify": 
                        case "post-box": 
                        case "post-box-right":
                        case "c-wrap":
                        case "c-head":
                        case "q": // quote
                        case "q-wrap": // quote
                        case "q-head": // quote                         
                        case "post-ul": // ul
                            ParseRoot(n);
                            return;
                    }
                    switch (attributesSnapshot)
                    {
                        case "class='sp-wrap'": // spoiler
                            var head = n.SelectSingleNode("div[@class='sp-head folded']");
                            Elements.Add("<Spoiler> " + head.InnerText);
                            break;
                        case "style='margin-left: 2em' type='1'":
                            break;
                        case "class='post-box-default'":
                            Elements.Add("<post-box-default> " + n.InnerText);
                            break;
                        default:
                            throw new Exception(attributesSnapshot + "\n" + n.InnerText);
                    }
                    break;
                case "span":
                    var tag = ClassifySpan(n) switch
                    {
                        FontSize(> 20) => "Header",
                        Bold or LineBreak => "",
                        Center or FontFamily or Color or FontSize or
                            Italics or Underline or Normal
                            => null,
                        var x => throw new ArgumentOutOfRangeException(x.ToString())
                    };
                    if (tag != null)
                    {
                        PushTheBuffer();
                        if (tag != "") _tags.Add(tag);
                    }
                    ParseRoot(n);
                    if (tag != null)
                    {
                        PushTheBuffer();
                        _tags.Remove(tag);
                    }
                    break;
                case "b":
                    PushTheBuffer();
                    ParseRoot(n);
                    PushTheBuffer();
                    break;
                case "u":
                case "h2":
                    ParseRoot(n);
                    break;
                case "#text":
                    _buffer.Append(n.InnerText);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(n.NodeType + ": " + n.Name);
            }
        }

    }

    private static Style ClassifySpan(HtmlNode node)
    {
        var snapshot = GetAttributesSnapshot(node);
        switch (snapshot)
        {
            case "":
                return new Normal();
            case "class='post-align' style='text-align: center;'":
                return new Center();
            case "class='post-font-serif1'":
            case "class='post-font-serif2'":
            case "class='post-font-impact'":
            case "class='post-font-mono1'":
            case "class='post-font-mono2'":
            case "class='post-font-cursive1'":
            case "class='post-font-cursive2'":
            case "class='post-font-sans1'":
            case "class='post-font-sans2'":
            case "class='post-font-sans3'":
                return new FontFamily();
            case "class='post-br'":
                return new LineBreak();
            case "class='post-i'":
                return new Italics();
            case "class='post-u'":
                return new Underline();
            case "class='post-s'":
                return new Bold();
        }

        var keys = node.Attributes.Select(a => a.Name).OrderBy(x => x).StrJoin();
        if (keys == "style")
        {
            var style = GetStyle(node);
            if (style?.Keys.StrJoin() == "font-size, line-height")
                if (style["line-height"] == "normal")
                    return new FontSize(style["font-size"].RemovePostfix("px")!.ParseInt());
        }
        else if (keys == "class")
        {
            var @class = node.GetAttributeValue("class", null);
            if (@class == "post-b")
                return new Bold();
        }
        else if (keys == "class, style")
        {
            var @class = node.GetAttributeValue("class", null);
            if (@class == "p-color")
            {
                var style = GetStyle(node);
                if (style?.Keys.StrJoin() == "color")
                    return new Color();
            }

            return new Bold();
        }
        throw new InvalidOperationException(
            $"<{node.Name} {node.Attributes.Select(a => a.Name + "=" + a.Value.Quote()).StrJoin(" ")}>");
    }

    private static Dictionary<string, string>? GetStyle(HtmlNode node)
    {
        return node
            .GetAttributeValue("style", null)?
            .Split(";", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split(": "))
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
    }
    private static string GetAttributesSnapshot(HtmlNode node)
    {
        return node.Attributes.Select(a => a.Name + "=" + a.Value.Quote()).StrJoin(" ");
    }

    private void PushTheBuffer()
    {
        if (_buffer.Length > 0)
        {
            var value = _buffer.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
                Elements.Add(_tags.Count > 0 ?
                    $"<{_tags.StrJoin()}> {value}"
                    : value);
        }

        _buffer.Clear();
    }

}