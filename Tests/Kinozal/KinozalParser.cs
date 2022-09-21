using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using HtmlAgilityPack;
using JetBrains.Annotations;
using RegExtract;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Kinozal;

public sealed record KinozalForumPost(int Id, int? SeriesId, XElement Xml)
{
    [UsedImplicitly]
    [Obsolete("For deserialization only", true)]
    public KinozalForumPost() : this(0, null!, null!)
    {
    }
}

public static class KinozalParser
{
    public static int[] ParseKinozalFantasyHeaders(this HtmlNode node)
    {
        var rows = node.SelectNodes("//table[@class='t_peer w100p']/tr/td[2]/a").Skip(1);
        return rows
            .Select(n => n.Href(skipPrefix: "/details.php?id=")?.ParseIntOrNull())
            .OfType<int>()
            .ToArray();
    }

    public static KinozalForumPost GetKinozalForumPost(this HtmlNode node)
    {
        var post = node.SelectSubNode("//div[@class='mn1_content']")!;
        var id = post
            .SelectSubNode("//a")!
            .Href()!
            .Split("?id=")[1].ToInt();

        var seriesId = post
            .SelectSubNodes("div[@class='bx1'][1]//ul[@class='lis']/li/a")
            .FirstOrDefault(a => a.InnerText.Trim() == "Цикл")?
            .GetAttributeValue("onclick", null)
            .Extract<int>($"showtab\\({id},(\\d+)\\); return false;");

        var sections = post
            .SelectSubNodes("div[@class='bx1 justify']")
            .Select(div => div.CleanUpToXml());
        var root = new XElement("root", new XAttribute("topic-id", id));
        foreach (var section in sections) root.Add(section);
        return new KinozalForumPost(id, seriesId, root);
    }

    private const RegexOptions RxO = RegexOptions.Compiled | RegexOptions.IgnoreCase;
    private static readonly Regex EmptySeriesFormat = new(
        "\\s*Цикл( книг)?\\s*:", RxO);

    private const string Header = "(Книги цикла|Серия( книг)?|Содержание (.*?)|Цикл( книг)?)";
    private static readonly Regex SeriesFormatWithColon = new(
        $"^\\s*({Header}\\s+)?(?<name>.*?)\\s*:(.*)", RxO);
    private static readonly Regex SeriesFormatWithQuotes = new(
        $"^\\s*({Header}\\s+)?(«|\")(?<name>.*?)(»|\")\\s*", RxO);
    private static readonly Regex SeriesFormatWithDashAndNewLine = new(
        $"^\\s*{Header}\\s*-?\\s*(?<name>.*?)\\n", RxO);
    private static readonly Regex MultiEntryFormat = new(
        "«(?<name>[^»]+)»(\\s*/\\s*)?", RxO);

    public static IEnumerable<string> ParseKinozalFormat(this string input)
    {
        if (EmptySeriesFormat.IsMatch(input)) yield break;
        var prelim = GetFirstMatch(input,
            SeriesFormatWithColon,
            SeriesFormatWithQuotes,
            SeriesFormatWithDashAndNewLine) ??
            LaconicStyle(input);
        if (JustNumberedList(input))
            yield break;
        if (prelim == null)
            yield break;
        var list = GetAllNamedMatches(MultiEntryFormat, "name", prelim).ToList();
        if (list.Count > 1)
        {
            foreach (var item in list)
                yield return Peel(item);
        }
        else
        {
            yield return Peel(prelim);
        }

        static string Peel(string input) =>
            input.Unquote().Unbrace('«', '»').Trim();

        static bool JustNumberedList(string input)
        {
            var readLines = input.ReadLines().ToList();
            if (readLines.Count < 2) return false;
            var numbers = readLines
                .Select(l => l.LeftPart('.').ParseIntOrNull())
                .ToList();
            return numbers.All(i => i != null);
        }

        static string? LaconicStyle(string input)
        {
            var readLines = input.ReadLines().ToList();
            if (readLines.Count < 2) return null;
            var numbers = readLines
                .Select(l => l.LeftPart('.').ParseIntOrNull())
                .ToList();
            if (numbers[0] != null) return null;
            return numbers.Skip(1).All(i => i != null) ? readLines[0] : null;
        }
        static string? GetFirstMatch(string input, params Regex[] regexList)
        {
            foreach (var regex in regexList)
            {
                var match = regex.Match(input);
                if (match.Success)
                    return match.Groups["name"].Value;
            }

            return null;
        }
        static IEnumerable<string> GetAllNamedMatches(Regex regex, string groupName, string input)
        {
            var match = regex.Match(input);
            while (match.Success)
            {
                yield return match.Groups[groupName].Value;
                match = match.NextMatch();
            }
        }
    }

    public static IEnumerable<string> GetSeries(this XElement kinozalFormat)
    {
        var innerText = kinozalFormat.InnerText();
        return ParseKinozalFormat(innerText);
    }
}

public sealed class SeriesFormatTests
{
    [Theory]
    [InlineData("Цикл книг:\n01. «Крутые наследнички»\n02. «За всеми...")]
    [InlineData("+ «Ангельская» работёнка / Angels in the Moonlight (2017)\nЧеловек с одним")]
    [InlineData("1. Охотник на вундерваффе\n2. Охотник на попаданцев\n3. Охота на охотника\n4. Охотник на шпионов\n5. Охота в атомном аду")]
    public void ParseKinozalFormat_NoResults(string input)
    {
        input.ParseKinozalFormat().Should().BeEmpty();
    }
    [Theory]
    [InlineData("Цикл \"Вадим Арсеньев\":\n1. Зов Полярной...", "Вадим Арсеньев")]
    [InlineData("Цикл книг «Неверный ленинец»:\nКнига 1. Провок..", "Неверный ленинец")]
    [InlineData("Цикл \" Сам себе цикл\":\n1. Сам себе к", "Сам себе цикл")]
    [InlineData("Цикл \"Красные\":1. Неверный \"Единая\":1. Единая", "Красные")]
    [InlineData("Цикл \"Другой мир\"\n1. Другой мир.", "Другой мир")]
    [InlineData("Содержание цикла «Рик Саттор»:\n1. Мальчик из другой", "Рик Саттор")]
    [InlineData("Серия «Коптский крест»\n1. Коптский", "Коптский крест")]
    [InlineData("Книги цикла «Анита Моррьентес»:\n1. Волчий камень\n2.", "Анита Моррьентес")]
    [InlineData("\"Приключения Буратино\":\n1. Времени нет\n2. Новейший Завет", "Приключения Буратино")]
    [InlineData("Содержание трилогии «КоДекс 1962»\n1. Зародыш мой видели очи Твои. История любви\n2. Тысяча лет Исландии\n3. Я спящая дверь", "КоДекс 1962")]
    [InlineData("Серия книг - Мир из прорех\n1. Новые правила\n2. Другой город\n3. Иные земли", "Мир из прорех")]
    [InlineData("цикл «Расследования Макара Илюшина и Сергея Бабкина»\n(по версии сайта \"ЛитРес\")\n[1 книга][Рассказ] Мужская логика 8-го Марта\n[2 книга][Роман] Знак Истинного Пути\n[3 книга][Роман] Остров сбывшейся мечты\n[4 книга][Роман] Тёмная сторона души\n[5 книга][Роман] Водоворот чужих желаний\n[6 книга][Роман] Рыцарь нашего времени\n[7 книга][Роман] Призрак в кривом зеркале\n[8 книга][Роман] Танцы марионеток\n[9 книга][Роман] Улыбка пересмешника\n[10 книга][Роман] Дудочка крысолова\n[11 книга][Роман] Манускрипт дьявола\n[12 книга][Роман] Золушка и Дракон\n[13 книга][Рассказ] Убийственная библиотека\n[14 книга][Роман] Комната старинных ключей\n[15 книга][Роман] Пари с морским дьяволом\n[16 книга][Роман] Охота на крылатого льва\n[17 книга][Роман] Нежные листья, ядовитые корни\n[18 книга][Роман] Чёрный пудель, рыжий кот, или Свадьба с препятствиями\n[19 книга][Роман] Бумажный занавес, стеклянная корона\n[20 книга][Роман] Пирог из горького миндаля\n[21 книга][Роман] Закрой дверь за совой\n[22 книга][Роман] Нет кузнечика в траве\n[23 книга][Роман] След лисицы на камнях\n[24 книга][Роман] Кто остался под холмом\n[25 книга][Роман] Человек из дома напротив\n[26 книга][Роман] Самая хитрая рыба\n[27 книга][Роман]Тот, кто ловит мотыльков\n[28 книга][Роман]Лягушачий король\n[29 книга][Роман]Тигровый, черный, золотой\n+ [Рассказ]Черная кошка в белой комнате", "Расследования Макара Илюшина и Сергея Бабкина")]
    [InlineData("цикл крестный отец\n1. Крестный отец\n3. Омерта\n( По версии Литрес)", "крестный отец")]
    [InlineData("Ведьмак\n1. Последнее желание\n2. Меч Предназначения\n3. Кровь эльфов\n4. Час Презрения\n5. Крещение огнём\n6. Башня Ласточки\n7. Владычица Озера\n8. Сезон гроз", "Ведьмак")]
    [InlineData("Хайнский цикл (Книги цикла сюжетно не связаны)\n01. Обделённые (1974) + За день до революции (1974)\n02. Слово для «леса» и «мира» одно (1972)\n03. Мир Роканнона (1966)\n04. Обширней и медлительней империй (1971)\n05. Планета изгнания (1966)\n06. Город иллюзий (1967)\n07. Левая рука Тьмы (1969)+Король планеты Зима (1969) + Взросление в Кархайде (1995)\n08. Толкователи (2000)\n09. Рыбак из Внутриморья [цикл] (История «шобиков» (1990), Танцуя Ганам (1993), Ещё одна история, или Рыбак из Внутриморья (1994))\n10. Четыре пути к прощению (1995) [роман-сборник] (Предательства (1994), День прощения (1994), Муж рода (1995), Освобождение женщины (1995), Старая Музыка и рабыни (1999), Заметки об Уэреле и Йеове (1995))\n11. Рассказы (Невыбранная любовь (1994), Законы гор (1996), Дело о Сеггри (1994), Одиночество (1994))", "Хайнский цикл (Книги цикла сюжетно не связаны)")]
    public void ParseKinozalFormat(string input, string? expectedOutput)
    {
        input.ParseKinozalFormat().Should().Equal(expectedOutput);
    }
    [Fact]
    public void ParseKinozalFormat_MultipleResults()
    {
        "Цикл «Мечник Континента» / «Долгая дорога в стаб»:\n1. Долгая дорога в стаб\n2. Фагоцит\n3. Вера в будущее\n4. За пределами"
            .ParseKinozalFormat().Should()
            .Equal("Мечник Континента", "Долгая дорога в стаб");
    }
}