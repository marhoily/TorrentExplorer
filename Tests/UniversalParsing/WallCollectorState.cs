using System.Net;
using Newtonsoft.Json.Linq;

namespace Tests.UniversalParsing;

internal sealed class WallCollectorState
{
    private JArray _headers = new();
    private JArray _spoilers = new();
    private readonly JObject _attributes = new();

    public JArray Sections { get; } = new();


    public void PushCurrentSection()
    {
        if (_attributes.Count <= 0) return;
        var tmp = new JObject
        {
            ["headers"] = _headers
        };

        foreach (var (k, v) in _attributes)
            tmp.Add(k, v);
        if (_spoilers.Count > 0)
        {
            tmp["spoilers"] = _spoilers;
            _spoilers = new JArray();
        }
        Sections.Add(tmp);
        _headers = new JArray();
        _attributes.RemoveAll();
    }

    public void AddHeader(string value) =>
        _headers.Add(WebUtility.HtmlDecode(value));

    public void AddAttribute(string key, string value)
    {
        if (!_attributes.ContainsKey(key))
            _attributes.Add(key, WebUtility.HtmlDecode(value));
    }

    public void AddSpoiler(string value) =>
        _spoilers.Add(WebUtility.HtmlDecode(value));
}