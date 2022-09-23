using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using RegExtract;
using Tests.UniversalParsing;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Rutracker;

public record Header(int Id);

public abstract record Topic;

[UsedImplicitly]
public sealed record Story(
    int TopicId,
    string Url,
    string? Title,
    string? Author,
    string? Performer,
    string? Year,
    string? Series,
    string? NumberInSeries,
    string? Genre,
    string? PlayTime) : Topic;

public sealed record Series : Topic;

public static class Parser
{
    private static readonly Regex SeriesRgx =
        new("\\s+\\(" +
            "(Книга|книга|Часть|часть|Том|том)" +
            "\\s+(?<num>первая|вторая|третья|четвертая|пятая|I{1,3}|IV|V{0,3}|\\d+)" +
            "\\)ξ");

    public static Header[] ParseRussianFantasyHeaders(this HtmlNode node)
    {
        return node.SelectNodes("//tr[@class='hl-tr']/td[1]")
            .Select(n => new Header(n.ParseIntAttribute("id")))
            .ToArray();
    }

    public static HtmlNode GetForumPost(this HtmlNode node) =>
        node.SelectSingleNode("//div[@class='post_body']");

    public static Topic? ParseRussianFantasyTopic(this JObject post)
    {
        var topicId = post.FindTag("topic-id")!.ParseInt();
        var year = post.FindTag("Год выпуска")?.TrimEnd('.', 'г', ' ');
        var lastName = post.FindTags("Фамилия автора", "Фамилии авторов", "Фамилия авторов",
            "Aвтор", "Автор" /* different "A"? */, "Автора", "Авторы");
        var firstName = post.FindTags("Имя автора", "Имена авторов");
        var performer = post.FindTags("Исполнитель", "Исполнители", "Исполнитель и звукорежиссёр");
        var (series, num) = GetSeries(post);
        var numberInSeries = post.FindTag("Номер книги") ?? num;
        var genre = post.FindTag("Жанр");
        var playTime = post.FindTag("Время звучания");
        var title = GetTitle(post, series, firstName, lastName);
        if (title == null)
            return null;

        var author = CombineAuthors(lastName, firstName);
        return new Story(topicId,
            $"https://rutracker.org/forum/viewtopic.php?t={topicId}",
            title, author, performer, year, series, numberInSeries,
            genre, playTime);
    }

    private static string? GetTitle(JObject post, string? series, string? firstName,
        string? secondName)
    {
        var title = GetRawTitle(post, series);
        var noJunk = RemoveExplicitJunkFromTitle(title);
        if (noJunk == null) return null;
        var s1 = RemoveSeriesPrefixFromTitle(noJunk, series);
        var s2 = RemoveAuthorPrefixFromTitle(s1, firstName, secondName);
        var s3 = RemoveSeriesPrefixFromTitle(s2, series);
        var s4 = RemoveAuthorPrefixFromTitle(s3, firstName, secondName);
        return s4
            .Trim('•', ' ')
            .Unquote()
            .Unbrace('«', '»')
            .Unbrace('<', '>')
            .CompressIfPossible();

        static string? GetRawTitle(JObject post, string? series)
        {
            var titleOptions = GetTitleOptions(post);
            var first = titleOptions.FirstOrDefault();
            var second = titleOptions.Skip(1).FirstOrDefault();
            var result = first == series && second != null ? second : first;
            return result?
                .Trim(' ', '•')
                .Replace('\n', ' ')
                .Unquote();
        }

        static List<string> GetTitleOptions(JObject post) =>
            post.TryGetValue("headers", out var arr) && arr is JArray jArr
                ? jArr.Select(token => token.Value<string>()).ToList()!
                : new List<string>();

        static string? RemoveExplicitJunkFromTitle(string? input)
        {
            if (input is null or "Рассказы")
                return null;

            var s1 = input.RemoveRegexIfItIsNotTheWholeString("\\[.*\\]\\.?");
            var s2 = s1.Unbrace('[', ']');
            var s3 = s2.RemoveRegexIfItIsNotTheWholeString("\\(.*\\)\\.?");
            var s4 = s3.Unbrace('(', ')');
            var s5 = s4.StartsWith("Рассказ")
                ? s4["Рассказ".Length..].Trim(' ', '\"')
                : s4;
            return s5;
        }

        static string RemoveAuthorPrefixFromTitle(string title, string? f, string? s)
        {
            var o1 = f + " " + s;
            if (string.IsNullOrWhiteSpace(o1)) return title;

            if (title.Contains(o1))
                return title.Replace(o1, "").Trim('-', '–', ' ');

            var o2 = s + " " + f;
            if (title.Contains(o2))
                return title.Replace(o2, "").Trim('-', '–', ' ');

            return title;
        }

        static string RemoveSeriesPrefixFromTitle(string title, string? series)
        {
            var idx = title.IndexOf(" серия ", StringComparison.InvariantCulture);
            if (idx != -1)
                return title[..idx].Trim('.', '-').Trim();

            if (series == null)
                return title;

            if (!title.StartsWith(series) || title == series || title == series + ".") return title;

            var t1 = Regex.Replace(title,
                    series + "(\n|. )" +
                    "(Книга|книга|Часть|часть|Том|том)" +
                    "\\s+(первая|вторая|третья|четвертая|пятая|I{1,3}|IV|V{0,3}|\\d+)" +
                    "(\\.|,)", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(t1))
                return title.Replace(series, "").TrimStart('\n', ' ', '.');
            if (t1 != title) return t1;

            var t3 = Regex.Replace(title,
                series + "\\s*(-|,)?\\s*\\d+(\\.|,|:)", "").Trim();
            if (t3 != title) return t3;
            var t2 = Regex.Replace(title,
                series + "\\s*(\\.|:|-)\\s*", "").Trim();
            if (t2 != title && !int.TryParse(t2.Trim(), out _)) return t2;
            var t4 = title.Replace(series + ".", "").Trim();
            if (t4 != title) return t4;
            return title;
        }
    }

    private static string? CombineAuthors(string? s, string? f)
    {
        if (string.IsNullOrWhiteSpace(s + f))
            return null;
        var a = s?.Trim().Replace(";", ",");
        var b = f?.Trim().Replace(";", ",");
        if (a == null) return b;
        if (b == null) return a;
        if (a.Contains(',') && b.Contains(','))
            return a.Split(',', RemoveEmptyEntries).Zip(b.Split(',', RemoveEmptyEntries))
                .Select(x => x.First.Trim('_').Trim() + " " + x.Second.Trim('_').Trim())
                .StrJoin();
        return a + " " + b;
    }

    private static (string?, string?) GetSeries(JObject post)
    {
        var rawSeries = post.FindTags("Цикл/серия", "Цикл", "Серия");
        if (rawSeries != null && string.IsNullOrWhiteSpace(rawSeries))
            return ("<YES>", null);
        var wrap = rawSeries ?? GetSeriesFromSpoiler(post);
        var unbrace = wrap?.TrimEnd(':', ' ', '-')
            .Trim()
            .Unquote()
            .Unbrace('«', '»');
        if (unbrace == null) return (null, null);

        var match = SeriesRgx.Match(unbrace + "ξ");
        if (!match.Success) return (unbrace, null);
        var s = SeriesRgx.Replace(unbrace + "ξ", "")
            .Trim()
            .Unquote()
            .Unbrace('«', '»');
        return (s, match.Groups["num"].Value);

        static string? GetSeriesFromSpoiler(JObject post)
        {
            var src = post.TryGetValue("spoilers", out var arr) && arr is JArray jArr
                ? jArr.Select(token => token.Value<string>()).ToList()!
                : new List<string>();

            var spoiler = src.FirstOrDefault(s => s.Contains("Цикл"));
            if (spoiler == null) return null;
            var replace = spoiler
                .Replace("Цикл/серия", "Цикл")
                .Replace("Цикл книг", "Цикл")
                .Replace("&#34;", "ξ")
                .Replace('<', 'ξ')
                .Replace('>', 'ξ')
                .Replace('«', 'ξ')
                .Replace('»', 'ξ');
            if (replace.Trim() == "Цикл")
                return "<PRESENT>";
            if (!replace.Contains("ξ"))
            {
                var s = replace["Цикл".Length..];
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }

            return replace.Extract<string>(@"Цикл ξ(.*)ξ");
        }
    }

    public static int? GetTopicId(this XNode node)
    {
        if (node is not XElement element) return null;
        var attributeValue = element.Attribute("data-ext_link_data")?.Value;
        if (attributeValue == null) return null;
        var jObject = JObject.Parse(attributeValue);
        return jObject["t"]!.Value<int>();
    }
}