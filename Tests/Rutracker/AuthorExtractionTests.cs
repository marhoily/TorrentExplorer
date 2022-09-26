using Microsoft.VisualStudio.TestPlatform.Utilities;
using Newtonsoft.Json.Linq;
using Tests.UniversalParsing;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record RawAuthor(
    string? FirstName, string? LastName,
    string? FirstNames, string? LastNames,
    string? Name, string? Names);

public sealed class AuthorExtractionTests
{
    public const string Raw = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-raw.json";
    public const string Refined = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-refined.json";

    public static RawAuthor ToRawAuthor(JObject post)
    {
        string? Get(params string[] keys) => post.FindTags(keys)?.SkipLong(20);
        return new RawAuthor(
            Get("Имя автора"), 
            Get("Фамилия автора", "Фамилия автора сценария"),
            Get("Имена авторов"), 
            Get("Фамилии авторов", "Фамилия авторов"),
            Get("Автор"), 
            Get("Фамилии и имена авторов", "Автора", "Авторы"));
    }

    [Fact]
    public async Task SaveRaw()
    {
        var posts = await Step2.Output.ReadJson<JObject[][]>();

        await Raw.SaveJson(posts!
            .SelectMany(p => p)
            .WhereNotNull()
            .Select(ToRawAuthor)
            .ToList());
    }

    [Fact]
    public async Task Authors()
    {
        var posts = await Refined.ReadJson<RawAuthor[]>();

        await Refined.SaveJson(posts!
            .SelectMany(section => section.Refine())
            .ToList());
    }
}