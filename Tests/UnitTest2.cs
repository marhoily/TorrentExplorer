using System.Text;
using FluentAssertions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tests.Utilities;
using Xunit.Abstractions;

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
                .SaveJson(collector.Sections);
        }
    }
}

public sealed class Collector
{
    public List<Dictionary<string, string>> Sections { get; } = new();
    private Dictionary<string, string> _currentSection = new();
    bool _isHeaderSize;
    private string? _currentKey;
    private bool _colon;
    private readonly StringBuilder _currentValue = new();
    public int ThreadId { get; private set; }
    public void Parse(HtmlNode htmlNode)
    {
        var attributeValue = htmlNode.GetAttributeValue("data-ext_link_data", null);
        var jObject = JObject.Parse(attributeValue);
        ThreadId = jObject["t"]!.Value<int>();
        ParseRoot(htmlNode);
        FinishKey();
        if (_currentSection.Count > 0)
            PushCurrentSection();
    }
    public void ParseRoot(HtmlNode htmlNode)
    {
        foreach (var n in htmlNode.ChildNodes)
        {
            var innerText = n.InnerText.Trim();
            switch (n.Name)
            {
                case "a":
                    if (_colon)
                    {
                        ParseRoot(n);
                        break;
                    }
                    FinishKey();
                    _currentSection.Add(n.InnerText, n.GetAttributeValue("href", null));
                    break;
                case "br":
                    if (_currentValue.Length > 0)
                    {
                        _currentKey.Should().NotBeNull();
                        _currentSection.Add(_currentKey!, _currentValue.ToString());
                        _colon = false;
                        _currentKey = null;
                        _currentValue.Clear();
                    }
                    if (!_colon)
                    {
                        if (_currentKey?.Length > 100)
                        {
                            _currentKey = null;
                            break;
                        }
                        _currentKey.Should().BeNull();
                    }
                    break;
                case "var":
                    // Image. Skip
                    break;
                case "hr":
                    // Horizontal line. Skip
                    break;
                case "div":
                    if (n.GetAttributeValue("class", null) == "post_body")
                        ParseRoot(n);
                    break;
                case "span":
                    var style = n
                        .GetAttributeValue("style", null)?
                        .Split("; ")
                        .ToDictionary(x => x.Split(": ")[0], x => x.Split(": ")[1]);

                    _isHeaderSize =
                        style?.TryGetValue("font-size", out var fontSize) == true &&
                        fontSize.RemovePostfix("px")?.ParseIntOrNull() > 20;
                    if (_isHeaderSize)
                    {
                        if (_currentSection.ContainsKey("<HEADER-SIZE>"))
                            PushCurrentSection();
                        _currentSection["<HEADER-SIZE>"] = n.InnerText;
                        break;
                    }

                    //var @class = n.GetAttributeValue("class", null).Split(' ');
                    //if (@class.Contains("post-b"))
                    //{
                    //    _currentKey = innerText;
                    //    break;
                    //}
                    // assume it's just some additional style  
                    ParseRoot(n);
                    //throw new ArgumentOutOfRangeException(n.NodeType + ": " + n.Name);
                    break;
                case "#text":
                    if (innerText == "")
                        break;
                    if (_currentKey == null)
                    {
                        if (innerText.EndsWith(":"))
                        {
                            _currentKey = innerText.TrimEnd(':');
                            _colon = true;
                        }
                        else _currentKey = innerText;
                        break;
                    }
                    if (_currentKey != null)
                    {
                        if (_colon)
                        {
                           // _currentSection.Add(_currentKey, innerText);
                           // _colon = false;
                           // _currentKey = null;
                           _currentValue.Append(n.InnerText);
                            break;

                        }
                        else if (innerText == ":")
                        {
                            _colon = true;
                            break;
                        }
                        else if (innerText.StartsWith(":"))
                        {
                           // _currentSection.Add(_currentKey, innerText.TrimStart(':', ' '));
                           // _colon = false;
                           // _currentKey = null;
                           _currentValue.Append(n.InnerText.TrimStart(':', ' '));

                            break;
                        }
                    }

                    _currentValue.Append(n.InnerText);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(n.NodeType + ": " + n.Name);

            }
        }
    }

    private void FinishKey()
    {
        _colon.Should().BeFalse();
        if (_currentKey != null)
            _currentSection[_currentKey] = "<TAG>";
        _currentKey = null;
    }

    private void PushCurrentSection()
    {
        Sections.Add(_currentSection);
        _currentSection = new();
    }
}