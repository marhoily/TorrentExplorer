using Tests.Utilities;
using Xunit.Abstractions;

namespace Tests.Rutracker;

public sealed class Step4
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Step4(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task SelectPoliglot()
    {
        var posts = await Step3.Output.ReadJson<Story[]>();
        await @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4.json".SaveJson(posts!
            .Where(p => p.ReleaseGroup == "Полиглот")
            .GroupBy(p => p.Genre)
            .ToDictionary(g => $"{g.Key ?? "???"} {g.Count()}"));
    }

    [Fact]
    public async Task SortBlacklist()
    {
        var blacklist = await File.ReadAllLinesAsync(@"C:\temp\bad-genres.txt");
        await File.WriteAllLinesAsync(
            @"C:\temp\bad-genres.txt",
            blacklist.OrderBy(x => x));
    }

    [Fact]
    public async Task SelectNonPoliglot()
    {
        var blacklist = await File.ReadAllLinesAsync(@"C:\temp\bad-genres.txt");
        var posts = await Step3.Output.ReadJson<Story[]>();
        _testOutputHelper.WriteLine(posts!
            .Where(p => p.Genre == "\"")
            .Select(p => p.Url)
            .StrJoin(Environment.NewLine));
        await @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4-non.json".SaveJson(posts!
            .Select(p => p.Genre?.ToLowerInvariant())
            .Where(NotInBlacklist)
            .WhereNotNull()
            .Select(Clean)
            .SelectMany(x => x.Split(new[]
                {
                    "\\", "/", ",", "&", " > ", " - ", "–", " and ",
                    "(", ")", "--", "\"", ".", "{", "}", "|", " и ", "+", ":", ";"
                },
                StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim())
            .Select(Translate)
            .Distinct()
            .OrderBy(x => x));

        bool NotInBlacklist(string? g) =>
            !string.IsNullOrWhiteSpace(g) && 
            blacklist.All(sample => !g.Contains(sample));
    }

    private static readonly Dictionary<string, string> Translation = new()
    {
        ["шутки юмора"] = "humor",
        ["шутки"] = "humor",
        ["юмор"] = "humor",
        ["фэнтази"] = "fantasy",
        ["фэнтези"] = "fantasy",
        ["фэнтэзи"] = "fantasy",
        ["фентези"] = "fantasy",
        ["хоррор"] = "fantasy",
        ["ужас"] = "fantasy",
        ["триллер"] = "thriller",
        ["шпионский"] = "thriller",
        ["эротика"] = "erotic",
        ["фантастика"] = "sci-fi",
        ["фантастический роман"] = "sci-fi",
       // ["трагедия"] = "tragedy",
    };
    static string Translate(string input) => 
        Translation.TryGetValue(input, out var result) ? result : input;

    static string Clean(string g)
    {
        return g
            .Replace("si,fi", "sci-fi")
            .Replace("sci fi", "sci-fi")
            .Replace("sci-fic", "sci-fi")
            .Replace("si-fi", "sci-fi")
            .Replace("приключение", "приключения")
            .Replace("science fiction", "sci-fi")
            .Replace("science-fiction", "sci-fi")
            .Replace("fantastic fiction", "si-fi")
            .Replace("ужос", "ужасы")
            .Replace("ужасы", "ужас")
            .Replace("страшилки", "ужас")
            .Replace("fantastic", "si-fi")
            .Replace("youg", "young")
            .Replace("young-adult", "young adult")
            .Replace("adults", "adult")
            .Replace("детекив", "detective")
            .Replace("деттектив", "detective")
            .Replace("дискуссия", "detective")
            .Replace("для детей", "detective")
            .Replace("кибер-панк", "киберпанк")
            .Replace("westerns", "western")
            .Replace("dетектив", "detective")
            .Replace("fantasz", "fantasy")
            .Replace("fantasz", "fantasy")
            .Replace("fantazy", "fantasy")
            .Replace("klassika", "classic")
            .Replace("classics", "classic")
            .Replace("suspance", "suspense")
            .Replace("suspence", "suspense")
            .Replace("erotica", "erotic")
            .Replace("humour", "humor")
            .TrimEnd('/', ' ');
    }
}
