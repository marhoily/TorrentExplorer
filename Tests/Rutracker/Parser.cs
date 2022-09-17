using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RegExtract;
using Tests.Utilities;
using static System.StringSplitOptions;
using static Tests.Utilities.HtmlExtensions;

namespace Tests.Rutracker;

public static class Known
{
    public static bool IsKnownTag(this string value) => 
        !string.IsNullOrWhiteSpace(value) &&
        Tags.Any(t => Eq(value, t));

    private static bool Eq(string value, string tag) =>
        value.StartsWith(tag) && 
        value[tag.Length..].Trim() is "" or ":";

    public static readonly string[] Tags =
    {
        "Год выпуска",
        "Фамилия автора",
        "Фамилии авторов",
        "Aвтор",
        "Автор",
        "Автора",
        "Авторы",
        "Имя автора",
        "Имена авторов",
        "Исполнитель",
        "Исполнители",
        "Исполнитель и звукорежиссёр",
        "Цикл",
        "Цикл/серия",
        "Номер книги",
        "Жанр",
        "Время звучания"
    };
}
public record Header(int Id);

public abstract record Topic;

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
    public static Header[] ParseRussianFantasyHeaders(this HtmlNode node)
    {
        return node.SelectNodes("//tr[@class='hl-tr']/td[1]")
            .Select(n => new Header(n.ParseIntAttribute("id")))
            .ToArray();
    }

    public static HtmlNode GetForumPost(this HtmlNode node) =>
        node.SelectSingleNode("//div[@class='post_body']");

    public static Topic? ParseRussianFantasyTopic(this HtmlNode node)
    {
        var topicId = node.FirstChild.GetTopicId();

        var post = node.SelectSingleNode("//div[@class='post_body']");
        if (post.FindTags("Год выпуска").Count() > 1)
            return new Series();

        var year = post.FindTag("Год выпуска")?.TagValue().TrimEnd('.', 'г', ' ');
        var lastName = post.FindTags("Фамилия автора", "Фамилии авторов", 
            "Aвтор", "Автор" /* different "A"? */, "Автора", "Авторы")?.TagValue();
        var firstName = post.FindTags("Имя автора", "Имена авторов")?.TagValue();
        var performer = post.FindTags("Исполнитель", "Исполнители", "Исполнитель и звукорежиссёр")?.TagValue();
        var series = GetSeries(post);
        var numberInSeries = post.FindTag("Номер книги")?.TagValue();
        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();
        var title = GetTitle(post, series, firstName, lastName);
        if (title == null)
            return null;

        var author = CombineAuthors(lastName, firstName);
        return new Story(topicId,
            $"https://rutracker.org/forum/viewtopic.php?t={topicId}",
            title, author, performer, year, series, numberInSeries,
            genre, playTime);
    }
    public static Topic? ParseRussianFantasyTopic(this Dictionary<string, object> post)
    {
        var topicId = post.FindTag("topic-id")!.ParseInt();
        var year = post.FindTag("Год выпуска")?.TrimEnd('.', 'г', ' ');
        var lastName = post.FindTags("Фамилия автора", "Фамилии авторов", 
            "Aвтор", "Автор" /* different "A"? */, "Автора", "Авторы");
        var firstName = post.FindTags("Имя автора", "Имена авторов");
        var performer = post.FindTags("Исполнитель", "Исполнители", "Исполнитель и звукорежиссёр");
        var series = GetSeries(post);
        var numberInSeries = post.FindTag("Номер книги");
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

    private static string? GetTitle(HtmlNode post, string? series, string? firstName, string? secondName)
    {
        var title = GetRawTitle(post, series);
        var noJunk = RemoveExplicitJunkFromTitle(title);
        if (noJunk == null) return null;
        var s1 = RemoveSeriesPrefixFromTitle(noJunk, series);
        var s2 = RemoveAuthorPrefixFromTitle(s1, firstName, secondName);
        var s3 = RemoveSeriesPrefixFromTitle(s2, series);
        var s4 = RemoveAuthorPrefixFromTitle(s3, firstName, secondName);
        return s4.Trim('•', ' ');

        static string? GetRawTitle(HtmlNode post, string? series)
        {
            var titleOptions = GetTitleOptions(post);
            var first = titleOptions.FirstOrDefault();
            var second = titleOptions.Skip(1).FirstOrDefault();
            var result = first == series && second != null ? second : first;
            return result?.Trim(' ', '•').Unquote();
        }

        static List<string> GetTitleOptions(HtmlNode post)
        {
            return post.Walk<string>((node, cfg) =>
            {
                if (node.NodeType == HtmlNodeType.Text && Known.Tags.Any(a => node.InnerText.Contains(a)))
                    return cfg.YieldBreak(WalkInstruction.GoBy);

                if (node.Name != "span")
                    return cfg.Continue(WalkInstruction.GoDeep);

                if ((node.GetFontSize() ?? 0) <= 20)
                    return cfg.Continue(WalkInstruction.GoDeep);

                return string.IsNullOrWhiteSpace(node.InnerText) 
                    ? cfg.Continue(WalkInstruction.GoDeep) 
                    : cfg.Yield(node.InnerText, WalkInstruction.GoBy);
            });
        }

        static string? RemoveExplicitJunkFromTitle(string? input)
        {
            if (input is null or "Рассказы")
                return null;

            var s1 = input.RemoveRegexIfItIsNotTheWholeString("\\[.*\\]");
            var s2 = s1.Unbrace('[', ']');
            var s3 = s2.RemoveRegexIfItIsNotTheWholeString("\\(.*\\)");
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
                    "(Книга|Часть|Том) " +
                    "(первая|вторая|третья|I|II|III|IV|V|VI|VII|\\d+)" +
                    "(\\.|,)", "")
                .Trim();
            if (string.IsNullOrWhiteSpace(t1))
                return title.Replace(series, "").TrimStart('\n', ' ', '.');
            if (t1 != title) return t1;

            var t2 = Regex.Replace(title, series + " (\\d)*(\\.|,)", "").Trim();
            if (t2 != title) return t2;
            var t3 = Regex.Replace(title, series + "( |-)(\\d)*(\\.|,)", "").Trim();
            if (t3 != title) return t3;
            var t4 = title.Replace(series + ".", "").Trim();
            if (t4 != title) return t4;
            return title;
        }
    }
    private static string? GetTitle(Dictionary<string, object> post, string? series, string? firstName, string? secondName)
    {
        var title = GetRawTitle(post, series);
        var noJunk = RemoveExplicitJunkFromTitle(title);
        if (noJunk == null) return null;
        var s1 = RemoveSeriesPrefixFromTitle(noJunk, series);
        var s2 = RemoveAuthorPrefixFromTitle(s1, firstName, secondName);
        var s3 = RemoveSeriesPrefixFromTitle(s2, series);
        var s4 = RemoveAuthorPrefixFromTitle(s3, firstName, secondName);
        return s4.Trim('•', ' ');

        static string? GetRawTitle(Dictionary<string, object> post, string? series)
        {
            var titleOptions = GetTitleOptions(post);
            var first = titleOptions.FirstOrDefault();
            var second = titleOptions.Skip(1).FirstOrDefault();
            var result = first == series && second != null ? second : first;
            return result?.Trim(' ', '•').Unquote();
        }

        static List<string> GetTitleOptions(Dictionary<string, object> post)
        {
            return post.TryGetValue("headers", out var arr) && arr is JArray jArr
                ? jArr.Select(token => token.Value<string>()).ToList()!
                : new List<string>();
        }

        static string? RemoveExplicitJunkFromTitle(string? input)
        {
            if (input is null or "Рассказы")
                return null;

            var s1 = input.RemoveRegexIfItIsNotTheWholeString("\\[.*\\]");
            var s2 = s1.Unbrace('[', ']');
            var s3 = s2.RemoveRegexIfItIsNotTheWholeString("\\(.*\\)");
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
                    "(Книга|Часть|Том) " +
                    "(первая|вторая|третья|I|II|III|IV|V|VI|VII|\\d+)" +
                    "(\\.|,)", "")
                .Trim();
            if (string.IsNullOrWhiteSpace(t1))
                return title.Replace(series, "").TrimStart('\n', ' ', '.');
            if (t1 != title) return t1;

            var t2 = Regex.Replace(title, series + " (\\d)*(\\.|,)", "").Trim();
            if (t2 != title) return t2;
            var t3 = Regex.Replace(title, series + "( |-)(\\d)*(\\.|,)", "").Trim();
            if (t3 != title) return t3;
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

    private static string? GetSeries(HtmlNode post)
    {
        var rawSeries = post.FindTags("Цикл", "Цикл/серия")?.TagValue();
        if (rawSeries != null && string.IsNullOrWhiteSpace(rawSeries))
            return "<YES>";
        return rawSeries ?? GetSeriesFromSpoiler(post);

        static string? GetSeriesFromSpoiler(HtmlNode post)
        {
            var singleOrDefault = post.GetSpoilers()
                .SingleOrDefault(s => s.Header.Contains("Цикл"));
            if (singleOrDefault == null) return null;
            var replace = singleOrDefault
                .Header
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
    private static string? GetSeries(Dictionary<string, object> post)
    {
        var rawSeries = post.FindTags("Цикл", "Цикл/серия");
        if (rawSeries != null && string.IsNullOrWhiteSpace(rawSeries))
            return "<YES>";
        return rawSeries ?? GetSeriesFromSpoiler(post);

        static string? GetSeriesFromSpoiler(Dictionary<string, object> post)
        {
            var src =  post.TryGetValue("spoilers", out var arr) && arr is JArray jArr
                ? jArr.Select(token => token.Value<string>()).ToList()!
                : new List<string>();

            var singleOrDefault = src
                .SingleOrDefault(s => s.Contains("Цикл"));
            if (singleOrDefault == null) return null;
            var replace = singleOrDefault
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
}