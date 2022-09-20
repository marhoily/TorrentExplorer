using Newtonsoft.Json.Linq;

namespace Tests.Rutracker;

public static class ParserUtils
{
    public static string? FindTags(this JObject dic, params string[] keys) =>
        keys.Select(dic.FindTag).FirstOrDefault(t => t != null);

    public static string? FindTag(this JObject dic, string key) =>
        dic.TryGetValue(key, out var result) ? result.ToString() : default;
}