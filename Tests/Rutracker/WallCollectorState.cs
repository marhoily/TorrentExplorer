using System.Net;

namespace Tests.Rutracker;

internal sealed class WallCollectorState
{
    private readonly int _topicId;
    private List<string> _currentHeaders = new();
    private List<string> _currentSpoilers = new();
    private readonly Dictionary<string, object> _currentSection = new();
    public List<Dictionary<string, object>> Sections { get; } = new();

    public WallCollectorState(int topicId)
    {
        _topicId = topicId;
    }

    public void PushCurrentSection()
    {
        if (_currentSection.Count <= 0) return;
        var tmp = new Dictionary<string, object>
        {
            [_topicId.ToString()] = $"https://rutracker.org/forum/viewtopic.php?t={_topicId}",
            ["headers"] = _currentHeaders
        };
        foreach (var (k, v) in _currentSection) tmp.Add(k, v);
        if (_currentSpoilers.Count > 0)
        {
            tmp["spoilers"] = _currentSpoilers;
            _currentSpoilers = new List<string>();
        }
        Sections.Add(tmp);
        _currentHeaders = new List<string>();
        _currentSection.Clear();
    }

    public void AddHeader(string value) =>
        _currentHeaders.Add(WebUtility.HtmlDecode(value));

    public void AddAttribute(string key, string value)
    {
        if (!_currentSection.ContainsKey(key))
            _currentSection.Add(key, WebUtility.HtmlDecode(value));
    }

    public void AddSpoiler(string value) =>
        _currentSpoilers.Add(WebUtility.HtmlDecode(value));
}