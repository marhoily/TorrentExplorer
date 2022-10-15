using System.Collections.Immutable;
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
        var knownAtoms = KnownAtoms.ToDictionary(x => x, x => x.Replace(" ", "_")+",");
        _testOutputHelper.WriteLine(posts!
            .Where(p => p.Genre == "\"")
            .Select(p => p.Url)
            .StrJoin(Environment.NewLine));

        IEnumerable<(string Full, string Atom)> CollectionSelector(string x) =>
            x.Split(new[] { "\\", "/", ",", "&", " > ", " - ", "–", " and ", "(", ")", "--", "\"", ".", "{", "}", "|", " и ", "+", ":", ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(y => y.Trim())
                .Where(y => !BlacklistedAtoms.Contains(y))
                .Select(ReplaceKnownAtoms)
                .Select(Translate)
                .Select(y => (Full: x, Atom: y));

        await @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4-non.json".SaveJson(posts!
            .Select(p => p.Genre?.ToLowerInvariant())
            .Where(NotInBlacklist)
            .WhereNotNull()
            .Select(Clean)
            .SelectMany(CollectionSelector)
            .GroupBy(t => t.Atom)
            .ToImmutableSortedDictionary(
                g => g.Key,
                g => g.Select(i => i.Full).Distinct().OrderBy(x => x)));

        bool NotInBlacklist(string? g) =>
            !string.IsNullOrWhiteSpace(g) &&
            blacklist.All(sample => !g.Contains(sample));
        string ReplaceKnownAtoms(string input) => knownAtoms
            .Aggregate(input, (current, a) => current.Replace(a.Key, a.Value))
            .Replace(",,", ",")
            .TrimEnd(',');
    }


    private static readonly string[] KnownAtoms =
    {
        "19th century","young adult","adult fiction","alternate history",
        "ancient history","asian literature","black humor","chick lit","cultural asia",

    };

    private static readonly HashSet<string> BlacklistedAtoms = new()
    {
        "", "abridged", "audiobook", "classic literature with classical music",
        "story", "tale", "tales", "unabridged", "unabridged audiobook",
        "unadridged audiobook", "английский", "английский язык",
        "аудио книга на английском языке", "аудио книга с распечаткой",
        "аудио книги", "аудиокнига", "аудиокнига двух языках",
        "аудиокнига на английском", "аудиокнига на английском языке", "без сокращений",
        "в дополнении", "др", "см", "сказки венского леса", "современная версия артуровских легенд",
        "экранизированный бестселлер", "a novel", "eng", "english audiobook",
        "english language", "language", "literature", "novel", "novella", "prose",
        "science", "sequel","канады","литература сша","литература на английском языке",
        "русский"
    };
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
        //["a novel"] = "novel",
    };
    static string Translate(string input) =>
        Translation.TryGetValue(input, out var result) ? result : input;

    static string Clean(string g)
    {
        return g
            .Replace("si,fi", "sci-fi")
            .Replace("si-fi", "sci-fi")
            .Replace("sience fiction", "sci-fi")
            .Replace("sci fi", "sci-fi")
            .Replace("sci-fic", "sci-fi")
            .Replace("ya", "young adult")
            .Replace("si-fi", "sci-fi")
            .Replace("thrillers", "thriller")
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
