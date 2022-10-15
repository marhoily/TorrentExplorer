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
    public async Task SelectNonPoliglot()
    {
        var posts = await Step3.Output.ReadJson<Story[]>();
        _testOutputHelper.WriteLine(posts!
            .Where(p => p.Genre == "\"")
            .Select(p => p.Url)
            .StrJoin(Environment.NewLine));
        await @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-4-non.json".SaveJson(posts!
            .Select(p => p.Genre?.ToLowerInvariant())
            .Where(Blacklist)
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

        static bool Blacklist(string? g)
        {
            if (string.IsNullOrWhiteSpace(g)) return false;
            if (g.Contains("lectures")) return false;
            if (g.Contains("пояснения")) return false;
            if (g.Contains("теория")) return false;
            if (g.Contains("сочинение")) return false;
            if (g.Contains("poetry")) return false;
            if (g.Contains("мотивация")) return false;
            if (g.Contains("биография")) return false;
            if (g.Contains("археология")) return false;
            if (g.Contains("астрология")) return false;
            if (g.Contains("астрономия")) return false;
            if (g.Contains("атеист")) return false;
            if (g.Contains("native")) return false;
            if (g.Contains("abook")) return false;
            if (g.Contains("manual")) return false;
            if (g.Contains("childrens")) return false;
            if (g.Contains("аффирмации")) return false;
            if (g.Contains("выдержки")) return false;
            if (g.Contains("рассказ")) return false;
            if (g.Contains("религия")) return false;
            if (g.Contains("collection")) return false;
            if (g.Contains("short")) return false;
            if (g.Contains("improv")) return false;
            if (g.Contains("self")) return false;
            if (g.Contains("management")) return false;
            if (g.Contains("storys")) return false;
            if (g.Contains("managment")) return false;
            if (g.Contains("mathematics")) return false;
            if (g.Contains("детская")) return false;
            if (g.Contains("children")) return false;
            if (g.Contains("buisness")) return false;
            if (g.Contains("buddhist")) return false;
            if (g.Contains("business")) return false;
            if (g.Contains("ages 9+")) return false;
            if (g.Contains("analytical")) return false;
            if (g.Contains("biology")) return false;
            if (g.Contains("biography")) return false;
            if (g.Contains("zoology")) return false;
            if (g.Contains("акции")) return false;
            if (g.Contains("physics")) return false;
            if (g.Contains("pickup")) return false;
            if (g.Contains("оккультизм")) return false;
            if (g.Contains("семинар")) return false;
            if (g.Contains("околонаучно")) return false;
            if (g.Contains("этикет")) return false;
            if (g.Contains("этика")) return false;
            if (g.Contains("эссе")) return false;
            if (g.Contains("юриспруденция")) return false;
            if (g.Contains("юридический")) return false;
            if (g.Contains("библейские")) return false;
            if (g.Contains("economic")) return false;
            if (g.Contains("philosophy")) return false;
            if (g.Contains("religion")) return false;
            if (g.Contains("christian")) return false;
            if (g.Contains("documentary")) return false;
            if (g.Contains("talk show")) return false;
            if (g.Contains("txt set")) return false;
            if (g.Contains("presidential")) return false;
            if (g.Contains("«мягкая»")) return false;
            if (g.Contains("нлп")) return false;
            if (g.Contains("религии")) return false;
            if (g.Contains("литературоведение")) return false;
            if (g.Contains("эффективность")) return false;
            if (g.Contains("лекции")) return false;
            if (g.Contains("лекция")) return false;
            if (g.Contains("наука")) return false;
            if (g.Contains("научная литература")) return false;
            if (g.Contains("программа")) return false;
            if (g.Contains("научно-поп")) return false;
            if (g.Contains("научный")) return false;
            if (g.Contains("нейробиология")) return false;
            if (g.Contains("о жизни и творчестве")) return false;
            if (g.Contains("образование")) return false;
            if (g.Contains("образовательный")) return false;
            if (g.Contains("обучающий")) return false;
            if (g.Contains("обучение")) return false;
            if (g.Contains("педагогика")) return false;
            if (g.Contains("переговоры")) return false;
            if (g.Contains("экономика")) return false;
            if (g.Contains("маркетинг")) return false;
            if (g.Contains("математика")) return false;
            if (g.Contains("личный")) return false;
            if (g.Contains("культурология")) return false;
            if (g.Contains("научпоп")) return false;
            if (g.Contains("autobiograrhy")) return false;
            if (g.Contains("astronautics")) return false;
            if (g.Contains("bible")) return false;
            if (g.Contains("biographies")) return false;
            if (g.Contains("бизнес")) return false;
            if (g.Contains("библия")) return false;
            if (g.Contains("биология")) return false;
            if (g.Contains("popular")) return false;
            if (g.Contains("positivity")) return false;
            if (g.Contains("success-training")) return false;
            if (g.Contains("talk-show")) return false;
            if (g.Contains("technology")) return false;
            if (g.Contains("theory")) return false;
            if (g.Contains("investment")) return false;
            if (g.Contains("poem")) return false;
            if (g.Contains("experimental")) return false;
            if (g.Contains("coping with illness")) return false;
            if (g.Contains("neuroscience")) return false;
            if (g.Contains("communism")) return false;
            if (g.Contains("study")) return false;
            if (g.Contains("education")) return false;
            if (g.Contains("spirituality")) return false;
            if (g.Contains("spiurituality")) return false;
            if (g.Contains("medicine")) return false;
            if (g.Contains("social science")) return false;
            if (g.Contains("ww ii")) return false;
            if (g.Contains("пособие")) return false;
            if (g.Contains("популярная психология")) return false;
            if (g.Contains("популярная книга")) return false;
            if (g.Contains("популяризация науки")) return false;
            if (g.Contains("политология")) return false;
            if (g.Contains("подкасты")) return false;
            if (g.Contains("притча")) return false;
            if (g.Contains("просвещение")) return false;
            if (g.Contains("право")) return false;
            if (g.Contains("поэзия")) return false;
            if (g.Contains("поэма")) return false;
            if (g.Contains("философия")) return false;
            if (g.Contains("юридическое")) return false;
            if (g.Contains("христианство")) return false;
            if (g.Contains("филология")) return false;
            if (g.Contains("философск")) return false;
            if (g.Contains("финансы")) return false;
            if (g.Contains("фитнес")) return false;
            if (g.Contains("фольклор")) return false;
            if (g.Contains("инвестиции")) return false;
            if (g.Contains("эзотери")) return false;
            if (g.Contains("деловая")) return false;
            if (g.Contains("деловой")) return false;
            if (g.Contains("геополитика")) return false;
            if (g.Contains("прогноз")) return false;
            if (g.Contains("учебная")) return false;
            if (g.Contains("успех")) return false;
            if (g.Contains("уроки")) return false;
            if (g.Contains("богословие")) return false;
            if (g.Contains("менеджмент")) return false;
            if (g.Contains("тантра")) return false;
            if (g.Contains("травелог")) return false;
            if (g.Contains("трейдинг")) return false;
            if (g.Contains("тренинг")) return false;
            if (g.Contains("стихотворений")) return false;
            if (g.Contains("социолингвистика")) return false;
            if (g.Contains("affluent consumers")) return false;
            if (g.Contains("faith")) return false;
            if (g.Contains("world war ii")) return false;
            if (g.Contains("беседы")) return false;
            if (g.Contains("басни")) return false;
            if (g.Contains("басня")) return false;
            if (g.Contains("аудиопьеса")) return false;
            if (g.Contains("аудиоспектакль")) return false;
            if (g.Contains("аудиокурс")) return false;
            if (g.Contains("детские сказки")) return false;
            if (g.Contains("детские ужастики")) return false;
            if (g.Contains("детский роман")) return false;
            if (g.Contains("детский детектив")) return false;
            if (g.Contains("дзэн")) return false;
            if (g.Contains("документальная")) return false;
            if (g.Contains("документальный")) return false;
            if (g.Contains("духовная")) return false;
            if (g.Contains("журналистика")) return false;
            if (g.Contains("есесвенные науки")) return false;
            if (g.Contains("йога")) return false;
            if (g.Contains("стих")) return false;
            if (g.Contains("саморазвитие")) return false;
            if (g.Contains("постановка")) return false;
            if (g.Contains("религиозная")) return false;
            if (g.Contains("радиоспектакль")) return false;
            if (g.Contains("радиопьеса")) return false;
            if (g.Contains("спектакль")) return false;
            if (g.Contains("радио")) return false;
            if (g.Contains("развитие")) return false;
            if (g.Contains("radio")) return false;
            if (g.Contains("quantum")) return false;
            if (g.Contains("самопознание")) return false;
            if (g.Contains("самоусовершествование")) return false;
            if (g.Contains("сказка для самых маленьких")) return false;
            if (g.Contains("сборник")) return false;
            if (g.Contains("философ")) return false;
            if (g.Contains("сонеты")) return false;
            if (g.Contains("hypnosis")) return false;
            if (g.Contains("аналитичес")) return false;
            if (g.Contains("spiritual")) return false;
            if (g.Contains("идеологии")) return false;
            if (g.Contains("загадки")) return false;
            if (g.Contains("дхамма")) return false;
            if (g.Contains("драматургия")) return false;
            if (g.Contains("биографии")) return false;
            if (g.Contains("stories")) return false;
            if (g.Contains("vajrayana")) return false;
            if (g.Contains("theravada")) return false;
            if (g.Contains("адвайта-веданта")) return false;
            if (g.Contains("трактат")) return false;
            return true;
        }

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
        ["трагедия"] = "tragedy",
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
