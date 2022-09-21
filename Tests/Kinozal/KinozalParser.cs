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

    private static readonly Regex EmptySeriesFormat = new(
        "\\s*Цикл( книг)?\\s*:", RegexOptions.Compiled);
    private static readonly Regex SeriesFormat = new(
        "\\s*Цикл( книг)?\\s+(?<name>.*?)\\s*:(.*)", RegexOptions.Compiled);

    public static IEnumerable<string> ParseKinozalFormat(this string input)
    {
        if (EmptySeriesFormat.IsMatch(input)) yield break;
        var match = SeriesFormat.Match(input);
        if (!match.Success) yield break;
        var series = match.Groups["name"].Value.Unquote().Unbrace('«', '»').Trim();
        if (series.Length > 40)
            Console.WriteLine(
                input.EncodeJson().Quoted() + ", " +
                series.EncodeJson().Quoted());
        yield return series;

    }

    public static IEnumerable<string>  GetSeries(this XElement kinozalFormat)
    {
        var innerText = kinozalFormat.InnerText();
        return ParseKinozalFormat(innerText);
    }
}

public sealed class SeriesFormatTests
{
    [Theory]
    [InlineData("Цикл книг:\n01. «Крутые наследнички»\n02. «За всеми...")]
    public void ParseKinozalFormat_NoResults(string input)
    {
        input.ParseKinozalFormat().Should().BeEmpty();
    }
    [Theory]
    [InlineData("Цикл \"Вадим Арсеньев\":\n1. Зов Полярной...", "Вадим Арсеньев")]
    [InlineData("Цикл книг «Неверный ленинец»:\nКнига 1. Провок..", "Неверный ленинец")]
    [InlineData("Цикл \" Сам себе цикл\":\n1. Сам себе к", "Сам себе цикл")]
    [InlineData("Цикл \"Красные Цепи\":1. Красные Цепи 2. Молот Ведьм3. КультЦикл \"Единая теория всего\":1. Единая", "Красные Цепи")]
    public void ParseKinozalFormat(string input, string? expectedOutput)
    {
        input.ParseKinozalFormat().Should().Equal(expectedOutput);
    }
    [Fact]
    public void ParseKinozalFormat_MultipleResults()
    {
        "Цикл «Мечник Континента» / «Долгая дорога в стаб»:\n1. Долгая дорога в стаб\n2. Фагоцит\n3. Вера в будущее\n4. За пределами"
            .ParseKinozalFormat().Should()
            .Equal("Мечник Континента","Долгая дорога в стаб");
    }
}