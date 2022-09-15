using System.Text;
using Tests.Utilities;

namespace Tests;

public class Step2
{
    [Fact]
    public async Task Test1()
    {
        var htmlList = await @"C:\temp\TorrentsExplorerData\step1.json"
            .ReadJson<string[][]>();
        var result = new List<Dictionary<string, string>>();
        foreach (var htmlNode in htmlList!)
        {
            var collider = new Collider();
            collider.Parse(htmlNode);
            result.AddRange(collider.Sections);
        }

        await $@"C:\temp\TorrentsExplorerData\Step2.json"
            .SaveJson(result);
        /*  foreach (var section in result)
          {
              await $@"C:\temp\TorrentsExplorerData\Step2\{section["topic-id"]}.json"
                  .SaveJson(section);

          }*/
    }
}

public sealed class Collider
{
    public List<Dictionary<string, string>> Sections { get; } = new();
    private Dictionary<string, string> _currentSection = new();
    private string? _url;
    private int _topicId;

    public void Parse(string[] lines)
    {
        _url = lines[0];
        _topicId = int.Parse(_url["https://rutracker.org/forum/viewtopic.php?t=".Length..]);
        ParseHeaders(lines, 1);
        PushTheSection();
    }

    private void PushTheSection()
    {
        _currentSection["url"] = _url!;
        _currentSection["topic-id"] = _topicId.ToString("D8");
        Sections.Add(_currentSection);
        _currentSection = new Dictionary<string, string>();
    }

    private void ParseHeaders(string[] lines, int index)
    {
        if (index >= lines.Length) return;
        if (Test(lines, index, "<Header> ") is { } h)
        {
            _currentSection["title"] = h;
            index++;
        }
        ParseAttributes(lines, index);
    }

    private void ParseAttributes(string[] lines, int index)
    {
        if (index >= lines.Length) return;
        if (lines[index].StartsWith("<Header> "))
        {
            PushTheSection();
            ParseHeaders(lines, index);
            return;
        }

        if (lines[index] == ":" || lines[index].StartsWith(":"))
            throw new InvalidOperationException("");
        if (lines[index].StartsWith(":"))
            throw new InvalidOperationException("");
        ParseColon(lines, lines[index], index + 1);
    }

    private void ParseColon(string[] lines, string key, int index)
    {
        if (index >= lines.Length) return;
        if (lines[index].StartsWith("<Header> "))
        {
            _currentSection["#pre-header-" + _currentSection.Count] = key;
            ParseHeaders(lines, index + 1);
        }
        else if (lines[index] == ":")
            ParseAttributeValue(lines, key, index + 1);
        else if (lines[index].StartsWith(":"))
        {
            lines[index] = lines[index].TrimStart(':', ' ');
            ParseAttributeValue(lines, key, index);
        }
    }
    private void ParseAttributeValue(string[] lines, string key, int index)
    {
        if (index >= lines.Length) return;
        if (lines[index].StartsWith("<Header> "))
            throw new InvalidOperationException();
        if (lines[index] == ":" || lines[index].StartsWith(":"))
            throw new InvalidOperationException("double column?");

        var sb = new StringBuilder(lines[index]);
        var colonIdx = FindColon(lines, index + 1);
        for (var i = index + 1; i < colonIdx; i++)
        {
            sb.AppendLine(lines[i]);
        }
        _currentSection[key] = sb.ToString();
        ParseAttributes(lines, colonIdx);
    }

    private int FindColon(string[] lines, int index)
    {
        for (; index < lines.Length; index++)
        {
            if (lines[index] == ":" || lines[index].StartsWith(":"))
            {
                return index - 1;
            }

            if (lines[index].TrimEnd().EndsWith(":"))
                return index;
        }

        return index;
    }

    private string? Test(string[] lines, int index, string test)
    {
        if (index >= lines.Length) return null;
        if (lines[index].StartsWith(test))
            return lines[index][test.Length..];
        return null;
    }
}