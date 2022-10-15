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
        var knownAtoms = KnownAtoms.ToDictionary(x => x, x => x.Replace(" ", "_") + ",");
        _testOutputHelper.WriteLine(posts!
            .Where(p => p.Genre == "\"")
            .Select(p => p.Url)
            .StrJoin(Environment.NewLine));

        IEnumerable<(string Full, string Atom)> SplitPunctuation(string x) =>
            x.Split(new[] { "\\", "/", ",", "&", " > ", " - ", "–", " and ", "(", ")", "--", "\"", ".", "{", "}", "|", " и ", "+", ":", ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(y => y.Trim())
                .Where(y => !BlacklistedAtoms.Contains(y))
                .Select(ReplaceKnownAtoms)
                .SelectMany(y => y.Split(new []{' ', ','}, StringSplitOptions.RemoveEmptyEntries))
                .Where(y => !BlacklistedAtoms.Contains(y))
                .Select(Translate)
                .SelectMany(MultiplyAtoms)
                .Select(y => (Full: x, Atom: y));

        IEnumerable<string> MultiplyAtoms(string input)
        {
            if (input == "иронический_детектив")
            {
                yield return "humor";
                yield return "detective";
            }
            else if (input == "историко-приключенческий")
            {
                yield return "history";
                yield return "adventure";
            }
            else if (input == "сказка-классика-приключения")
            {
                yield return "fairytale";
                yield return "classic";
                yield return "adventure";
            }
            else if (input == "fiction-suspense")
            {
                yield return "fiction";
                yield return "suspense";
            }
            else yield return input;
        }
        await @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4-non.json".SaveJson(posts!
            .Select(p => p.Genre?.ToLowerInvariant())
            .Where(NotInBlacklist)
            .WhereNotNull()
            .Select(Clean)
            .SelectMany(SplitPunctuation)
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
        "19th century", "young adult", "adult fiction", "alternate history",
        "ancient history", "asian literature", "black humor",
        "chick lit", "cultural asia", "new age", "иронический детектив",
        "юношеская проза", "черный юмор","критический реализм","проза",
        "научная фантастика","путевые заметки",
        "боевые искусства","альтернативная история","магический реализм",
        "post apocalyptic","prisoners of war","non fiction","new adult",
        "space opera","боевые единоборства","martial arts","fairy tales",
        "fairy tale","forgotten realms","time travel"

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
        "science", "sequel", "канады", "литература сша", "литература на английском языке",
        "русский", "интеллектуальный", "художественный", "художественная",
        "худлжественная", "романы", "роман", "afternoon drama", "повесть", "о", "новелла",
        "на","мировая","зарубежный","зарубежная","записки","беллетристика","аудио",
        "английская","английской","американская","научная","литературы",
        "литература","книга","для","бестселлер","публицистика","публицистический","проза",
        "bbc","dramatisation","dramatization","dramatizations","novell","music",
        "literary","life","international","hard","general","audio","books","british",
        "case","cast","channeling","genre","hard-boiled","high","soft","weird",
        "культовая","капиталистическая","века","заданную","люди","против","с",
        "тему","cross-genre","epistolary","traditional","domestic","full",
        "магов","realism","realistic","элементами"

    };
    private static readonly Dictionary<string, string> Translation = new()
    {
        ["эпос"] = "epic",
        ["fairy_tale"] = "fairytale",
        ["бит"] = "bit",
        ["аллегория"] = "allegory",
        ["женский"] = "women",
        ["античная"] = "antiquity",
        ["авантюрно-криминальный"] = "crime",
        ["контркультура"] = "counterculture",
        ["модернизм"] = "modernism",
        ["мокьюментари"] = "mockumentary",
        ["альтернативная_история"] = "alternate_history",
        ["fairy_tales"] = "fairytale",
        ["martial_arts"] = "martial-arts",
        ["nonfiction"] = "non-fiction",
        ["остросюжетный"] = "thriller",
        ["социальная"] = "social",
        ["фарс"] = "farce",
        ["пародия"] = "parody",
        ["плутовской"] = "humor",
        ["полицейский"] = "police",
        ["пост-киберпанк"] = "post-cyberpunk",
        ["ритрит"] = "retreat",
        ["инфотейнмент"] = "infotainment",
        ["инфотэйнмент"] = "infotainment",
        ["дневник"] = "diary",
        ["драма"] = "drama",
        ["боевик"] = "action",
        ["готический"] = "gothic",
        ["криминальная"] = "crime",
        ["героическая"] = "heroic",
        ["криминальный"] = "crime",
        ["комический"] = "comedy",
        ["комедия-фарс"] = "comedy",
        ["комедия"] = "comedy",
        ["киберпанк"] = "cyberpunk",
        ["мифы"] = "mythology",
        ["мифология"] = "mythology",
        ["легенды"] = "mythology",
        ["legends"] = "mythology",
        ["сага"] = "saga",
        ["пираты"] = "pirates",
        ["морские"] = "marine",
        ["sea"] = "marine",
        ["космическая"] = "space",
        ["магический_реализм"] = "magic",
        ["боевые_единоборства"] = "martial-arts",
        ["боевые_искусства"] = "martial-arts",
        ["нанопанк"] = "nanopunk",
        ["комиксы"] = "comics",
        ["утопия"] = "utopia",
        ["трагикомедия"] = "tragicomedy",
        ["супергерои"] = "superheroes",
        ["стимпанк"] = "steampunk",
        ["треш"] = "trash",
        ["футуристика"] = "futurism",
        ["чиклит"] = "chick_lit",
        ["espionage"] = "spy",
        ["эпопея"] = "epic",
        ["technothriller"] = "techno-thriller",
        ["teen"] = "teenager",
        ["ghost"] = "ghosts",
        ["comic"] = "comics",
        ["criminals"] = "criminal",
        ["comical"] = "comedy",
        ["dystopian"] = "dystopia",
        ["distopia"] = "dystopia",
        ["постапокалипсис"] = "post-apocalypse",
        ["post_apocalyptic"] = "post-apocalypse",
        ["non_fiction"] = "non-fiction",
        ["постапокалиптика"] = "post-apocalypse",
        ["постапокалиптическая"] = "post-apocalypse",
        ["путешествия"] = "travel",
        ["психология"] = "psychology",
        ["psychologic"] = "psychology",
        ["psychological"] = "psychology",
        ["психологический"] = "psychology",
        ["психологическая"] = "psychology",
        ["путешественника"] = "travel",
        ["путевые_заметки"] = "travel",
        ["эпическое"] = "epic",
        ["черный_юмор"] = "black_humor",
        ["трагедия"] = "tragedy",
        ["техно-триллер"] = "techno-thriller",
        ["технотриллер"] = "techno-thriller",
        ["вампирский"] = "vampires",
        ["вампирах"] = "vampires",
        ["wartime"] = "war",
        ["военный"] = "war",
        ["военная"] = "war",
        ["вестерн"] = "western",
        ["historical"] = "history",
        ["исторический"] = "history",
        ["historical"] = "history",
        ["историческая"] = "history",
        ["историчесикй"] = "history",
        ["историчесий"] = "history",
        ["история"] = "history",
        ["классический"] = "classic",
        ["классическая"] = "classic",
        ["классика"] = "classic",
        ["любовная"] = "romance",
        ["любовный"] = "romance",
        ["мистика"] = "mystical",
        ["magical"] = "magic",
        ["мистицизм"] = "mystical",
        ["мистический"] = "mystical",
        ["mysteries"] = "mystery",
        ["mistery"] = "mystery",
        ["mystic"] = "mystical",
        ["антиутопия"] = "dystopia",
        ["мелодрамма"] = "melodrama",
        ["xix"] = "19th_century",
        ["мелодрама"] = "melodrama",
        ["нуар"] = "noir",
        ["семейный"] = "family",
        ["сатира"] = "satire",
        ["сатирический"] = "satire",
        ["юношеская_проза"] = "young_adult",
        ["juvenile"] = "young_adult",
        ["triller"] = "thriller",
        ["триллера"] = "thriller",
        ["ситком"] = "sitcom",
        ["подростковая"] = "young_adult",
        ["подростковое"] = "young_adult",
        ["подростковый"] = "young_adult",
        ["молодёжная"] = "young_adult",
        ["молодежный"] = "young_adult",
        ["политический"] = "politics",
        ["political"] = "politics",
        ["политеческий"] = "politics",
        ["политика"] = "politics",
        ["приключения"] = "adventure",
        ["приключенческий"] = "adventure",
        ["adventures"] = "adventure",
        ["современный"] = "contemporary",
        ["modern"] = "contemporary",
        ["love"] = "romance",
        ["романтическое"] = "romance",
        ["романтический"] = "romance",
        ["романтическая"] = "romance",
        ["романтика"] = "romance",
        ["сказки"] = "fairytale",
        ["сказка"] = "fairytale",
        ["саспенс"] = "suspense",
        ["современная"] = "contemporary",
        ["шутки юмора"] = "humor",
        ["шутки"] = "humor",
        ["юмор"] = "humor",
        ["фэнтази"] = "fantasy",
        ["фэнтези"] = "fantasy",
        ["фэнтэзи"] = "fantasy",
        ["фентези"] = "fantasy",
        ["хоррор"] = "fantasy",
        ["ужас"] = "fantasy",
        ["ужасов"] = "fantasy",
        ["спорт"] = "sport",
        ["триллер"] = "thriller",
        ["шпионский"] = "thriller",
        ["эротика"] = "erotic",
        ["sf"] = "sci-fi",
        ["romantic"] = "romance",
        ["фантастика"] = "sci-fi",
        ["фантастический"] = "sci-fi",
        ["научно-фантастический"] = "sci-fi",
        ["научная_фантастика"] = "sci-fi",
        ["si-fi"] = "sci-fi",
        ["остросюжетная"] = "thriller",
        ["юмористическое"] = "humor",
        ["юмористический"] = "humor",
        ["юмористические"] = "humor",
        ["юмористическая"] = "humor",
        ["юмористичекий"] = "humor",
        ["юмористичекая"] = "humor",
        ["юмора"] = "humor",
        ["фикция"] = "fiction",
        ["детективная"] = "detective",
        ["детективный"] = "detective",
        ["детектив"] = "detective",
        ["detectiv"] = "detective",
    };

    private static string Translate(string input) =>
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
