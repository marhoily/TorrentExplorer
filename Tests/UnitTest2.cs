using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Tests.Utilities;

namespace Tests;

public class UnitTest2
{

    [Fact]
    public async Task Test1()
    {
        var htmlList = await @"c:\temp\bulk.json".ReadJson<string[]>();
        foreach (var htmlNode in htmlList!.Select(html => html.ParseHtml()))
        {
            var collector = new Collector();
            collector.Parse(htmlNode.ChildNodes[0]);
            //collector.Sections.Should().NotBeEmpty();
            await $@"C:\temp\TorrentsExplorerData\Extract\{collector.ThreadId:D8}.json"
                .SaveJson(collector.Elements);
        }
    }
}

public abstract record Style;

public sealed record FontSize(int Value) : Style;
public sealed record Color(string Value) : Style;
public sealed record Bold : Style;
public sealed record Center : Style;
public sealed record FontFamily : Style;
public sealed record LineBreak : Style;
public sealed record Italics : Style;

public sealed record Element(string Tags, string Value);

public sealed class Collector
{
    public List<Element> Elements { get; }= new();
    private readonly StringBuilder _buffer = new();
    private readonly HashSet<string> _tags = new();
    public int ThreadId { get; private set; }
    public void Parse(HtmlNode htmlNode)
    {
        var attributeValue = htmlNode.GetAttributeValue("data-ext_link_data", null);
        var jObject = JObject.Parse(attributeValue);
        ThreadId = jObject["t"]!.Value<int>();
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
                    Elements.Add(new Element("link", n.InnerText));
                    break;
                case "br":
                    _buffer.AppendLine();
                    break;
                case "var":
                    Elements.Add(new Element("image", "..."));
                    break;
                case "ul":
                    PushTheBuffer();
                    break;
                case "hr":
                    PushTheBuffer();
                    break;
                case "div":
                    if (GetAttributesSnapshot(n) == "class='sp-wrap'")
                    {
                        var head = n.SelectSingleNode("div[@class='sp-head folded']");
                        Elements.Add(new Element("Spoiler", head.InnerText));
                        break;
                    }
                    throw new Exception(n.InnerText);
                case "span":
                    var tag = Classify(n) switch
                    {
                        FontSize(> 20) => "<Header>",
                        Bold => "<B>",
                        Center or FontFamily or Color or FontSize or LineBreak or Italics=> null,
                        var x => throw new ArgumentOutOfRangeException(x.ToString())
                    };
                    if (tag != null)
                    {
                        PushTheBuffer();
                        _tags.Add(tag);
                    }
                    ParseRoot(n);
                    if (tag != null)
                    {
                        PushTheBuffer();
                        _tags.Remove(tag);
                    }
                    break;
                case "#text":
                    _buffer.Append(n.InnerText);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(n.NodeType + ": " + n.Name);
            }
        }

    }

    private static Style Classify(HtmlNode node)
    {
        var snapshot = GetAttributesSnapshot(node);
        if (snapshot == "class='post-align' style='text-align: center;'")
            return new Center();
        if (snapshot == "class='post-font-serif1'")
            return new FontFamily();
        if (snapshot == "class='post-br'")
            return new LineBreak();
        if (snapshot == "class='post-i'")
            return new Italics();

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
                    return new Color(style["color"]);
            }

            return new Bold();
        }
        //   <span class='p-color' style='color: green;'>

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
            Elements.Add(new Element(_tags.StrJoin(), _buffer.ToString()));
        _buffer.Clear();
    }

}