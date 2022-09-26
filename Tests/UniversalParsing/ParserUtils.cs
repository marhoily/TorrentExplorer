using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using ServiceStack;
using Tests.Utilities;

namespace Tests.UniversalParsing;

public static class ParserUtils
{
    public static string? FindTags(this JObject dic, params string[] keys) => 
        keys.Select(dic.FindTag).FirstNonDefaultOrEmpty();

    public static string? SkipLong(this string input, int wordsCount) => 
        input.Split(' ').Skip(wordsCount).Any() == true ? null : input;

    public static string? FindTag(this JObject dic, string key) =>
        dic.TryGetValue(key, out var result) ? result.ToString() : default;

    public static bool IsHeader(this XElement node) => 
        (node.GetFontSize() ?? 0) > 20;
}